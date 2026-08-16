using BiometricClockingSystem.Api.Data;
using BiometricClockingSystem.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BiometricClockingSystem.Api.DTOs.Employee;
using BiometricClockingSystem.Api.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace BiometricClockingSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeController : ControllerBase
{
    private const int MaxFaceImageBytes = 5 * 1024 * 1024;
    private const int MaxFaceUploadBytes = 7 * 1024 * 1024;
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public EmployeeController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    //GET ALL
    // GET: api/Employee
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetEmployees()
    {
        var employees = await _context.Employees
             .AsNoTracking()
             .Where(e => e.IsActive)
             .Select(e => new EmployeeResponseDto
             {
                 EmployeeNumber = e.EmployeeNumber,
                 FirstName = e.FirstName,
                 LastName = e.LastName,
                 Department = e.Department,
                 Position = e.Position,
                 PhoneNumber = e.PhoneNumber,
                 Email = e.Email,
                 IsActive = e.IsActive,
                 CreatedAt = e.CreatedAt
             })
             .ToListAsync();
        return Ok(employees);
    }

    //FIND EMPLOYEE
    // GET: api/Employee/{id}
    [Authorize(Roles = "Admin")]
    [HttpGet("number/{employeeNumber}")]
    public async Task<IActionResult> GetByEmployeeNumber(string employeeNumber)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e =>
                e.EmployeeNumber == employeeNumber &&
                e.IsActive);

        if (employee == null)
            return NotFound(new
            {
                success = false,
                message = "Employee not found."
            });

        return Ok(ToResponseDto(employee));
    }


    //CREATE: api/Employee
    // POST: api/Employee
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("privileged")]
    [HttpPost]
    [Consumes("application/json")]
    [RequestSizeLimit(MaxFaceUploadBytes)]
    public async Task<IActionResult> CreateEmployee(CreateEmployeeDto dto)
    {
        if (!IsValidDescriptor(dto.FaceDescriptor))
            return BadRequest(new { success = false, message = "A valid 128-value face descriptor is required." });

        byte[] faceImage;

        try
        {
            faceImage = ConvertBase64ToBytes(dto.FaceImageBase64);
        }

        catch (FormatException)
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid face image."
            });
        }

        if (faceImage.Length == 0 || faceImage.Length > MaxFaceImageBytes)
            return BadRequest(new { success = false, message = "Face image must be between 1 byte and 5 MB." });

        var employeeNumber = await GenerateEmployeeNumberAsync();
        var employee = new Employee
        {
            EmployeeNumber = employeeNumber,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Department = dto.Department,
            Position = dto.Position,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            FaceDescriptor = dto.FaceDescriptor,
            FaceImage = faceImage,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            RegisteredOn = DateTime.UtcNow,
            RegisteredByAdminUsername = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty
        };

        _context.Employees.Add(employee);

        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("EmployeeCreated", "Employee", employee.EmployeeNumber);

        return Ok(new
        {
            success = true,
            message = "Employee registered successfully.",
            employeeNumber = employee.EmployeeNumber
        });
    }

    //UPDATE: api/Employee/{id}
    // PUT: api/Employee/{id}
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("privileged")]
    [HttpPut("{employeeNumber}")]
    [Consumes("application/json")]
    [RequestSizeLimit(MaxFaceUploadBytes)]
    public async Task<IActionResult> UpdateEmployee(String employeeNumber, UpdateEmployeeDto dto)
    {
        var employee = await _context.Employees
                 .FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber);

        if (employee == null)
            return NotFound();

        if (!IsValidDescriptor(dto.FaceDescriptor))
            return BadRequest(new { success = false, message = "A valid 128-value face descriptor is required." });

        byte[] faceImageBytes;
        try
        {
            faceImageBytes = ConvertBase64ToBytes(dto.FaceImageBase64);
        }

        catch (FormatException)
        {
            return BadRequest(new { success = false, message = "Invalid face image." });
        }

        if (faceImageBytes.Length == 0 || faceImageBytes.Length > MaxFaceImageBytes)
            return BadRequest(new { success = false, message = "Face image must be between 1 byte and 5 MB." });

        employee.FirstName = dto.FirstName;
        employee.LastName = dto.LastName;
        employee.Department = dto.Department;
        employee.Position = dto.Position;
        employee.PhoneNumber = dto.PhoneNumber;
        employee.Email = dto.Email;
        employee.FaceImage = faceImageBytes;
        employee.FaceDescriptor = dto.FaceDescriptor;

        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("EmployeeUpdated", "Employee", employee.EmployeeNumber);

        return Ok(new
        {
            success = true,
            message = "Employee updated successfully.",
            employee = ToResponseDto(employee)
        });
    }

    // DELETE: api/Employee/{id}
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("privileged")]
    [HttpDelete("{employeeNumber}")]
    public async Task<IActionResult> DeleteEmployee(String employeeNumber)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber);

        if (employee == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Employee not found."
            });
        }
        employee.IsActive = false;

        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("EmployeeDeactivated", "Employee", employee.EmployeeNumber);

        return Ok(new
        {
            success = true,
            message = "Employee deactivated successfully."
        });
    }

    private static byte[] ConvertBase64ToBytes(string base64)
    {
        const string jpegPrefix = "data:image/jpeg;base64,";
        if (!base64.StartsWith(jpegPrefix, StringComparison.OrdinalIgnoreCase))
            throw new FormatException("Only JPEG webcam captures are accepted.");

        var bytes = Convert.FromBase64String(base64[jpegPrefix.Length..]);
        if (bytes.Length < 3 || bytes[0] != 0xFF || bytes[1] != 0xD8 || bytes[2] != 0xFF)
            throw new FormatException("Invalid JPEG image.");

        return bytes;
    }

    private static bool IsValidDescriptor(float[]? descriptor) =>
        descriptor is { Length: 128 } && descriptor.All(float.IsFinite);

    private static EmployeeResponseDto ToResponseDto(Employee employee) => new()
    {
        EmployeeNumber = employee.EmployeeNumber,
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        Department = employee.Department,
        Position = employee.Position,
        PhoneNumber = employee.PhoneNumber,
        Email = employee.Email,
        IsActive = employee.IsActive,
        CreatedAt = employee.CreatedAt
    };

     // PUT THE METHOD HERE
    private async Task<string> GenerateEmployeeNumberAsync()
    {
        var random = new Random();

        string employeeNumber;

        do
        {
            int number = random.Next(1000, 10000);
            employeeNumber = $"EMP-{number}";
        }
        while (await _context.Employees.AnyAsync(
            e => e.EmployeeNumber == employeeNumber));

        return employeeNumber;
    }
}
