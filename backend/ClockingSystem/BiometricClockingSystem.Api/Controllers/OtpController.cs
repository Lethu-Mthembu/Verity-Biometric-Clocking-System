using BiometricClockingSystem.Api.Data;
using BiometricClockingSystem.Api.Models;
using BiometricClockingSystem.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace BiometricClockingSystem.Api.Controllers;

[ApiController]
[Route("api/otp")]
public sealed class OtpController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IOtpService _otpService;
    private readonly IAttendanceService _attendanceService;
    public OtpController(ApplicationDbContext context, IOtpService otpService, IAttendanceService attendanceService) =>
        (_context, _otpService, _attendanceService) = (context, otpService, attendanceService);

    [AllowAnonymous]
    [EnableRateLimiting("otp")]
    [HttpPost("challenge")]
    public async Task<IActionResult> CreateChallenge([FromBody] CreateOtpChallengeRequest request)
    {
        if (request.ClockType != ClockType.ClockIn)
            return BadRequest(new { message = "OTP is only required for clock-in." });

        if (!await _context.Employees.AnyAsync(e => e.EmployeeNumber == request.EmployeeNumber && e.IsActive))
            return NotFound(new { message = "Active employee not found." });
        OtpChallenge challenge;
        try
        {
            challenge = await _otpService.CreateAsync(request.EmployeeNumber, request.ClockType);
        }
        catch (OtpDeliveryException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = exception.Message });
        }
        return Ok(new { challengeId = challenge.Id, expiresAt = challenge.ExpiresAt });
    }

    [AllowAnonymous]
    [EnableRateLimiting("otp")]
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyOtpRequest request)
    {
        // Read the challenge before verification consumes it so the verified
        // employee and requested clock action can be recorded afterwards.
        OtpChallenge challenge;
        if (request.ChallengeId != Guid.Empty)
        {
            if (!_otpService.TryGetChallenge(request.ChallengeId, out challenge))
                return BadRequest(new { valid = false, message = "The verification code has expired or is invalid." });
        }
        else if (!string.IsNullOrWhiteSpace(request.EmployeeNumber))
        {
            var employee = await _context.Employees.AsNoTracking()
                .FirstOrDefaultAsync(employee => employee.EmployeeNumber == request.EmployeeNumber && employee.IsActive);
            if (employee is null || !_otpService.TryGetChallengeForEmployee(employee.EmployeeNumber, out challenge))
                return BadRequest(new { valid = false, message = "The verification code has expired or is invalid." });
        }
        else
        {
            return BadRequest(new { valid = false, message = "The verification code has expired or is invalid." });
        }

        if (challenge.ClockType != ClockType.ClockIn)
            return BadRequest(new { valid = false, message = "OTP is only required for clock-in." });

        var code = string.IsNullOrWhiteSpace(request.Code) ? request.Otp : request.Code;
        var result = await _otpService.VerifyAsync(challenge.Id, code);
        if (!result.Succeeded) return BadRequest(new { valid = false, message = result.Error });
        var session = await _attendanceService.ClockInAsync(challenge.EmployeeId, ClockAuthMethod.Face);
        if (session is null) return Conflict(new { valid = false, message = "No active clock-in session was found." });
        return Ok(new { success = true, valid = true, employeeId = challenge.EmployeeId, clockType = challenge.ClockType, attendanceSessionId = session.AttendanceId });
    }

    public sealed class CreateOtpChallengeRequest { public string EmployeeNumber { get; init; } = string.Empty; public ClockType ClockType { get; init; } = ClockType.ClockIn; }
    public sealed class VerifyOtpRequest
    {
        public Guid ChallengeId { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Otp { get; init; } = string.Empty;
        public string? EmployeeNumber { get; init; }
    }
}
