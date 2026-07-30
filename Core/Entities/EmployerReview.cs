using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>Employer rating submitted by a worker after a completed booking.</summary>
public class EmployerReview
{
    public Guid Id { get; set; }
    public Guid EmployerId { get; set; }
    public EmployerAccount Employer { get; set; } = null!;

    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public int Rating { get; set; }  // 15
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
