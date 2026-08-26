using BiometricClockingSystem.Api.DTOs;
namespace BiometricClockingSystem.Api.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginDto dto);
        Task<AuthOperationResult> CreateHrAccountAsync(CreateHrAccountDto dto);
        Task<LoginResponseDto?> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
        Task LogoutAsync(Guid userId);
        Task<HrAccountStatusDto> GetHrAccountStatusAsync();
    }

    public sealed record AuthOperationResult(bool Succeeded, string? Error = null);
    public sealed record HrAccountStatusDto(bool Configured, string? Email);
}
