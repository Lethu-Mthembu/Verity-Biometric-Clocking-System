using Microsoft.EntityFrameworkCore;
using BiometricClockingSystem.Api.Data;
using BiometricClockingSystem.Api.Models;

namespace BiometricClockingSystem.Api.Services;

public class EmployeeService
{
    private readonly ApplicationDbContext _context;

    public EmployeeService(ApplicationDbContext context)
    {
        _context = context;
    }

   

    // Your other EmployeeService methods go below here
}