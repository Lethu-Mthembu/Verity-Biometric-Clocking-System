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

    // HR accounts are created by an administrator with a one-time temporary
    // password. Their JWT is restricted until they choose a private password.
    public bool RequirePasswordChange { get; set; }

    // Rotated whenever the account password changes or the user signs out.
    // Tokens carry this value, allowing server-side revocation without a
    // separate token blacklist.
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    public int FailedLoginCount { get; set; }

    public DateTime? LockoutEndUtc { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
