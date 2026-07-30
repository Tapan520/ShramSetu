namespace ShramSetu.Core.Entities;

/// <summary>
/// Contractor workforce team  a named group of workers managed by one contractor employer.
/// Enables bulk assignment and reporting for Phase 3.
/// </summary>
public class WorkforceTeam
{
    public Guid Id { get; set; }
    public Guid EmployerId { get; set; }
    public EmployerAccount Employer { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ProjectName { get; set; }
    public string? SiteLocation { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<WorkforceTeamMember> Members { get; set; } = new List<WorkforceTeamMember>();
}
