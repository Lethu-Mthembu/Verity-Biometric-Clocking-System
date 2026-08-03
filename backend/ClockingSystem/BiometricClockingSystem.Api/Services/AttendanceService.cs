using BiometricClockingSystem.Api.Data;
using BiometricClockingSystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BiometricClockingSystem.Api.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _context;

        public AttendanceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Attendance> ClockInAsync(string employeeId, ClockAuthMethod authMethod, string? performedBy = null, string? notes = null)
        {
            var activeSession = await _context.Attendances
                .FirstOrDefaultAsync(s => s.EmployeeNumber == employeeId && s.IsActive);

            if (activeSession != null)
            {
                activeSession.IsActive = false;
                activeSession.ClockOut = DateTime.UtcNow;
                activeSession.ClockOutAuthMethod = authMethod;
                activeSession.UpdatedAt = DateTime.UtcNow;
                activeSession.Notes = string.IsNullOrWhiteSpace(notes) ? "Clocked in again while another session was active." : notes;
            }

            var session = new Attendance
            {
                EmployeeNumber = employeeId,
                ClockIn = DateTime.UtcNow,
                ClockOut = null,
                IsActive = true,
                ClockInAuthMethod = authMethod,
                PerformedBy = performedBy,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Attendances.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<Attendance?> ClockOutAsync(string employeeId, ClockAuthMethod authMethod, string? performedBy = null, string? notes = null)
        {
            var activeSession = await _context.Attendances
                .FirstOrDefaultAsync(s => s.EmployeeNumber == employeeId && s.IsActive);

            if (activeSession == null)
            {
                return null;
            }

            activeSession.ClockOut = DateTime.UtcNow;
            activeSession.IsActive = false;
            activeSession.ClockOutAuthMethod = authMethod;
            activeSession.PerformedBy = performedBy;
            activeSession.Notes = notes;
            activeSession.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return activeSession;
        }

        public async Task<Attendance?> GetActiveSessionAsync(string employeeId)
        {
            return await _context.Attendances
                .FirstOrDefaultAsync(s => s.EmployeeNumber == employeeId && s.IsActive);
        }
    }
}
