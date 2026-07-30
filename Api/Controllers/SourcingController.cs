using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Api.Dtos;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/sourcing")]
[Produces("application/json")]
public class SourcingController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SourcingController(ApplicationDbContext db) => _db = db;

    /// <summary>Submit a new sourcing / concierge request.</summary>
    [HttpPost]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<SourcingRequestDto>> Create([FromBody] CreateSourcingRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);

        if (employer is null)
        {
            // Auto-create a minimal profile for employers who registered via mobile
            employer = new EmployerAccount
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Mobile User",
                Phone = req.ContactPhone
            };
            _db.EmployerAccounts.Add(employer);
        }
        else
        {
            employer.Phone = req.ContactPhone;
        }

        var sourcing = new SourcingRequest
        {
            Id = Guid.NewGuid(),
            EmployerId = employer.Id,
            SkillCategoryId = req.SkillCategoryId,
            WorkerCount = req.WorkerCount,
            DurationDays = req.DurationDays,
            BudgetPerDay = req.BudgetPerDay,
            LocationCity = req.LocationCity,
            LocationState = req.LocationState,
            Description = req.Description
        };

        _db.SourcingRequests.Add(sourcing);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyRequests), ToDto(sourcing, employer));
    }

    /// <summary>Get the authenticated employer's sourcing requests.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<PagedResult<SourcingRequestDto>>> GetMyRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page     = Math.Max(1, page);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null)
            return Ok(new PagedResult<SourcingRequestDto>([], page, pageSize, 0));

        var total = await _db.SourcingRequests.CountAsync(r => r.EmployerId == employer.Id);
        var items = await _db.SourcingRequests
            .Include(r => r.SkillCategory)
            .Where(r => r.EmployerId == employer.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<SourcingRequestDto>(
            items.Select(r => ToDto(r, employer)).ToList(), page, pageSize, total));
    }

    private static SourcingRequestDto ToDto(SourcingRequest r, EmployerAccount _) => new(
        r.Id,
        r.SkillCategory?.Name ?? string.Empty,
        r.WorkerCount,
        r.DurationDays,
        r.BudgetPerDay,
        r.LocationCity,
        r.LocationState,
        r.Description,
        r.Status.ToString(),
        r.AdminNotes,
        r.CreatedAt,
        r.FulfilledAt);
}
