namespace ShramSetu.Core.Entities;

/// <summary>File/text evidence attached to a dispute.</summary>
public class DisputeEvidence
{
    public Guid Id { get; set; }
    public Guid DisputeId { get; set; }
    public Dispute Dispute { get; set; } = null!;

    public string SubmittedByUserId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>URL of uploaded evidence file (photo, document).</summary>
    public string? FileUrl { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
