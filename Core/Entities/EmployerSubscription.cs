using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class EmployerSubscription
{
    public Guid Id { get; set; }

    public Guid EmployerId { get; set; }
    public EmployerAccount Employer { get; set; } = null!;

    public Guid PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; }

    /// <summary>Payment gateway transaction reference (Razorpay / Stripe).</summary>
    public string? PaymentReference { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
