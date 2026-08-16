using System.ComponentModel.DataAnnotations;

namespace BiometricClockingSystem.Api.DTOs;

public sealed class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    public string NewPassword { get; init; } = string.Empty;
}
