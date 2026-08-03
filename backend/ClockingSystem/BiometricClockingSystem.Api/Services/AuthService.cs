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

    //register user
    public async Task<bool> RegisterAsync(RegisterDto dto)
    {
        var exists = await _context.Users
            .AnyAsync(x => x.Email == dto.Email);

        if (exists)
            return false;

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsActive = true,
            Role = dto.Role
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return true;
    }

    //login user
    public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users
         .FirstOrDefaultAsync(x =>
             x.Email == dto.Email &&
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
            Role = user.Role
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