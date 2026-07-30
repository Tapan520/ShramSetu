namespace ShramSetu.Core.Entities;

/// <summary>MCQ question for skill assessment quizzes.</summary>
public class AssessmentQuestion
{
    public Guid Id { get; set; }
    public Guid SkillCategoryId { get; set; }
    public SkillCategory SkillCategory { get; set; } = null!;

    public string QuestionText { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string CorrectOption { get; set; } = "A";   // A/B/C/D
    public int Marks { get; set; } = 10;
    public bool IsActive { get; set; } = true;
}
