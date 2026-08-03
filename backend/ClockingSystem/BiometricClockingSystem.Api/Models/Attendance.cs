using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiometricClockingSystem.Api.Models;

public class Attendance
{
    [Key]
    public Guid AttendanceId { get; set; }
    [Required]
    public string EmployeeNumber { get; set; } = string.Empty;

    public Employee? Employee { get; set; } = null!;

    public DateTime ClockIn { get; set; }

    public DateTime? ClockOut { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    public ClockAuthMethod ClockInAuthMethod { get; set; }

    public ClockAuthMethod? ClockOutAuthMethod { get; set; }

    [StringLength(200)]
    public string? Notes { get; set; }

    [StringLength(100)]
    public string? PerformedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [NotMapped]
    public int? DurationMinutes => ClockOut.HasValue ? (int?)(ClockOut.Value - ClockIn).TotalMinutes : null;
}