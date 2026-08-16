using BiometricClockingSystem.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BiometricClockingSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AttendanceController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Returns the complete attendance history for the HR and admin dashboards.
    [HttpGet("logs")]
    [Authorize(Roles = "Admin,HR", Policy = "PasswordReady")]
    public async Task<IActionResult> GetLogs(CancellationToken cancellationToken)
    {
        var logs = await _context.Attendances
            .AsNoTracking()
            .OrderByDescending(attendance => attendance.ClockIn)
            .Select(attendance => new
            {
                attendanceId = attendance.AttendanceId,
                employeeNumber = attendance.EmployeeNumber,
                employeeName = attendance.Employee == null
                    ? "Unknown employee"
                    : $"{attendance.Employee.FirstName} {attendance.Employee.LastName}",
                department = attendance.Employee == null
                    ? string.Empty
                    : attendance.Employee.Department,
                clockIn = attendance.ClockIn,
                clockOut = attendance.ClockOut,
                isActive = attendance.IsActive,
                clockInAuthMethod = attendance.ClockInAuthMethod,
                clockOutAuthMethod = attendance.ClockOutAuthMethod,
                notes = attendance.Notes,
                performedBy = attendance.PerformedBy,
                createdAt = attendance.CreatedAt,
                updatedAt = attendance.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(logs);
    }
}
