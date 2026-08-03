namespace BiometricClockingSystem.Api.Services;

public sealed class TwilioOptions
{
    public const string SectionName = "Twilio";

    public string AccountSid { get; init; } = string.Empty;
    public string AuthToken { get; init; } = string.Empty;
    public string FromPhoneNumber { get; init; } = string.Empty;
}

public sealed class OtpDeliveryException : Exception
{
    public OtpDeliveryException(string message) : base(message) { }
}
