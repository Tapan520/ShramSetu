using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>Platform commission / fee record per booking or subscription.</summary>
public class PlatformFee
{
    public Guid Id { get; set; }
    public PlatformFeeType Type { get; set; }
    public PlatformFeeStatus Status { get; set; } = PlatformFeeStatus.Pending;

    public Guid? BookingId { get; set; }
    public Booking? Booking { get; set; }

    public Guid? SubscriptionId { get; set; }
    public EmployerSubscription? Subscription { get; set; }

    public string UserId { get; set; } = string.Empty;   // who owes the fee
    public decimal Amount { get; set; }
    public decimal CommissionRate { get; set; }           // % applied

    public string? TransactionRef { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CollectedAt { get; set; }
}
