using System.Threading.Tasks;

namespace BiometricClockingSystem.Api.Services
{
    public class FingerprintMatchResult
    {
        public bool IsMatch { get; set; }
        public double Confidence { get; set; }
    }

    // Compares a fingerprint the administrator just scanned against the
    // employee's stored enrollment template (captured at registration).
    // Only ever called from the admin override flow - never by the employee.
    public interface IFingerprintMatchingService
    {
        Task<FingerprintMatchResult> VerifyAsync(byte[] storedTemplate, byte[] scannedTemplate);
    }

    // PLACEHOLDER - NOT PRODUCTION READY.
    //
    // Fingerprint matching is done by whatever SDK ships with your reader
    // (DigitalPersona, SecuGen, Futronic, ZKTeco, etc). Most expose a
    // "MatchTemplates" / "Verify" call that takes two templates and returns
    // a similarity score. Call that here instead of this placeholder.
    public class FingerprintMatchingService : IFingerprintMatchingService
    {
        public Task<FingerprintMatchResult> VerifyAsync(byte[] storedTemplate, byte[] scannedTemplate)
        {
            // TODO: replace with your fingerprint SDK's actual matching call.
            return Task.FromResult(new FingerprintMatchResult { IsMatch = false, Confidence = 0 });
        }
    }
}
