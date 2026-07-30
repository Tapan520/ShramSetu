using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>Platform-wide announcement broadcast by admin.</summary>
public class Announcement
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public AnnouncementTarget Target { get; set; } = AnnouncementTarget.All;
    public bool SendPush { get; set; } = true;
    public bool SendSms { get; set; } = false;
    public bool ShowBanner { get; set; } = true;  // show as sitewide banner
    public string? BannerCssClass { get; set; } = "alert-info";
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}
