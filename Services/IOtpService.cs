namespace ShramSetu.Services;

public interface IOtpService
{
    /// <summary>Generate a 6-digit OTP, persist its hash and send it via SMS.</summary>
    Task<string> SendOtpAsync(string phone, CancellationToken ct = default);

    /// <summary>Verify the OTP. Returns true on success and marks it used.</summary>
    Task<bool> VerifyOtpAsync(string phone, string code, CancellationToken ct = default);
}
