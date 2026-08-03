using System.ComponentModel.DataAnnotations;
namespace BiometricClockingSystem.Api.Models;

public class Employee
{
    [Key]
    [StringLength(8)]
    [Display(Name = "Employee ID")]
    public string EmployeeNumber { get; set; } = string.Empty;


    [Required(ErrorMessage = "Name is required.")]
    [StringLength(150)]
    [Display(Name = "Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(150)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;


    [Required(ErrorMessage = "Department is required.")]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    public string Position { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone]
    [Display(Name = "Phone Number")]

    public string PhoneNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A facial photo capture is required to register an employee.")]
    public byte[]? FaceImage { get; set; }

    // MIME type of FacialImage so it can be displayed correctly later
    // (e.g. "image/jpeg"). Defaults to jpeg since that's what the webcam
    // capture in the registration view produces.
    public string FacialImageContentType { get; set; } = "image/jpeg";

    // 128-value descriptor generated locally by the kiosk browser.
    public float[] FaceDescriptor { get; set; } = Array.Empty<float>();
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- Registration audit info ----

    public DateTime RegisteredOn { get; set; } = DateTime.UtcNow;

    // Username of the admin who performed this registration - useful for audit trails.
    [StringLength(100)]
    public string RegisteredByAdminUsername { get; set; } = string.Empty;


}