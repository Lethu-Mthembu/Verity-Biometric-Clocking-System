using BiometricClockingSystem.Api.Models;

namespace BiometricClockingSystem.Api.Services
{
    public interface IAttendanceService
    {
        Task<Attendance> ClockInAsync(string employeeId, ClockAuthMethod authMethod, string? performedBy = null, string? notes = null);
        Task<Attendance?> ClockOutAsync(string employeeId, ClockAuthMethod authMethod, string? performedBy = null, string? notes = null);
        Task<Attendance?> GetActiveSessionAsync(string   employeeId);
    }
}
