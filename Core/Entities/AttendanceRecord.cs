using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class AttendanceRecord
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    public TimeSpan? CheckInTime { get; set; }
    public TimeSpan? CheckOutTime { get; set; }

    /// <summary>Hours worked (computed or entered manually).</summary>
    public decimal HoursWorked { get; set; }

    public string? Notes { get; set; }
    public DateTime MarkedAt { get; set; } = DateTime.UtcNow;
    public string? MarkedByUserId { get; set; }
}
