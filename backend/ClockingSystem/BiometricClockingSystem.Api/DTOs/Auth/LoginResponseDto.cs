namespace BiometricClockingSystem.Api.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    
    public Guid UserId { get; set; }

     public UserRole Role { get; set; } 

    public string Email { get; set; } = string.Empty;

    public bool MustChangePassword { get; set; }

    // Password verification is only the first step for privileged accounts.
    public bool RequiresPasskey { get; set; }
    public bool PasskeySetupRequired { get; set; }
}
