using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class Referral
{
    public Guid Id { get; set; }

    public string ReferrerUserId { get; set; } = string.Empty;
    public string? ReferredUserId { get; set; }

    /// <summary>Unique referral code (e.g. "RAMESH42").</summary>
    public string Code { get; set; } = string.Empty;

    public ReferralStatus Status { get; set; } = ReferralStatus.Pending;
    public decimal RewardAmount { get; set; }
    public bool RewardCredited { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }
}
