using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Api.Dtos;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/saved")]
[Authorize(Roles = "Employer,Admin")]
[Produces("application/json")]
public class SavedWorkersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public SavedWorkersController(ApplicationDbContext db) => _db = db;

    /// <summary>Get the employer's saved/favourite workers list.</summary>
    [HttpGet]
    public async Task<ActionResult<IList<SavedWorkerDto>>> GetSaved()
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null) return Ok(Array.Empty<SavedWorkerDto>());

        var saved = await _db.SavedWorkers
            .Include(s => s.Worker).ThenInclude(w => w.SkillCategory)
            .Where(s => s.EmployerId == employer.Id)
            .OrderByDescending(s => s.SavedAt)
            .ToListAsync();

        return Ok(saved.Select(s => new SavedWorkerDto(
            s.Id, s.WorkerId, s.Worker.FullName, s.Worker.PhotoUrl,
            s.Worker.SkillCategory.Name, s.Note, s.SavedAt)));
    }

    /// <summary>Save / favourite a worker.</summary>
    [HttpPost]
    public async Task<ActionResult<SavedWorkerDto>> Save([FromBody] SaveWorkerRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null) return BadRequest(new { message = "Employer profile not found." });

        var already = await _db.SavedWorkers
            .AnyAsync(s => s.EmployerId == employer.Id && s.WorkerId == req.WorkerId);
        if (already) return Conflict(new { message = "Worker already saved." });

        var worker = await _db.Workers.Include(w => w.SkillCategory)
            .FirstOrDefaultAsync(w => w.Id == req.WorkerId);
        if (worker is null) return NotFound(new { message = "Worker not found." });

        var saved = new SavedWorker
        {
            Id = Guid.NewGuid(),
            EmployerId = employer.Id,
            WorkerId   = req.WorkerId,
            Note       = req.Note
        };

        _db.SavedWorkers.Add(saved);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSaved),
            new SavedWorkerDto(saved.Id, saved.WorkerId, worker.FullName, worker.PhotoUrl,
                worker.SkillCategory.Name, saved.Note, saved.SavedAt));
    }

    /// <summary>Remove a saved worker.</summary>
    [HttpDelete("{workerId:guid}")]
    public async Task<IActionResult> Unsave(Guid workerId)
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        var saved    = await _db.SavedWorkers
            .FirstOrDefaultAsync(s => s.EmployerId == employer!.Id && s.WorkerId == workerId);

        if (saved is null) return NotFound();
        _db.SavedWorkers.Remove(saved);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
