namespace BiometricClockingSystem.Api.Models;

// Only public WebAuthn material is stored. The private key stays in Windows
// Hello, Google Password Manager, iCloud Keychain, or the user's authenticator.
public sealed class PasskeyCredential
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public byte[] CredentialId { get; set; } = Array.Empty<byte>();
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();
    public byte[] UserHandle { get; set; } = Array.Empty<byte>();
    public uint SignatureCounter { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}
