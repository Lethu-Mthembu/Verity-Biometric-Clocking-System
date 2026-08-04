namespace BiometricClockingSystem.Api.Services;

public sealed class SendGridOptions
{
    public const string SectionName = "SendGrid";

    public string SenderAddress { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
}

public sealed class OtpDeliveryException : Exception
{
    public OtpDeliveryException(string message) : base(message) { }
}
