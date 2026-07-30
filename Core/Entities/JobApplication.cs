using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class JobApplication
{
    public Guid Id { get; set; }

    public Guid JobPostId { get; set; }
    public JobPost JobPost { get; set; } = null!;

    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public string? CoverNote { get; set; }
    public JobApplicationStatus Status { get; set; } = JobApplicationStatus.Applied;

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? EmployerNote { get; set; }
}
