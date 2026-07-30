using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class Notification
{
    public Guid Id { get; set; }

    /// <summary>Identity UserId of the recipient.</summary>
    public string RecipientUserId { get; set; } = string.Empty;

    /// <summary>Also stored as UserId for in-app inbox queries.</summary>
    public string UserId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public bool IsSent { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public string? FailureReason { get; set; }
}

