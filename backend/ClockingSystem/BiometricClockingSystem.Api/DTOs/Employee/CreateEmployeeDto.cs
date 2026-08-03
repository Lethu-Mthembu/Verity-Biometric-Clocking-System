namespace BiometricClockingSystem.Api.DTOs.Employee;

public class CreateEmployeeDto
{
    public string EmployeeNumber { get; set; } = "";
    public string FirstName { get; set; } = "";

    public string LastName { get; set; } = "";

    public string Department { get; set; } = "";

    public string Position { get; set; } = "";

    public string PhoneNumber { get; set; } = "";

    public string Email { get; set; } = "";

        // Webcam image from React
    public string FaceImageBase64 { get; set; } = string.Empty;

    // 128-value descriptor generated client-side by face-api.js during onboarding.
    public float[] FaceDescriptor { get; set; } = Array.Empty<float>();
}