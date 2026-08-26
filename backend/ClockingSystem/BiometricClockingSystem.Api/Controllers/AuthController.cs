using BiometricClockingSystem.Api.DTOs;
//using BiometricClockingSystem.Api.Interfaces;
using  BiometricClockingSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BiometricClockingSystem.Api.Models;

namespace BiometricClockingSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _authService = authService;
        _environment = environment;
        _configuration = configuration;
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

        IssueSessionCookie(result.Token);
        return Ok(ToSessionResponse(result));
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

        IssueSessionCookie(result.Token);
        return Ok(ToSessionResponse(result));
    }

    [Authorize]
    [HttpGet("session")]
    public IActionResult Session()
    {
        var roleValue = User.FindFirstValue(ClaimTypes.Role);
        var csrfToken = User.FindFirstValue("csrf");
        if (!Enum.TryParse<UserRole>(roleValue, out var role) || string.IsNullOrWhiteSpace(csrfToken))
            return Unauthorized(new { message = "Invalid authentication session." });

        return Ok(new AuthSessionDto
        {
            Role = role,
            MustChangePassword = User.HasClaim("password_change_required", "true"),
            CsrfToken = csrfToken
        });
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
        DeleteSessionCookie();
        return NoContent();
    }

    private AuthSessionDto ToSessionResponse(LoginResponseDto response) => new()
    {
        Role = response.Role,
        MustChangePassword = response.MustChangePassword,
        CsrfToken = response.CsrfToken
    };

    private void IssueSessionCookie(string token) => Response.Cookies.Append("verity_session", token, new CookieOptions
    {
        HttpOnly = true,
        Secure = !_environment.IsDevelopment(),
        SameSite = _environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
        Path = "/",
        IsEssential = true,
        MaxAge = TimeSpan.FromMinutes(GetAccessTokenLifetimeMinutes())
    });

    private void DeleteSessionCookie() => Response.Cookies.Delete("verity_session", new CookieOptions
    {
        Secure = !_environment.IsDevelopment(),
        SameSite = _environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
        Path = "/"
    });

    private int GetAccessTokenLifetimeMinutes()
    {
        var configured = _configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 30;
        return Math.Clamp(configured, 5, 60);
    }
}
