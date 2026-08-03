namespace BiometricClockingSystem.Api.Models
{
    public class FingerprintOverrideViewModel
    {
        public string EmployeeNumber { get; set; } = string.Empty;
        public string EmployeeFullName { get; set; } = string.Empty;
        public ClockType ClockType { get; set; }

        // Populated by the fingerprint reader SDK bridge (admin's device).
        public string ScannedFingerprintBase64 { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }
}
