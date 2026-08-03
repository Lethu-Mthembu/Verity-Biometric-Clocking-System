using BiometricClockingSystem.Api.Models;

namespace BiometricClockingSystem.Api.DTOs;

public class RegisterDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Admin;
}