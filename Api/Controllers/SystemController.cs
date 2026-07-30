using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class SystemController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public SystemController(ApplicationDbContext db) => _db = db;

    /// <summary>Returns minimum required and latest app version per platform. Used by mobile app to gate old builds.</summary>
    [HttpGet("version/{platform}")]
    public async Task<IActionResult> GetVersion(string platform)
    {
        if (!Enum.TryParse<AppPlatform>(platform, true, out var p))
            return BadRequest(new { message = $"Unknown platform '{platform}'. Use android or ios." });

        var version = await _db.AppVersions.FirstOrDefaultAsync(v => v.Platform == p);
        if (version is null)
            return Ok(new { MinVersion = "1.0.0", LatestVersion = "1.0.0", ForceUpdate = false });

        return Ok(new
        {
            version.MinVersion, version.LatestVersion,
            version.UpdateMessage, version.ForceUpdate
        });
    }

    /// <summary>Admin sets the minimum and latest version for a platform.</summary>
    [HttpPut("version/{platform}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetVersion(string platform, [FromBody] SetVersionRequest req)
    {
        if (!Enum.TryParse<AppPlatform>(platform, true, out var p))
            return BadRequest(new { message = $"Unknown platform '{platform}'." });

        var existing = await _db.AppVersions.FirstOrDefaultAsync(v => v.Platform == p);
        if (existing is null)
        {
            _db.AppVersions.Add(new Core.Entities.AppVersion
            {
                Id = Guid.NewGuid(), Platform = p,
                MinVersion = req.MinVersion, LatestVersion = req.LatestVersion,
                UpdateMessage = req.UpdateMessage, ForceUpdate = req.ForceUpdate
            });
        }
        else
        {
            existing.MinVersion    = req.MinVersion;
            existing.LatestVersion = req.LatestVersion;
            existing.UpdateMessage = req.UpdateMessage;
            existing.ForceUpdate   = req.ForceUpdate;
            existing.UpdatedAt     = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Admin-only audit log browser.</summary>
    [HttpGet("audit-logs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityType, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = _db.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        var total = await query.CountAsync();
        var logs  = await query.OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new { total, page, pageSize, logs });
    }

    public record SetVersionRequest(string MinVersion, string LatestVersion,
        string? UpdateMessage, bool ForceUpdate);
}
