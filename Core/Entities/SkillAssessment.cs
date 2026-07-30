using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class SkillAssessment
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;
    public Guid SkillCategoryId { get; set; }
    public SkillCategory SkillCategory { get; set; } = null!;

    public SkillAssessmentStatus Status { get; set; } = SkillAssessmentStatus.NotAttempted;
    public int Score { get; set; }        // 0-100
    public int PassingScore { get; set; } = 70;
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? PassedAt { get; set; }
}
