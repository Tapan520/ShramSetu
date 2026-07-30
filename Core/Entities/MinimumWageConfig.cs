namespace ShramSetu.Core.Entities;

/// <summary>State-wise minimum daily wage configuration set by admin.</summary>
public class MinimumWageConfig
{
    public Guid Id { get; set; }
    public string State { get; set; } = string.Empty;
    public Guid SkillCategoryId { get; set; }
    public SkillCategory SkillCategory { get; set; } = null!;
    public decimal MinDailyWage { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public string? Reference { get; set; }   // e.g. notification number
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
