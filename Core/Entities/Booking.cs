using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class Booking
{
    public Guid Id { get; set; }

    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public Guid EmployerId { get; set; }
    public EmployerAccount Employer { get; set; } = null!;

    public BookingType Type { get; set; } = BookingType.DirectContact;
    public BookingStatus Status { get; set; } = BookingStatus.Requested;

    public DateTime StartDate { get; set; }
    public int DurationDays { get; set; }
    public decimal AgreedWage { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Review? Review { get; set; }
}
