using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize]
[Produces("application/json")]
public class SessionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SessionsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetMySessions()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var sessions = await _db.UserSessions
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
            .OrderByDescending(s => s.LastActiveAt)
            .Select(s => new { s.Id, s.DeviceName, s.DeviceType, s.IpAddress, s.CreatedAt, s.LastActiveAt })
            .ToListAsync();
        return Ok(sessions);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RevokeSession(Guid id)
    {
        var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var session = await _db.UserSessions.FindAsync(id);
        if (session is null || session.UserId != userId) return NotFound();

        session.Status    = SessionStatus.Revoked;
        session.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("revoke-all")]
    public async Task<IActionResult> RevokeAll()
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var sessions = await _db.UserSessions
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
            .ToListAsync();

        foreach (var s in sessions) { s.Status = SessionStatus.Revoked; s.RevokedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return Ok(new { message = $"{sessions.Count} session(s) revoked." });
    }
}
