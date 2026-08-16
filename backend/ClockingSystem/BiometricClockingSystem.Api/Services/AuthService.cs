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
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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
            RequirePasswordChange = true
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

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
            return null;

        bool validPassword = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

        if (!validPassword)
            return null;

        string token = GenerateJwtToken(user);

        return new LoginResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role,
            MustChangePassword = user.RequirePasswordChange
        };
    }

    public async Task<LoginResponseDto?> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user =>
            user.Id == userId && user.IsActive && user.Role == UserRole.HR);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return null;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.RequirePasswordChange = false;
        await _context.SaveChangesAsync();

        return new LoginResponseDto
        {
            Token = GenerateJwtToken(user),
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role,
            MustChangePassword = false
        };
    }

    //token generation
    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role.ToString()),
        new Claim("password_change_required", user.RequirePasswordChange ? "true" : "false"),
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
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

   
}
