using BiometricClockingSystem.Api.DTOs;
namespace BiometricClockingSystem.Api.Services
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(RegisterDto dto);
        Task<LoginResponseDto?> LoginAsync(LoginDto dto);
    }
}