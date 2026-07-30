namespace ShramSetu.Core.Entities;

/// <summary>Stores a user's preferred notification channel settings.</summary>
public class NotificationPreference
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    public bool BookingUpdates_SMS { get; set; } = true;
    public bool BookingUpdates_Push { get; set; } = true;
    public bool BookingUpdates_WhatsApp { get; set; } = false;

    public bool JobAlerts_SMS { get; set; } = true;
    public bool JobAlerts_Push { get; set; } = true;

    public bool PaymentNotifications_SMS { get; set; } = true;
    public bool PaymentNotifications_Push { get; set; } = true;

    public bool ChatMessages_Push { get; set; } = true;

    public bool SystemAnnouncements_SMS { get; set; } = false;
    public bool SystemAnnouncements_Push { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
