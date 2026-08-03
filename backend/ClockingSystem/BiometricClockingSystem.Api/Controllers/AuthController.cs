using BiometricClockingSystem.Api.DTOs;
//using BiometricClockingSystem.Api.Interfaces;
using  BiometricClockingSystem.Api.Services;
using Microsoft.AspNetCore.Mvc;

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

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var success = await _authService.RegisterAsync(dto);

        if (!success)
            return BadRequest("Email already exists.");

        return Ok("Registration successful.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        if (result == null)
            return Unauthorized("Invalid email or password.");

        return Ok(result);
    }
}