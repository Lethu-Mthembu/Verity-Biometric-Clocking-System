namespace BiometricClockingSystem.Api.Services;

public sealed class GmailOptions
{
    public const string SectionName = "Gmail";

    public string SenderAddress { get; init; } = string.Empty;
    public string AppPassword { get; init; } = string.Empty;
}

public sealed class OtpDeliveryException : Exception
{
    public OtpDeliveryException(string message) : base(message) { }
}
