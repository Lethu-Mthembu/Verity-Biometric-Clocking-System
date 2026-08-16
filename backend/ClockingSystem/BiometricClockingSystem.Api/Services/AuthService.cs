using BiometricClockingSystem.Api.Data;
using BiometricClockingSystem.Api.DTOs;
//using BiometricClocking.API.Interfaces;
using BiometricClockingSystem.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BiometricClockingSystem.Api.Services;

public class AuthService : IAuthService
{
    private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword("unused-password", workFactor: 11);
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;

    public AuthService(ApplicationDbContext context, IConfiguration configuration, IAuditService auditService)
    {
        _context = context;
        _configuration = configuration;
        _auditService = auditService;
    }

    public async Task<AuthOperationResult> CreateHrAccountAsync(CreateHrAccountDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(user => user.Role == UserRole.HR))
            return new(false, "The HR account has already been configured.");

        if (await _context.Users.AnyAsync(user => user.Email == email))
            return new(false, "Email already exists.");

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.TemporaryPassword),
            IsActive = true,
            Role = UserRole.HR,
            RequirePasswordChange = true,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("HrAccountCreated", "User", user.Id.ToString(), "HR account created with a temporary password.");

        return new(true);
    }

    public async Task<HrAccountStatusDto> GetHrAccountStatusAsync()
    {
        var account = await _context.Users
            .AsNoTracking()
            .Where(user => user.Role == UserRole.HR && user.IsActive)
            .Select(user => new { user.Email })
            .SingleOrDefaultAsync();

        return account is null
            ? new(false, null)
            : new(true, account.Email);
    }

    //login user
    public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
         .FirstOrDefaultAsync(x =>
             x.Email == email &&
             x.IsActive);

        if (user == null)
        {
            // Maintain comparable password work for an unknown email to make
            // account enumeration by timing substantially harder.
            _ = BCrypt.Net.BCrypt.Verify(dto.Password, DummyPasswordHash);
            await _auditService.RecordAsync("LoginFailed", "Authentication", details: "Unknown or inactive account.");
            return null;
        }

        if (user.LockoutEndUtc is { } lockoutEnd && lockoutEnd > DateTime.UtcNow)
        {
            await _auditService.RecordAsync("LoginBlocked", "User", user.Id.ToString(), "Account is temporarily locked.", user.Id, user.Email);
            return null;
        }

        bool validPassword = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

        if (!validPassword)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= 5)
            {
                user.FailedLoginCount = 0;
                user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(15);
            }
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("LoginFailed", "User", user.Id.ToString(), "Invalid password.", user.Id, user.Email);
            return null;
        }

        if (string.IsNullOrWhiteSpace(user.SecurityStamp))
            user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        await _context.SaveChangesAsync();

        var hasPasskey = await _context.PasskeyCredentials.AnyAsync(credential => credential.UserId == user.Id);
        if (!hasPasskey)
        {
            await _auditService.RecordAsync("PasskeySetupRequired", "User", user.Id.ToString(), null, user.Id, user.Email);
            return BuildLoginResponse(user, "PasskeySetup", 5, passkeySetupRequired: true);
        }

        await _auditService.RecordAsync("PasswordVerified", "User", user.Id.ToString(), "Passkey verification required.", user.Id, user.Email);
        return BuildLoginResponse(user, "PasskeyChallenge", 5, requiresPasskey: true);
    }

    public async Task<LoginResponseDto?> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user =>
            user.Id == userId && user.IsActive && user.Role == UserRole.HR);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return null;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.RequirePasswordChange = false;
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("PasswordChanged", "User", user.Id.ToString(), null, user.Id, user.Email);

        return BuildLoginResponse(user);
    }

    public async Task<LoginResponseDto?> CompletePasskeyChallengeAsync(Guid userId, bool enrolledNewPasskey)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == userId && user.IsActive);
        if (user is null) return null;

        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync(
            enrolledNewPasskey ? "PasskeyEnrolled" : "PasskeyLoginSucceeded",
            "User",
            user.Id.ToString(),
            null,
            user.Id,
            user.Email);

        return BuildLoginResponse(user);
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == userId && user.IsActive);
        if (user is null) return;

        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("Logout", "User", user.Id.ToString(), null, user.Id, user.Email);
    }

    //token generation
    private LoginResponseDto BuildLoginResponse(User user, string? roleOverride = null, int? lifetimeMinutes = null, bool requiresPasskey = false, bool passkeySetupRequired = false) => new()
    {
        Token = GenerateJwtToken(user, roleOverride, lifetimeMinutes),
        UserId = user.Id,
        Email = user.Email,
        Role = user.Role,
        MustChangePassword = user.RequirePasswordChange,
        RequiresPasskey = requiresPasskey,
        PasskeySetupRequired = passkeySetupRequired
    };

    private string GenerateJwtToken(User user, string? roleOverride = null, int? lifetimeMinutes = null)
    {
        var claims = new[]
        {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, roleOverride ?? user.Role.ToString()),
        new Claim("password_change_required", user.RequirePasswordChange ? "true" : "false"),
        new Claim("security_stamp", user.SecurityStamp),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(lifetimeMinutes ?? GetAccessTokenLifetimeMinutes()),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetAccessTokenLifetimeMinutes()
    {
        var configured = _configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 30;
        return Math.Clamp(configured, 5, 60);
    }

   
}
