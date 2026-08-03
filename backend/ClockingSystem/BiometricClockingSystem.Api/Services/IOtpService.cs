using BiometricClockingSystem.Api.Models;

namespace BiometricClockingSystem.Api.Services;

public sealed record OtpChallenge(Guid Id, string EmployeeId, ClockType ClockType, DateTime ExpiresAt);
public sealed record OtpVerificationResult(bool Succeeded, string? Error = null);

public interface IOtpService
{
    Task<OtpChallenge> CreateAsync(string employeeId, ClockType clockType);
    bool TryGetChallenge(Guid challengeId, out OtpChallenge challenge);
    bool TryGetChallengeForEmployee(string employeeId, out OtpChallenge challenge);
    Task<OtpVerificationResult> VerifyAsync(Guid challengeId, string code);
    Task GenerateAndSendAsync(string employeeId);
}
