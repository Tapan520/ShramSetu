using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>Progressive trust badge earned by a worker.</summary>
public class WorkerBadge
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public BadgeTier Tier { get; set; }
    public string? Notes { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
