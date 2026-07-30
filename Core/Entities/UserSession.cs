using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>Tracks active user sessions for device management.</summary>
public class UserSession
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string SessionToken { get; set; } = string.Empty;   // hashed refresh token
    public string DeviceName { get; set; } = string.Empty;
    public string? DeviceType { get; set; }    // mobile/desktop/tablet
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
