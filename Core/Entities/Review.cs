namespace ShramSetu.Core.Entities;

public class Review
{
    public Guid Id { get; set; }

    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public Guid EmployerId { get; set; }
    public EmployerAccount Employer { get; set; } = null!;

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    /// <summary>15 star rating.</summary>
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
