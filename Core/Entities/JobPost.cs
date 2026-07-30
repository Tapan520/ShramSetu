using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class JobPost
{
    public Guid Id { get; set; }

    public Guid EmployerId { get; set; }
    public EmployerAccount Employer { get; set; } = null!;

    public Guid SkillCategoryId { get; set; }
    public SkillCategory SkillCategory { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string? LocationCity { get; set; }
    public string? LocationState { get; set; }

    public decimal DailyWage { get; set; }
    public int DurationDays { get; set; }
    public DateTime StartDate { get; set; }

    public int VacancyCount { get; set; } = 1;
    public JobPostStatus Status { get; set; } = JobPostStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
}
