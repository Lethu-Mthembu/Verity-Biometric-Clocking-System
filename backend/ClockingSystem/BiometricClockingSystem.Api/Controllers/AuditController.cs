using BiometricClockingSystem.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BiometricClockingSystem.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("privileged")]
public sealed class AuditController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuditController(ApplicationDbContext context) => _context = context;

    // Deliberately capped. This is an investigation view, not an unrestricted
    // export of staff activity or IP data.
    [HttpGet]
    public async Task<IActionResult> GetRecent([FromQuery] int take = 100)
    {
        take = Math.Clamp(take, 1, 200);
        var records = await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(log => log.OccurredAt)
            .Take(take)
            .Select(log => new
            {
                log.Id,
                log.OccurredAt,
                log.ActorEmail,
                log.Action,
                log.TargetType,
                log.TargetId,
                log.ClientIpAddress,
                log.Details
            })
            .ToListAsync();

        return Ok(records);
    }
}
