using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>User report for a suspicious job post, worker or employer profile.</summary>
public class UserReport
{
    public Guid Id { get; set; }
    public string ReportedByUserId { get; set; } = string.Empty;
    public string ReportedUserId { get; set; } = string.Empty;

    public Guid? JobPostId { get; set; }
    public JobPost? JobPost { get; set; }

    public ReportType Type { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public string Details { get; set; } = string.Empty;
    public string? AdminNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}
