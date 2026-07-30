using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>GST-compliant invoice for subscription or booking commission fees.</summary>
public class GstInvoice
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string BilledToUserId { get; set; } = string.Empty;
    public string? BilledToGstin { get; set; }
    public string BilledToName { get; set; } = string.Empty;
    public string BilledToAddress { get; set; } = string.Empty;

    public Guid? SubscriptionId { get; set; }
    public EmployerSubscription? Subscription { get; set; }

    public decimal BaseAmount { get; set; }
    public decimal CgstRate { get; set; } = 9;    // %
    public decimal SgstRate { get; set; } = 9;    // %
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public GstInvoiceStatus Status { get; set; } = GstInvoiceStatus.Draft;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public string? PdfUrl { get; set; }
}
