using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>Worker subscribes to alerts for jobs matching their criteria.</summary>
public class JobAlert
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public Guid? SkillCategoryId { get; set; }
    public SkillCategory? SkillCategory { get; set; }

    public string? City { get; set; }
    public decimal? MinWage { get; set; }
    public decimal? MaxWage { get; set; }

    public bool SendSms { get; set; } = true;
    public bool SendPush { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastTriggeredAt { get; set; }
}
