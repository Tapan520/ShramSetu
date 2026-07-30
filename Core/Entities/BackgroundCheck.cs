using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class BackgroundCheck
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public BackgroundCheckType CheckType { get; set; }
    public BackgroundCheckStatus Status { get; set; } = BackgroundCheckStatus.Pending;

    /// <summary>External provider reference (e.g. Signzy / IDfy check ID).</summary>
    public string? ProviderReference { get; set; }
    public string? ProviderName { get; set; }

    public string? ResultSummary { get; set; }
    public string? ReportUrl { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? RequestedByUserId { get; set; }
}
