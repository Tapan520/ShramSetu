namespace ShramSetu.Core.Entities;

/// <summary>Work portfolio photo uploaded by a worker.</summary>
public class WorkerPortfolioPhoto
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public string PhotoUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int DisplayOrder { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
