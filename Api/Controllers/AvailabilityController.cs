using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Api.Dtos;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/availability")]
[Authorize(Roles = "Worker")]
[Produces("application/json")]
public class AvailabilityController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AvailabilityController(ApplicationDbContext db) => _db = db;

    /// <summary>Get the authenticated worker's availability slots.</summary>
    [HttpGet]
    public async Task<ActionResult<IList<AvailabilityDto>>> GetMyAvailability(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return NotFound();

        var query = _db.WorkerAvailabilities.Where(a => a.WorkerId == worker.Id);
        if (from.HasValue) query = query.Where(a => a.EndDate >= from.Value);
        if (to.HasValue)   query = query.Where(a => a.StartDate <= to.Value);

        var slots = await query.OrderBy(a => a.StartDate).ToListAsync();
        return Ok(slots.Select(a => new AvailabilityDto(a.Id, a.StartDate, a.EndDate, a.SlotType.ToString(), a.Note)));
    }

    /// <summary>Get any worker's public availability (employers can see this).</summary>
    [HttpGet("worker/{workerId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<IList<AvailabilityDto>>> GetWorkerAvailability(
        Guid workerId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _db.WorkerAvailabilities.Where(a => a.WorkerId == workerId);
        if (from.HasValue) query = query.Where(a => a.EndDate >= from.Value);
        if (to.HasValue)   query = query.Where(a => a.StartDate <= to.Value);

        var slots = await query.OrderBy(a => a.StartDate).ToListAsync();
        return Ok(slots.Select(a => new AvailabilityDto(a.Id, a.StartDate, a.EndDate, a.SlotType.ToString(), a.Note)));
    }

    /// <summary>Add or update an availability slot.</summary>
    [HttpPost]
    public async Task<ActionResult<AvailabilityDto>> Set([FromBody] SetAvailabilityRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return NotFound();

        if (!Enum.TryParse<AvailabilitySlotType>(req.SlotType, out var slotType))
            return BadRequest(new { message = $"Invalid SlotType '{req.SlotType}'." });

        var slot = new WorkerAvailability
        {
            Id = Guid.NewGuid(),
            WorkerId = worker.Id,
            StartDate = req.StartDate.Date,
            EndDate   = req.EndDate.Date,
            SlotType  = slotType,
            Note      = req.Note
        };

        _db.WorkerAvailabilities.Add(slot);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetMyAvailability), new AvailabilityDto(slot.Id, slot.StartDate, slot.EndDate, slot.SlotType.ToString(), slot.Note));
    }

    /// <summary>Delete an availability slot.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        var slot   = await _db.WorkerAvailabilities.FindAsync(id);

        if (slot is null) return NotFound();
        if (slot.WorkerId != worker?.Id) return Forbid();

        _db.WorkerAvailabilities.Remove(slot);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
