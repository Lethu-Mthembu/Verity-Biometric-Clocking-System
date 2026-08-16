namespace BiometricClockingSystem.Api.Services;

public interface IAuditService
{
    Task RecordAsync(string action, string targetType, string? targetId = null, string? details = null, Guid? actorUserId = null, string? actorEmail = null);
}
