namespace BiometricClockingSystem.Api.Models;

public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public byte[]? FingerprintTemplate { get; set; }


    // Convenience flag - true once a fingerprint has actually been captured.
    public bool FingerprintEnrolled => FingerprintTemplate != null && FingerprintTemplate.Length > 0;


    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}