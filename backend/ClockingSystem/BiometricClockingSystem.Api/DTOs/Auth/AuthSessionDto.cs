using BiometricClockingSystem.Api.Models;

namespace BiometricClockingSystem.Api.DTOs;

// This is the only authentication state returned to browser JavaScript.
// The signed JWT itself is issued only as an HttpOnly cookie.
public sealed class AuthSessionDto
{
    public UserRole Role { get; init; }
    public bool MustChangePassword { get; init; }
    public string CsrfToken { get; init; } = string.Empty;
}
