namespace BiometricClockingSystem.Api.DTOs.Employee;

// This DTO intentionally excludes face images and descriptors. Biometric
// templates must never be returned to a browser, including to an admin.
public sealed class EmployeeResponseDto
{
    public string EmployeeNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string Position { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
