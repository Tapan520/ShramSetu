namespace ShramSetu.Core.Entities;

public class WorkforceTeamMember
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public WorkforceTeam Team { get; set; } = null!;

    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public string? Role { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public bool IsActive { get; set; } = true;
}
