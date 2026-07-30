using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>Formal dispute raised by a worker or employer against the other party.</summary>
public class Dispute
{
    public Guid Id { get; set; }

    /// <summary>Identity UserId of the person raising the dispute.</summary>
    public string RaisedByUserId { get; set; } = string.Empty;

    /// <summary>Identity UserId of the person being disputed against.</summary>
    public string AgainstUserId { get; set; } = string.Empty;

    public Guid? BookingId { get; set; }
    public Booking? Booking { get; set; }

    public DisputeType Type { get; set; }
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string? AdminNotes { get; set; }
    public string? Resolution { get; set; }
    public string? ResolvedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public ICollection<DisputeEvidence> Evidence { get; set; } = new List<DisputeEvidence>();
}
