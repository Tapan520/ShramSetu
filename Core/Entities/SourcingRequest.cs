using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class SourcingRequest
{
    public Guid Id { get; set; }

    public Guid EmployerId { get; set; }
    public EmployerAccount Employer { get; set; } = null!;

    public Guid SkillCategoryId { get; set; }
    public SkillCategory SkillCategory { get; set; } = null!;

    public int WorkerCount { get; set; } = 1;
    public int DurationDays { get; set; }
    public decimal BudgetPerDay { get; set; }
    public string? LocationCity { get; set; }
    public string? LocationState { get; set; }
    public string Description { get; set; } = string.Empty;

    public SourcingStatus Status { get; set; } = SourcingStatus.Open;

    /// <summary>Admin notes on how this request is being fulfilled.</summary>
    public string? AdminNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FulfilledAt { get; set; }
}
