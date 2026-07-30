using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>Digital work agreement generated for each booking.</summary>
public class WorkContract
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    public string? PdfUrl { get; set; }
    public string? Content { get; set; }    // HTML/text template rendered content

    public DateTime? WorkerSignedAt { get; set; }
    public string? WorkerSignatureRef { get; set; }

    public DateTime? EmployerSignedAt { get; set; }
    public string? EmployerSignatureRef { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}
