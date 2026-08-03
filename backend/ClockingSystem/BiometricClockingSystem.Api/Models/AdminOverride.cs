namespace BiometricClockingSystem.Api.Models;
public class AdminOverride
{
    public Guid Id { get; set; }

    public string EmployeeNumber { get; set; } = string.Empty;

    public Employee Employee { get; set; } = null!;

    public Guid AdminId { get; set; }

    public User Admin { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string Reason { get; set; } = string.Empty;

    public bool Successful { get; set; }
}