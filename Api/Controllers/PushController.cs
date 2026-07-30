using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Api.Dtos;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/push")]
[Authorize]
[Produces("application/json")]
public class PushController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PushController(ApplicationDbContext db) => _db = db;

    /// <summary>Register or refresh a device push token.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterPushTokenRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var existing = await _db.PushTokens.FirstOrDefaultAsync(t => t.Token == req.Token);
        if (existing is not null)
        {
            existing.UserId     = userId;
            existing.Platform   = req.Platform;
            existing.IsActive   = true;
            existing.LastUsedAt = DateTime.UtcNow;
        }
        else
        {
            _db.PushTokens.Add(new PushToken
            {
                Id           = Guid.NewGuid(),
                UserId       = userId,
                Token        = req.Token,
                Platform     = req.Platform,
                RegisteredAt = DateTime.UtcNow,
                IsActive     = true
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Push token registered." });
    }

    /// <summary>Unregister a device push token (on logout).</summary>
    [HttpDelete("unregister")]
    public async Task<IActionResult> Unregister([FromQuery] string token)
    {
        var pt = await _db.PushTokens.FirstOrDefaultAsync(t => t.Token == token);
        if (pt is not null)
        {
            pt.IsActive = false;
            await _db.SaveChangesAsync();
        }
        return NoContent();
    }
}
