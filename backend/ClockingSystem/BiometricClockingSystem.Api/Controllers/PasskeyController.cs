using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BiometricClockingSystem.Api.Data;
using BiometricClockingSystem.Api.Models;
using BiometricClockingSystem.Api.Services;
using Fido2NetLib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BiometricClockingSystem.Api.Controllers;

[ApiController]
[Route("api/passkeys")]
public sealed class PasskeyController : ControllerBase
{
    private readonly IPasskeyService _passkeys;
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public PasskeyController(IPasskeyService passkeys, IAuthService authService, ApplicationDbContext context, IAuditService auditService) =>
        (_passkeys, _authService, _context, _auditService) = (passkeys, authService, context, auditService);

    // A password-verified account with no passkey receives a five-minute,
    // enrolment-only token. It cannot access Admin or HR data.
    [Authorize(Roles = "PasskeySetup")]
    [EnableRateLimiting("privileged")]
    [HttpPost("registration/options")]
    public async Task<IActionResult> RegistrationOptions(CancellationToken cancellationToken) =>
        Ok(await _passkeys.BeginRegistrationAsync(CurrentUserId(), cancellationToken));

    [Authorize(Roles = "PasskeySetup")]
    [EnableRateLimiting("privileged")]
    [HttpPost("registration/result")]
    public async Task<IActionResult> RegistrationResult([FromBody] AuthenticatorAttestationRawResponse response, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        await _passkeys.FinishRegistrationAsync(userId, response, cancellationToken);
        var login = await _authService.CompletePasskeyChallengeAsync(userId, enrolledNewPasskey: true);
        return login is null ? Unauthorized() : Ok(login);
    }

    // Password verification issues a distinct five-minute challenge token;
    // this endpoint accepts only that token, never a normal Admin/HR session.
    [Authorize(Roles = "PasskeyChallenge")]
    [EnableRateLimiting("privileged")]
    [HttpPost("assertion/options")]
    public async Task<IActionResult> AssertionOptions(CancellationToken cancellationToken) =>
        Ok(await _passkeys.BeginAssertionAsync(CurrentUserId(), cancellationToken));

    [Authorize(Roles = "PasskeyChallenge")]
    [EnableRateLimiting("privileged")]
    [HttpPost("assertion/result")]
    public async Task<IActionResult> AssertionResult([FromBody] AuthenticatorAssertionRawResponse response, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        await _passkeys.FinishAssertionAsync(userId, response, cancellationToken);
        var login = await _authService.CompletePasskeyChallengeAsync(userId, enrolledNewPasskey: false);
        return login is null ? Unauthorized() : Ok(login);
    }

    // The sole Admin cannot reset their own passkey in the app. HR passkeys
    // are reset by the Admin, after which HR signs in with their password and
    // enrols a replacement passkey.
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("privileged")]
    [HttpPost("hr/reset")]
    public async Task<IActionResult> ResetHrPasskey(CancellationToken cancellationToken)
    {
        var hrUser = await _context.Users.SingleOrDefaultAsync(user => user.IsActive && user.Role == UserRole.HR, cancellationToken);
        if (hrUser is null) return NotFound(new { message = "No active HR account exists." });

        await _passkeys.ResetCredentialsAsync(hrUser.Id, cancellationToken);
        hrUser.SecurityStamp = Guid.NewGuid().ToString("N");
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("HrPasskeyReset", "User", hrUser.Id.ToString(), "An administrator reset the HR passkey.");
        return NoContent();
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(value, out var userId)) return userId;
        throw new UnauthorizedAccessException("Invalid authenticated user.");
    }
}
