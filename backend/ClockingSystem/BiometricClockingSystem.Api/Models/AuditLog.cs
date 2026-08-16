using System.ComponentModel.DataAnnotations;

namespace BiometricClockingSystem.Api.Models;

// Stores security-relevant actions without retaining passwords, OTPs, face
// images, descriptors, or request bodies.
public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Guid? ActorUserId { get; set; }

    [MaxLength(256)]
    public string? ActorEmail { get; set; }

    [Required, MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string TargetType { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? TargetId { get; set; }

    [MaxLength(64)]
    public string? ClientIpAddress { get; set; }

    [MaxLength(2000)]
    public string? Details { get; set; }
}
