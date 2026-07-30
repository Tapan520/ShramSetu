using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Api.Dtos;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/teams")]
[Authorize(Roles = "Employer,Admin")]
[Produces("application/json")]
public class WorkforceTeamsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public WorkforceTeamsController(ApplicationDbContext db) => _db = db;

    /// <summary>List all teams for the authenticated employer.</summary>
    [HttpGet]
    public async Task<ActionResult<IList<TeamDto>>> GetMyTeams()
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null) return Ok(Array.Empty<TeamDto>());

        var teams = await _db.WorkforceTeams
            .Include(t => t.Members)
            .Where(t => t.EmployerId == employer.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(teams.Select(t => new TeamDto(
            t.Id, t.Name, t.Description, t.ProjectName, t.SiteLocation,
            t.Members.Count(m => m.IsActive), t.IsActive, t.CreatedAt)));
    }

    /// <summary>Create a new workforce team.</summary>
    [HttpPost]
    public async Task<ActionResult<TeamDto>> Create([FromBody] CreateTeamRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null) return BadRequest(new { message = "Employer profile not found." });

        var team = new WorkforceTeam
        {
            Id           = Guid.NewGuid(),
            EmployerId   = employer.Id,
            Name         = req.Name,
            Description  = req.Description,
            ProjectName  = req.ProjectName,
            SiteLocation = req.SiteLocation
        };

        _db.WorkforceTeams.Add(team);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTeam), new { id = team.Id },
            new TeamDto(team.Id, team.Name, team.Description, team.ProjectName, team.SiteLocation, 0, true, team.CreatedAt));
    }

    /// <summary>Get a specific team with its members.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeamDto>> GetTeam(Guid id)
    {
        var team = await _db.WorkforceTeams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (team is null) return NotFound();
        return Ok(new TeamDto(team.Id, team.Name, team.Description, team.ProjectName, team.SiteLocation,
            team.Members.Count(m => m.IsActive), team.IsActive, team.CreatedAt));
    }

    /// <summary>Get all active members of a team.</summary>
    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<IList<TeamMemberDto>>> GetMembers(Guid id)
    {
        var members = await _db.WorkforceTeamMembers
            .Include(m => m.Worker).ThenInclude(w => w.SkillCategory)
            .Where(m => m.TeamId == id && m.IsActive)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync();

        return Ok(members.Select(m => new TeamMemberDto(
            m.Id, m.WorkerId, m.Worker.FullName, m.Worker.PhotoUrl,
            m.Worker.SkillCategory.Name, m.Role, m.JoinedAt, m.IsActive)));
    }

    /// <summary>Add a worker to a team.</summary>
    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<TeamMemberDto>> AddMember(Guid id, [FromBody] AddTeamMemberRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var team = await _db.WorkforceTeams.FindAsync(id);
        if (team is null) return NotFound();

        var worker = await _db.Workers.Include(w => w.SkillCategory).FirstOrDefaultAsync(w => w.Id == req.WorkerId);
        if (worker is null) return NotFound(new { message = "Worker not found." });

        var existing = await _db.WorkforceTeamMembers
            .FirstOrDefaultAsync(m => m.TeamId == id && m.WorkerId == req.WorkerId && m.IsActive);
        if (existing is not null) return Conflict(new { message = "Worker is already in this team." });

        var member = new WorkforceTeamMember
        {
            Id       = Guid.NewGuid(),
            TeamId   = id,
            WorkerId = req.WorkerId,
            Role     = req.Role
        };

        _db.WorkforceTeamMembers.Add(member);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMembers), new { id },
            new TeamMemberDto(member.Id, member.WorkerId, worker.FullName, worker.PhotoUrl,
                worker.SkillCategory.Name, member.Role, member.JoinedAt, true));
    }

    /// <summary>Remove a worker from a team.</summary>
    [HttpDelete("{id:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId)
    {
        var member = await _db.WorkforceTeamMembers.FindAsync(memberId);
        if (member is null || member.TeamId != id) return NotFound();

        member.IsActive = false;
        member.LeftAt   = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
