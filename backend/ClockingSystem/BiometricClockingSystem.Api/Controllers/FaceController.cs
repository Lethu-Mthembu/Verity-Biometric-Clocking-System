using BiometricClockingSystem.Api.Data;
using BiometricClockingSystem.Api.Models;
using BiometricClockingSystem.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace BiometricClockingSystem.Api.Controllers;

[ApiController]
[Route("api/face")]
public sealed class FaceController : ControllerBase
{
    private const long MaxFaceImageBytes = 5 * 1024 * 1024;
    private readonly ApplicationDbContext _context;
    private readonly IFacialRecognitionService _facialRecognitionService;
    private readonly IOtpService _otpService;
    private readonly IAttendanceService _attendanceService;

    public FaceController(
        ApplicationDbContext context,
        IFacialRecognitionService facialRecognitionService,
        IOtpService otpService,
        IAttendanceService attendanceService)
    {
        _context = context;
        _facialRecognitionService = facialRecognitionService;
        _otpService = otpService;
        _attendanceService = attendanceService;
    }

    // Kiosk login. The browser creates a descriptor locally; this action only
    // compares descriptors and never forwards a photo to an external provider.
    [HttpPost("verify")]
    [AllowAnonymous]
    [EnableRateLimiting("kiosk")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFaceImageBytes)]
    public async Task<IActionResult> Verify(
        [FromForm] string? faceDescriptor,
        [FromForm] string? employeeNumber,
        [FromForm] ClockType clockType = ClockType.ClockIn)
    {
        if (!TryParseDescriptor(faceDescriptor, out var scannedDescriptor))
            return BadRequest(new { exists = false, matched = false, message = "A valid 128-value face descriptor is required." });

        return await VerifyDescriptorAsync(scannedDescriptor, employeeNumber, clockType);
    }

    [HttpPost("verify")]
    [AllowAnonymous]
    [EnableRateLimiting("kiosk")]
    [Consumes("application/json")]
    public async Task<IActionResult> VerifyJson([FromBody] FaceVerificationRequest request)
    {
        if (request.Descriptor is null || request.Descriptor.Length != 128 || !request.Descriptor.All(float.IsFinite))
            return BadRequest(new { exists = false, matched = false, message = "A valid 128-value face descriptor is required." });

        return await VerifyDescriptorAsync(request.Descriptor, request.EmployeeNumber, request.ClockType);
    }

    private async Task<IActionResult> VerifyDescriptorAsync(float[] scannedDescriptor, string? employeeNumber, ClockType clockType)
    {

        var employees = _context.Employees.AsNoTracking().Where(e => e.IsActive && e.FaceDescriptor.Length == scannedDescriptor.Length);
        if (!string.IsNullOrWhiteSpace(employeeNumber))
            employees = employees.Where(e => e.EmployeeNumber == employeeNumber.Trim());

        var candidates = await employees.ToListAsync();
        Employee? matchedEmployee = null;
        var bestConfidence = 0d;

        foreach (var employee in candidates)
        {
            var result = _facialRecognitionService.VerifyDescriptor(employee.FaceDescriptor, scannedDescriptor);
            if (result.IsMatch && result.Confidence > bestConfidence)
            {
                matchedEmployee = employee;
                bestConfidence = result.Confidence;
            }
        }

        if (matchedEmployee is null)
            return Ok(new { exists = false, matched = false, message = "Face not found." });

        // An active session means this face scan is a clock-out. Clock-out is
        // deliberately face-only; OTP is required only for clock-in.
        var hasActiveSession = await _context.Attendances
            .AsNoTracking()
            .AnyAsync(attendance => attendance.EmployeeNumber == matchedEmployee.EmployeeNumber && attendance.IsActive);

        if (hasActiveSession)
        {
            var session = await _attendanceService.ClockOutAsync(
                matchedEmployee.EmployeeNumber,
                ClockAuthMethod.Face);

            if (session is null)
                return Conflict(new { exists = true, matched = true, message = "No active clock-in session was found." });

            return Ok(new
            {
                exists = true,
                matched = true,
                clockType = ClockType.ClockOut,
                clockedOut = true,
                fname = matchedEmployee.FirstName,
                lastname = matchedEmployee.LastName,
                employeeId = matchedEmployee.EmployeeNumber,
                employeeNumber = matchedEmployee.EmployeeNumber,
                employee = new { id = matchedEmployee.EmployeeNumber, fname = matchedEmployee.FirstName, lastname = matchedEmployee.LastName, department = matchedEmployee.Department },
                confidence = Math.Round(bestConfidence, 4),
                otpSent = false,
                message = "Face verified. Clock-out recorded."
            });
        }

        OtpChallenge otpChallenge;
        try
        {
            otpChallenge = await _otpService.CreateAsync(matchedEmployee.EmployeeNumber, ClockType.ClockIn);
        }
        catch (OtpDeliveryException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { exists = true, matched = true, message = exception.Message });
        }

        return Ok(new
        {
            exists = true,
            matched = true,
            fname = matchedEmployee.FirstName,
            lastname = matchedEmployee.LastName,
            employeeId = matchedEmployee.EmployeeNumber,
            employeeNumber = matchedEmployee.EmployeeNumber,
            employee = new { id = matchedEmployee.EmployeeNumber, fname = matchedEmployee.FirstName, lastname = matchedEmployee.LastName, department = matchedEmployee.Department },
            confidence = Math.Round(bestConfidence, 4),
            clockType = ClockType.ClockIn,
            otpChallengeId = otpChallenge.Id,
            otpExpiresAt = otpChallenge.ExpiresAt,
            otpSent = true,
            message = "Face verified."
        });
    }

    private static bool TryParseDescriptor(string? value, out float[] descriptor)
    {
        descriptor = Array.Empty<float>();
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            descriptor = System.Text.Json.JsonSerializer.Deserialize<float[]>(value) ?? Array.Empty<float>();
            return descriptor.Length == 128 && descriptor.All(float.IsFinite);
        }
        catch (System.Text.Json.JsonException) { return false; }
    }

    public sealed class FaceVerificationRequest
    {
        public float[]? Descriptor { get; init; }
        public string? EmployeeNumber { get; init; }
        public ClockType ClockType { get; init; } = ClockType.ClockIn;
    }
}
