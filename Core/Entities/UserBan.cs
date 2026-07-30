using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>Admin ban or suspension on any user account.</summary>
public class UserBan
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public BanStatus Status { get; set; } = BanStatus.Banned;
    public string Reason { get; set; } = string.Empty;
    public string BannedByUserId { get; set; } = string.Empty;
    public DateTime BannedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }   // null = permanent
    public DateTime? LiftedAt { get; set; }
    public string? LiftedByUserId { get; set; }
    public string? LiftReason { get; set; }
}
