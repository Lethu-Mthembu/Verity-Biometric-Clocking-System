using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using BiometricClockingSystem.Api.Data;
using BiometricClockingSystem.Api.Models;
using BiometricClockingSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BiometricClockingSystem.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminOverrideController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAttendanceService _attendanceService;
    private readonly AdminNotificationService _notifications;

    public AdminOverrideController(
        ApplicationDbContext context,
        IAttendanceService attendanceService,
        AdminNotificationService notifications)
    {
        _context = context;
        _attendanceService = attendanceService;
        _notifications = notifications;
    }

    // This is intentionally anonymous: an employee at the kiosk has not yet
    // authenticated. The requested clock direction is determined from the
    // employee's current active attendance session.
    [AllowAnonymous]
    [HttpPost("notify")]
    public async Task<IActionResult> Notify([FromBody] NotifyAdminRequest request)
    {
        var employeeNumber = request.EmployeeNumber?.Trim();
        if (string.IsNullOrWhiteSpace(employeeNumber))
            return BadRequest(new { success = false, message = "Employee number is required." });

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber && e.IsActive);

        if (employee == null)
            return NotFound(new { success = false, message = "Employee not found." });

        var overrideRequest = new OverrideRequest
        {
            EmployeeId = employee.EmployeeNumber,
            RequestedClockType = await _context.Attendances
                .AsNoTracking()
                .AnyAsync(attendance => attendance.EmployeeNumber == employee.EmployeeNumber && attendance.IsActive)
                    ? ClockType.ClockOut
                    : ClockType.ClockIn,
            RequestedAt = DateTime.UtcNow,
            Status = OverrideRequestStatus.Pending,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Face recognition failed." : request.Reason.Trim()
        };

        _context.OverrideRequests.Add(overrideRequest);
        await _context.SaveChangesAsync();

        _notifications.Publish(new AdminOverrideNotification(
            overrideRequest.OverrideRequestId,
            overrideRequest.EmployeeId,
            overrideRequest.RequestedClockType.ToString(),
            overrideRequest.RequestedAt));

        return Ok(new
        {
            success = true,
            overrideRequestId = overrideRequest.OverrideRequestId,
            requestedClockType = overrideRequest.RequestedClockType
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("override-requests")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var pending = await _context.OverrideRequests
            .Where(r => r.Status == OverrideRequestStatus.Pending)
            .OrderBy(r => r.RequestedAt)
            .Select(r => new
            {
                r.OverrideRequestId,
                EmployeeNumber = r.EmployeeId,
                r.RequestedClockType,
                r.RequestedAt,
                r.Reason
            })
            .ToListAsync();

        return Ok(pending);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var subscription = _notifications.Subscribe();
        try
        {
            await foreach (var notification in subscription.Reader.ReadAllAsync(cancellationToken))
            {
                await Response.WriteAsync("event: override-request\n", cancellationToken);
                await Response.WriteAsync($"data: {JsonSerializer.Serialize(notification)}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The browser closed the EventSource connection.
        }
        finally
        {
            _notifications.Unsubscribe(subscription.Id);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("override-requests/{id:int}/resolve")]
    public async Task<IActionResult> Resolve(int id)
    {
        var adminIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(adminIdValue, out var adminId))
            return Unauthorized(new { success = false, message = "Invalid administrator identity." });

        var admin = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == adminId && u.IsActive && u.Role == UserRole.Admin);
        if (admin == null)
            return Forbid();

        var overrideRequest = await _context.OverrideRequests
            .FirstOrDefaultAsync(r => r.OverrideRequestId == id && r.Status == OverrideRequestStatus.Pending);
        if (overrideRequest == null)
            return NotFound(new { success = false, message = "No pending request found." });

        var notes = "Approved through Windows Hello administrator confirmation.";
        var attendance = overrideRequest.RequestedClockType == ClockType.ClockOut
            ? await _attendanceService.ClockOutAsync(
                overrideRequest.EmployeeId,
                ClockAuthMethod.FingerprintOverride,
                admin.Email,
                notes)
            : await _attendanceService.ClockInAsync(
                overrideRequest.EmployeeId,
                ClockAuthMethod.FingerprintOverride,
                admin.Email,
                notes);

        if (attendance is null)
            return Conflict(new
            {
                success = false,
                message = "The employee has no active clock-in session to close."
            });

        overrideRequest.Status = OverrideRequestStatus.Resolved;
        overrideRequest.ResolvedByAdminUsername = admin.Email;
        overrideRequest.ResolvedAt = DateTime.UtcNow;

        _context.AdminOverrides.Add(new AdminOverride
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = overrideRequest.EmployeeId,
            AdminId = admin.Id,
            CreatedAt = DateTime.UtcNow,
            Successful = true
        });

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Override approved.", attendanceId = attendance.AttendanceId });
    }

    public sealed class NotifyAdminRequest
    {
        public string? EmployeeNumber { get; init; }
        public string? Reason { get; init; }
    }
}
