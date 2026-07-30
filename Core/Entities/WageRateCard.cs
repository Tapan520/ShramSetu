namespace ShramSetu.Core.Entities;

/// <summary>Recommended daily wage bands per skill and city set by admin.</summary>
public class WageRateCard
{
    public Guid Id { get; set; }
    public Guid SkillCategoryId { get; set; }
    public SkillCategory SkillCategory { get; set; } = null!;

    public string City { get; set; } = string.Empty;
    public string? State { get; set; }

    public decimal MinDailyWage { get; set; }
    public decimal MaxDailyWage { get; set; }
    public decimal RecommendedDailyWage { get; set; }

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedByUserId { get; set; }
}
