namespace ShramSetu.Core.Entities;

/// <summary>Worker of the Month nominated by admin.</summary>
public class WorkerOfTheMonth
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;
    public int Month { get; set; }   // 1-12
    public int Year { get; set; }
    public string? Reason { get; set; }
    public string NominatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
