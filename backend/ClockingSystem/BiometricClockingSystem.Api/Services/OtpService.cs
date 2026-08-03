using System.Security.Cryptography;
using System.Text;
using BiometricClockingSystem.Api.Data;
using BiometricClockingSystem.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BiometricClockingSystem.Api.Services;

public sealed class OtpService : IOtpService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OtpService> _logger;
    private readonly HttpClient _httpClient;
    private readonly TwilioOptions _twilio;

    public OtpService(
        ApplicationDbContext context,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        IOptions<TwilioOptions> twilioOptions,
        ILogger<OtpService> logger)
    {
        _context = context;
        _cache = cache;
        _httpClient = httpClientFactory.CreateClient();
        _twilio = twilioOptions.Value;
        _logger = logger;
    }

    public async Task<OtpChallenge> CreateAsync(string employeeId, ClockType clockType)
    {
        var phoneNumber = await _context.Employees
            .Where(employee => employee.EmployeeNumber == employeeId.ToString() && employee.IsActive)
            .Select(employee => employee.PhoneNumber)
            .SingleOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new OtpDeliveryException("The employee does not have a phone number for OTP delivery.");
        if (string.IsNullOrWhiteSpace(AccountSid) ||
            string.IsNullOrWhiteSpace(AuthToken) ||
            string.IsNullOrWhiteSpace(FromPhoneNumber))
            throw new OtpDeliveryException("Twilio SMS is not configured.");

        var id = Guid.NewGuid();
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var challenge = new StoredChallenge(employeeId, clockType, Hash(code), DateTime.UtcNow.Add(Lifetime));

        await SendSmsAsync(phoneNumber, code);
        _cache.Set(CacheKey(id), challenge, challenge.ExpiresAt);
        _cache.Set(EmployeeCacheKey(employeeId), id, challenge.ExpiresAt);
        _logger.LogInformation("OTP SMS sent for employee {EmployeeId}. Expires at {ExpiresAt:O}", employeeId, challenge.ExpiresAt);
        return new OtpChallenge(id, employeeId, clockType, challenge.ExpiresAt);
    }

    public Task GenerateAndSendAsync(string employeeId) => CreateAsync(employeeId, ClockType.ClockIn);

    public Task<OtpVerificationResult> VerifyAsync(Guid challengeId, string code)
    {
        if (!_cache.TryGetValue<StoredChallenge>(CacheKey(challengeId), out var challenge) || challenge is null)
            return Task.FromResult(new OtpVerificationResult(false, "The verification code has expired or is invalid."));

        _cache.Remove(CacheKey(challengeId));
        _cache.Remove(EmployeeCacheKey(challenge.EmployeeId));
        if (string.IsNullOrWhiteSpace(code) || !CryptographicOperations.FixedTimeEquals(Hash(code.Trim()), challenge.CodeHash))
            return Task.FromResult(new OtpVerificationResult(false, "The verification code is incorrect."));

        return Task.FromResult(new OtpVerificationResult(true));
    }

    public bool TryGetChallenge(Guid id, out OtpChallenge challenge)
    {
        if (_cache.TryGetValue<StoredChallenge>(CacheKey(id), out var stored) && stored is not null)
        {
            challenge = new OtpChallenge(id, stored.EmployeeId, stored.ClockType, stored.ExpiresAt);
            return true;
        }
        challenge = default!;
        return false;
    }

    public bool TryGetChallengeForEmployee(string employeeId, out OtpChallenge challenge)
    {
        if (_cache.TryGetValue<Guid>(EmployeeCacheKey(employeeId), out var challengeId))
            return TryGetChallenge(challengeId, out challenge);

        challenge = default!;
        return false;
    }

    private static byte[] Hash(string code) => SHA256.HashData(Encoding.UTF8.GetBytes(code));
    private static string CacheKey(Guid id) => $"otp:{id:N}";
    private static string EmployeeCacheKey(string employeeId) => $"otp:employee:{employeeId}";

    private async Task SendSmsAsync(string phoneNumber, string code)
    {
        var endpoint = $"https://api.twilio.com/2010-04-01/Accounts/{Uri.EscapeDataString(AccountSid)}/Messages.json";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = phoneNumber,
                ["From"] = FromPhoneNumber,
                ["Body"] = $"Your biometric attendance verification code is {code}. It expires in 5 minutes."
            })
        };
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{AccountSid}:{AuthToken}"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Twilio rejected OTP SMS with status {StatusCode}.", (int)response.StatusCode);
            throw new OtpDeliveryException("The verification code could not be sent. Please try again.");
        }
    }

    // Support either the nested ASP.NET configuration keys (Twilio__AccountSid)
    // or the standard Twilio environment-variable names used by most deployments.
    private string AccountSid => FirstConfiguredValue(_twilio.AccountSid, "TWILIOACCOUNTSID");
    private string AuthToken => FirstConfiguredValue(_twilio.AuthToken, "TWILIOAUTHTOKEN");
    private string FromPhoneNumber => FirstConfiguredValue(_twilio.FromPhoneNumber, "TWILIOFROMPHONE_NUMBER", "TWILIO_PHONE_NUMBER");

    private static string FirstConfiguredValue(string configuredValue, params string[] environmentNames)
    {
        if (!string.IsNullOrWhiteSpace(configuredValue)) return configuredValue;
        return environmentNames
            .Select(Environment.GetEnvironmentVariable)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    private sealed record StoredChallenge(string EmployeeId, ClockType ClockType, byte[] CodeHash, DateTime ExpiresAt);
}
