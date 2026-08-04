using System.Security.Cryptography;
using System.Text;
using BiometricClockingSystem.Api.Data;
using BiometricClockingSystem.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BiometricClockingSystem.Api.Services;

public sealed class OtpService : IOtpService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(1);
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OtpService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SendGridOptions _sendGrid;

    public OtpService(
        ApplicationDbContext context,
        IMemoryCache cache,
        IOptions<SendGridOptions> sendGridOptions,
        ILogger<OtpService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _cache = cache;
        _sendGrid = sendGridOptions.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<OtpChallenge> CreateAsync(string employeeId, ClockType clockType)
    {
        var emailAddress = await _context.Employees
            .Where(employee => employee.EmployeeNumber == employeeId && employee.IsActive)
            .Select(employee => employee.Email)
            .SingleOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(emailAddress))
            throw new OtpDeliveryException("The employee does not have an email address for OTP delivery.");
        if (string.IsNullOrWhiteSpace(_sendGrid.SenderAddress) ||
            string.IsNullOrWhiteSpace(_sendGrid.ApiKey))
            throw new OtpDeliveryException("SendGrid is not configured.");

        var id = Guid.NewGuid();
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var challenge = new StoredChallenge(employeeId, clockType, Hash(code), DateTime.UtcNow.Add(Lifetime));

        await SendEmailAsync(emailAddress, code);
        _cache.Set(CacheKey(id), challenge, challenge.ExpiresAt);
        _cache.Set(EmployeeCacheKey(employeeId), id, challenge.ExpiresAt);
        _logger.LogInformation("OTP email sent for employee {EmployeeId}. Expires at {ExpiresAt:O}", employeeId, challenge.ExpiresAt);
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

    private async Task SendEmailAsync(string emailAddress, string code)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.sendgrid.com/v3/mail/send");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _sendGrid.ApiKey);
            request.Content = JsonContent.Create(new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[] { new { email = emailAddress } }
                    }
                },
                from = new { email = _sendGrid.SenderAddress },
                subject = "Your biometric attendance verification code",
                content = new[]
                {
                    new
                    {
                        type = "text/plain",
                        value = $"Your biometric attendance verification code is {code}. It expires in 1 minute."
                    }
                }
            });

            var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                var providerMessage = await response.Content.ReadAsStringAsync(timeout.Token);
                _logger.LogWarning(
                    "SendGrid rejected OTP email with status {StatusCode}: {ProviderMessage}",
                    (int)response.StatusCode,
                    providerMessage);
                throw new OtpDeliveryException("The verification code could not be sent. Please try again.");
            }
        }
        catch (OtpDeliveryException)
        {
            throw;
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            _logger.LogWarning("SendGrid OTP email delivery timed out.");
            throw new OtpDeliveryException("The verification code could not be sent within 10 seconds. Please try again.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "SendGrid OTP email delivery failed.");
            throw new OtpDeliveryException("The verification code could not be sent. Please try again.");
        }
    }

    private sealed record StoredChallenge(string EmployeeId, ClockType ClockType, byte[] CodeHash, DateTime ExpiresAt);
}
