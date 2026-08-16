using BiometricClockingSystem.Api.DTOs;
//using BiometricClockingSystem.Api.Interfaces;
using  BiometricClockingSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BiometricClockingSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // User registration is deliberately not public. Employees use biometric
    // enrollment only; the one HR login is created by an authenticated admin.
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("privileged")]
    [HttpPost("hr-account")]
    public async Task<IActionResult> CreateHrAccount(CreateHrAccountDto dto)
    {
        var result = await _authService.CreateHrAccountAsync(dto);

        if (!result.Succeeded)
            return Conflict(new { message = result.Error });

        return StatusCode(StatusCodes.Status201Created, new { message = "HR account created. The HR user must change the temporary password after signing in." });
    }

    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("privileged")]
    [HttpGet("hr-account")]
    public async Task<ActionResult<HrAccountStatusDto>> GetHrAccountStatus() =>
        Ok(await _authService.GetHrAccountStatusAsync());

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        if (result == null)
            return Unauthorized("Invalid email or password.");

        return Ok(result);
    }

    [Authorize(Roles = "HR")]
    [EnableRateLimiting("privileged")]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized(new { message = "Invalid user identity." });

        var result = await _authService.ChangePasswordAsync(userId, dto);
        if (result is null)
            return BadRequest(new { message = "Current password is incorrect." });

        return Ok(result);
    }

    [Authorize]
    [EnableRateLimiting("privileged")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized(new { message = "Invalid user identity." });

        await _authService.LogoutAsync(userId);
        return NoContent();
    }
}
