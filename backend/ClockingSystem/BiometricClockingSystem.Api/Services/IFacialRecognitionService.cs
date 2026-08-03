namespace BiometricClockingSystem.Api.Services;

public class FaceMatchResult
{
    public bool IsMatch { get; init; }
    public double Confidence { get; init; }
}

// Descriptors are generated in the kiosk browser. No cloud provider or API key is used.
public interface IFacialRecognitionService
{
    FaceMatchResult VerifyDescriptor(IReadOnlyList<float> enrolledDescriptor, IReadOnlyList<float> scannedDescriptor);
    Task<FaceMatchResult> VerifyAsync(byte[] storedFacialImage, byte[] scannedFacialImage);
}

public sealed class FacialRecognitionService : IFacialRecognitionService
{
    // face-api.js's own convention: descriptors are compared by Euclidean
    // distance, not cosine similarity. ~0.6 is the standard threshold used
    // by faceRecognitionNet - lower distance means a closer match.
    private const double MatchThreshold = 0.6;

    public FaceMatchResult VerifyDescriptor(IReadOnlyList<float> enrolledDescriptor, IReadOnlyList<float> scannedDescriptor)
    {
        if (enrolledDescriptor.Count == 0 || enrolledDescriptor.Count != scannedDescriptor.Count)
            return new FaceMatchResult();

        double sumSquaredDiff = 0;
        for (var i = 0; i < enrolledDescriptor.Count; i++)
        {
            var diff = enrolledDescriptor[i] - scannedDescriptor[i];
            sumSquaredDiff += diff * diff;
        }

        var distance = Math.Sqrt(sumSquaredDiff);

        return new FaceMatchResult
        {
            IsMatch = distance < MatchThreshold,
            Confidence = Math.Clamp(1 - (distance / MatchThreshold), 0, 1)
        };
    }

    // Legacy MVC clocking uses raw images. The React kiosk uses descriptors;
    // fail safely here until that MVC view is upgraded to produce one too.
    public Task<FaceMatchResult> VerifyAsync(byte[] storedFacialImage, byte[] scannedFacialImage) =>
        Task.FromResult(new FaceMatchResult());
}
