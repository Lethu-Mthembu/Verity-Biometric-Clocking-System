using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiometricClockingSystem.Api.Models
{
    // Created whenever an employee taps "Call Administrator" because facial
    // recognition failed. This is what feeds the administrator's queue of
    // people currently needing a fingerprint override.
    public class OverrideRequest
    {
        [Key]
        public int OverrideRequestId { get; set; }

        [Required]
        public String EmployeeId { get; set; }= string.Empty;

        [ForeignKey(nameof(EmployeeId))]
        public Employee Employee { get; set; } = null!;

        public ClockType RequestedClockType { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public OverrideRequestStatus Status { get; set; } = OverrideRequestStatus.Pending;

        public string Reason { get; set; } = string.Empty;

        // Filled in once an administrator has resolved the request via fingerprint override.
        public string? ResolvedByAdminUsername { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
