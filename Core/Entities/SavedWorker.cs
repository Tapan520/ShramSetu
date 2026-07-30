namespace ShramSetu.Core.Entities;

/// <summary>Employer saves/favourites a worker profile for quick access.</summary>
public class SavedWorker
{
    public Guid Id { get; set; }
    public Guid EmployerId { get; set; }
    public EmployerAccount Employer { get; set; } = null!;

    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public string? Note { get; set; }
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
