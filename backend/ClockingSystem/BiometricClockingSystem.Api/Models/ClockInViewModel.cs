using System.ComponentModel.DataAnnotations;

namespace BiometricClockingSystem.Api.Models
{
    public class ClockInViewModel
    {
        [Required]
        [Display(Name = "Employee ID")]
        public string? EmployeeNumber { get; set; }

        public ClockType ClockType { get; set; }

        // Populated by the webcam capture JS on the kiosk page.
        public string? ScannedFaceImageBase64 { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
