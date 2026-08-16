using System.ComponentModel.DataAnnotations;

namespace BiometricClockingSystem.Api.DTOs;

public sealed class CreateHrAccountDto
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    public string TemporaryPassword { get; init; } = string.Empty;
}
