using BiometricClockingSystem.Api.Data;
using BiometricClockingSystem.Api.Models;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BiometricClockingSystem.Api.Services;

public sealed class PasskeyService : IPasskeyService
{
    private static readonly TimeSpan CeremonyLifetime = TimeSpan.FromMinutes(5);
    private readonly ApplicationDbContext _context;
    private readonly IFido2 _fido2;
    private readonly IMemoryCache _cache;

    public PasskeyService(ApplicationDbContext context, IFido2 fido2, IMemoryCache cache) =>
        (_context, _fido2, _cache) = (context, fido2, cache);

    public async Task<CredentialCreateOptions> BeginRegistrationAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetActivePrivilegedUserAsync(userId, cancellationToken);
        var existingCredentials = await _context.PasskeyCredentials
            .AsNoTracking()
            .Where(credential => credential.UserId == userId)
            .Select(credential => new PublicKeyCredentialDescriptor(credential.CredentialId))
            .ToListAsync(cancellationToken);

        var options = _fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = ToFidoUser(user),
            ExcludeCredentials = existingCredentials,
            AuthenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = ResidentKeyRequirement.Preferred,
                UserVerification = UserVerificationRequirement.Required
            },
            AttestationPreference = AttestationConveyancePreference.None
        });

        _cache.Set(RegistrationCacheKey(userId), options, CeremonyLifetime);
        return options;
    }

    public async Task FinishRegistrationAsync(Guid userId, AuthenticatorAttestationRawResponse response, CancellationToken cancellationToken)
    {
        if (!_cache.TryGetValue<CredentialCreateOptions>(RegistrationCacheKey(userId), out var options) || options is null)
            throw new InvalidOperationException("The passkey setup request has expired. Start again.");
        _cache.Remove(RegistrationCacheKey(userId));

        var result = await _fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = response,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = async (args, token) =>
                !await _context.PasskeyCredentials.AnyAsync(credential => credential.CredentialId == args.CredentialId, token)
        }, cancellationToken);

        _context.PasskeyCredentials.Add(new PasskeyCredential
        {
            UserId = userId,
            CredentialId = result.Id,
            PublicKey = result.PublicKey,
            UserHandle = options.User.Id,
            SignatureCounter = result.SignCount
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AssertionOptions> BeginAssertionAsync(Guid userId, CancellationToken cancellationToken)
    {
        await GetActivePrivilegedUserAsync(userId, cancellationToken);
        var credentials = await _context.PasskeyCredentials
            .AsNoTracking()
            .Where(credential => credential.UserId == userId)
            .Select(credential => new PublicKeyCredentialDescriptor(credential.CredentialId))
            .ToListAsync(cancellationToken);

        if (credentials.Count == 0)
            throw new InvalidOperationException("No passkey is enrolled for this account.");

        var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = credentials,
            UserVerification = UserVerificationRequirement.Required
        });
        _cache.Set(AssertionCacheKey(userId), options, CeremonyLifetime);
        return options;
    }

    public async Task FinishAssertionAsync(Guid userId, AuthenticatorAssertionRawResponse response, CancellationToken cancellationToken)
    {
        if (!_cache.TryGetValue<AssertionOptions>(AssertionCacheKey(userId), out var options) || options is null)
            throw new InvalidOperationException("The passkey request has expired. Sign in again.");
        _cache.Remove(AssertionCacheKey(userId));

        var credentials = await _context.PasskeyCredentials
            .Where(credential => credential.UserId == userId)
            .ToListAsync(cancellationToken);
        var storedCredential = credentials.SingleOrDefault(credential => credential.CredentialId.SequenceEqual(response.RawId));
        if (storedCredential is null)
            throw new InvalidOperationException("This passkey is not registered for the account.");

        var result = await _fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = response,
            OriginalOptions = options,
            StoredPublicKey = storedCredential.PublicKey,
            StoredSignatureCounter = storedCredential.SignatureCounter,
            IsUserHandleOwnerOfCredentialIdCallback = (args, _) =>
                Task.FromResult(args.UserHandle.SequenceEqual(storedCredential.UserHandle) && args.CredentialId.SequenceEqual(storedCredential.CredentialId))
        }, cancellationToken);

        storedCredential.SignatureCounter = result.SignCount;
        storedCredential.LastUsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetCredentialsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var credentials = await _context.PasskeyCredentials.Where(credential => credential.UserId == userId).ToListAsync(cancellationToken);
        _context.PasskeyCredentials.RemoveRange(credentials);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetActivePrivilegedUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await _context.Users.SingleOrDefaultAsync(user => user.Id == userId && user.IsActive && (user.Role == UserRole.Admin || user.Role == UserRole.HR), cancellationToken)
        ?? throw new InvalidOperationException("This account is not eligible for a passkey.");

    private static Fido2User ToFidoUser(User user) => new()
    {
        Id = user.Id.ToByteArray(),
        Name = user.Email,
        DisplayName = user.Role.ToString()
    };

    private static string RegistrationCacheKey(Guid userId) => $"passkey:registration:{userId:N}";
    private static string AssertionCacheKey(Guid userId) => $"passkey:assertion:{userId:N}";
}
