using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>Tracks which onboarding steps a worker has completed.</summary>
public class WorkerOnboarding
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public bool PhotoDone { get; set; }
    public bool SkillsDone { get; set; }
    public bool LocationDone { get; set; }
    public bool DocumentsDone { get; set; }
    public bool BankDone { get; set; }

    /// <summary>0100 completeness score.</summary>
    public int CompletenessScore => (new[] { PhotoDone, SkillsDone, LocationDone, DocumentsDone, BankDone }
        .Count(x => x)) * 20;

    public bool IsCompleted => CompletenessScore == 100;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
