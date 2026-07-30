namespace ShramSetu.Core.Entities;

/// <summary>Onboarding slide content configurable by admin (shown in mobile app).</summary>
public class OnboardingSlide
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? CtaText { get; set; }
    public string? CtaLink { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
