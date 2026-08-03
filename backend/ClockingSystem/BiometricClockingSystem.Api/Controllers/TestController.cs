using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiometricClockingSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("public")]
    public IActionResult Public()
    {
        return Ok("Everyone can access this.");
    }

    [Authorize]
    [HttpGet("private")]
    public IActionResult Private()
    {
        return Ok("You are logged in.");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public IActionResult Admin()
    {
        return Ok("Welcome Admin!");
    }

    [Authorize]
    [HttpGet("employee")]
    public IActionResult Employee()
    {
        return Ok("Welcome Employee!");
    }
}