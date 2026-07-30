namespace ShramSetu.Core.Entities;

/// <summary>AI-generated worker match score for a job post or sourcing request.</summary>
public class WorkerMatchScore
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public Guid? JobPostId { get; set; }
    public JobPost? JobPost { get; set; }

    public Guid? SourcingRequestId { get; set; }
    public SourcingRequest? SourcingRequest { get; set; }

    /// <summary>Score 0100 from the matching algorithm.</summary>
    public double Score { get; set; }

    /// <summary>Human-readable explanation of why this worker was matched.</summary>
    public string? Reason { get; set; }

    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}
