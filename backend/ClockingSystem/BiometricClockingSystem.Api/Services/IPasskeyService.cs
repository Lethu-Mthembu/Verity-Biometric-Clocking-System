using Fido2NetLib;

namespace BiometricClockingSystem.Api.Services;

public interface IPasskeyService
{
    Task<CredentialCreateOptions> BeginRegistrationAsync(Guid userId, CancellationToken cancellationToken);
    Task FinishRegistrationAsync(Guid userId, AuthenticatorAttestationRawResponse response, CancellationToken cancellationToken);
    Task<AssertionOptions> BeginAssertionAsync(Guid userId, CancellationToken cancellationToken);
    Task FinishAssertionAsync(Guid userId, AuthenticatorAssertionRawResponse response, CancellationToken cancellationToken);
    Task ResetCredentialsAsync(Guid userId, CancellationToken cancellationToken);
}
