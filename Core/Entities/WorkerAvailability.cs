using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>Worker marks specific date ranges as available or unavailable.</summary>
public class WorkerAvailability
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public AvailabilitySlotType SlotType { get; set; } = AvailabilitySlotType.Available;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
