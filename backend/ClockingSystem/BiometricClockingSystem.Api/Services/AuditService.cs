using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BiometricClockingSystem.Api.Data;
using BiometricClockingSystem.Api.Models;

namespace BiometricClockingSystem.Api.Services;

public sealed class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<AuditService> logger) =>
        (_context, _httpContextAccessor, _logger) = (context, httpContextAccessor, logger);

    public async Task RecordAsync(string action, string targetType, string? targetId = null, string? details = null, Guid? actorUserId = null, string? actorEmail = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;
            var subject = user?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (actorUserId is null && Guid.TryParse(subject, out var parsedActorId)) actorUserId = parsedActorId;
            actorEmail ??= user?.FindFirstValue(ClaimTypes.Email);

            _context.AuditLogs.Add(new AuditLog
            {
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                Details = details,
                ActorUserId = actorUserId,
                ActorEmail = actorEmail,
                ClientIpAddress = httpContext?.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            // Auditing must never make a successful attendance operation fail.
            _logger.LogError(exception, "Could not record audit action {Action}", action);
        }
    }
}
