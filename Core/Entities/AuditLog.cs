using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>Immutable audit trail of every significant action in the system.</summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AuditAction Action { get; set; }

    /// <summary>Entity type name e.g. "Booking", "Worker".</summary>
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;

    /// <summary>JSON snapshot of old values.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of new values.</summary>
    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
