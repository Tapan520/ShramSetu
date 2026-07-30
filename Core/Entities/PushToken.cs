namespace ShramSetu.Core.Entities;

/// <summary>FCM / APNs device token registered by a mobile app user.</summary>
public class PushToken
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>FCM registration token or APNs device token.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>e.g. "android" or "ios".</summary>
    public string Platform { get; set; } = string.Empty;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
