namespace ShramSetu.Core.Entities;

/// <summary>
/// Short-lived one-time password used for phone-based authentication.
/// The Code is stored as a SHA-256 hash  never in plain text.
/// </summary>
public class OtpCode
{
    public Guid Id { get; set; }

    /// <summary>E.164 format phone number e.g. +919876543210</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>SHA-256 hash of the 6-digit OTP.</summary>
    public string CodeHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;

    /// <summary>Number of failed verification attempts (lock out after 5).</summary>
    public int Attempts { get; set; } = 0;
}
