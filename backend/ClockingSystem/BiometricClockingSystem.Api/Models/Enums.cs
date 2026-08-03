namespace BiometricClockingSystem.Api.Models
{
    // Whether the employee is clocking in or clocking out.
    public enum ClockType
    {
        ClockIn,
        ClockOut
    }

    // How a clocking action was authenticated - useful for audit trails,
    // so you can see later whether an entry came from the employee's own
    // face scan or from an administrator's fingerprint override.
    public enum ClockAuthMethod
    {
        Face,
        FingerprintOverride
    }

    // Status of a "call administrator" request raised by an employee.
    public enum OverrideRequestStatus
    {
        Pending,
        Resolved,
        Cancelled
    }
}
