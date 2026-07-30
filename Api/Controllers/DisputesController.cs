using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/disputes")]
[Authorize]
[Produces("application/json")]
public class DisputesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DisputesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetMyDisputes()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var disputes = await _db.Disputes
            .Where(d => d.RaisedByUserId == userId || d.AgainstUserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new { d.Id, d.Title, d.Type, d.Status, d.CreatedAt, d.ResolvedAt })
            .ToListAsync();
        return Ok(disputes);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDispute(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var dispute = await _db.Disputes
            .Include(d => d.Evidence)
            .Include(d => d.Booking)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dispute is null) return NotFound();
        if (dispute.RaisedByUserId != userId && dispute.AgainstUserId != userId
            && !User.IsInRole("Admin")) return Forbid();

        return Ok(dispute);
    }

    [HttpPost]
    public async Task<IActionResult> Raise([FromBody] RaiseDisputeRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!Enum.TryParse<DisputeType>(req.Type, out var type))
            return BadRequest(new { message = $"Invalid type '{req.Type}'." });

        var dispute = new Dispute
        {
            Id               = Guid.NewGuid(),
            RaisedByUserId   = userId,
            AgainstUserId    = req.AgainstUserId,
            BookingId        = req.BookingId,
            Type             = type,
            Title            = req.Title,
            Description      = req.Description
        };

        _db.Disputes.Add(dispute);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetDispute), new { id = dispute.Id }, new { dispute.Id, dispute.Title, dispute.Status });
    }

    [HttpPost("{id:guid}/evidence")]
    public async Task<IActionResult> AddEvidence(Guid id, [FromBody] AddEvidenceRequest req)
    {
        var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var dispute = await _db.Disputes.FindAsync(id);
        if (dispute is null) return NotFound();
        if (dispute.RaisedByUserId != userId && dispute.AgainstUserId != userId) return Forbid();

        _db.DisputeEvidences.Add(new DisputeEvidence
        {
            Id                  = Guid.NewGuid(),
            DisputeId           = id,
            SubmittedByUserId   = userId,
            Description         = req.Description,
            FileUrl             = req.FileUrl
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Evidence submitted." });
    }

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveDisputeRequest req)
    {
        var dispute = await _db.Disputes.FindAsync(id);
        if (dispute is null) return NotFound();

        dispute.Status         = Enum.TryParse<DisputeStatus>(req.Status, out var s) ? s : dispute.Status;
        dispute.AdminNotes     = req.AdminNotes;
        dispute.Resolution     = req.Resolution;
        dispute.ResolvedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        dispute.ResolvedAt     = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    public record RaiseDisputeRequest(
        [System.ComponentModel.DataAnnotations.Required] string AgainstUserId,
        [System.ComponentModel.DataAnnotations.Required] string Type,
        [System.ComponentModel.DataAnnotations.Required] string Title,
        [System.ComponentModel.DataAnnotations.Required] string Description,
        Guid? BookingId);

    public record AddEvidenceRequest(
        [System.ComponentModel.DataAnnotations.Required] string Description,
        string? FileUrl);

    public record ResolveDisputeRequest(
        [System.ComponentModel.DataAnnotations.Required] string Status,
        string? AdminNotes,
        string? Resolution);
}
