namespace ShramSetu.Core.Entities;

/// <summary>Reusable job post template saved by an employer.</summary>
public class JobPostTemplate
{
    public Guid Id { get; set; }
    public Guid EmployerId { get; set; }
    public EmployerAccount Employer { get; set; } = null!;
    public string Name { get; set; } = string.Empty;   // template label
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid SkillCategoryId { get; set; }
    public SkillCategory SkillCategory { get; set; } = null!;
    public string? LocationCity { get; set; }
    public string? LocationState { get; set; }
    public decimal DailyWage { get; set; }
    public int DurationDays { get; set; }
    public int VacancyCount { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
