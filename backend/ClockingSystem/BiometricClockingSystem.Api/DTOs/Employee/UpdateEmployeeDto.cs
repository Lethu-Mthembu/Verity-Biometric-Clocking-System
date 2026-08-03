namespace BiometricClockingSystem.Api.DTOs.Employee;

public class UpdateEmployeeDto
{
    public string FirstName { get; set; } = "";

    public string LastName { get; set; } = "";

    public string Department { get; set; } = "";

    public string Position { get; set; } = "";

    public string PhoneNumber { get; set; } = "";

    public string Email { get; set; } = "";

    public string FaceImageBase64 { get; set; } = string.Empty;

    public float[] FaceDescriptor { get; set; } = Array.Empty<float>();

}