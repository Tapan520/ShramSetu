using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>Labour law compliance check result for a booking.</summary>
public class ComplianceCheck
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public ComplianceCheckType CheckType { get; set; }
    public ComplianceStatus Status { get; set; }
    public string? Details { get; set; }
    public string? Recommendation { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
