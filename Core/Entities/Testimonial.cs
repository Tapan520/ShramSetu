namespace ShramSetu.Core.Entities;

/// <summary>Worker success story / testimonial featured on homepage.</summary>
public class Testimonial
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;
    public string Headline { get; set; } = string.Empty;
    public string Story { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public decimal? MonthlyEarnings { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
