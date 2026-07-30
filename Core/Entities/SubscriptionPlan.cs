using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class SubscriptionPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SubscriptionTier Tier { get; set; }
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }

    /// <summary>Max active job posts allowed under this plan (-1 = unlimited).</summary>
    public int MaxJobPosts { get; set; }

    /// <summary>Max sourcing requests per month (-1 = unlimited).</summary>
    public int MaxSourcingRequests { get; set; }

    public bool CanAccessChat { get; set; }
    public bool CanAccessAnalytics { get; set; }
    public bool PrioritySupport { get; set; }

    public bool IsActive { get; set; } = true;
    public ICollection<EmployerSubscription> Subscriptions { get; set; } = new List<EmployerSubscription>();
}
