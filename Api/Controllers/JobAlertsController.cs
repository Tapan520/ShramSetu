using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;
using ShramSetu.Services;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/job-alerts")]
[Authorize(Roles = "Worker")]
[Produces("application/json")]
public class JobAlertsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public JobAlertsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetMyAlerts()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return Ok(Array.Empty<object>());

        var alerts = await _db.JobAlerts
            .Include(a => a.SkillCategory)
            .Where(a => a.WorkerId == worker.Id)
            .ToListAsync();

        return Ok(alerts.Select(a => new
        {
            a.Id, a.City, a.MinWage, a.MaxWage, a.IsActive,
            SkillCategory = a.SkillCategory?.Name,
            a.SendSms, a.SendPush, a.LastTriggeredAt
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAlertRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return BadRequest(new { message = "Worker profile not found." });

        _db.JobAlerts.Add(new JobAlert
        {
            Id              = Guid.NewGuid(),
            WorkerId        = worker.Id,
            SkillCategoryId = req.SkillCategoryId,
            City            = req.City,
            MinWage         = req.MinWage,
            MaxWage         = req.MaxWage,
            SendSms         = req.SendSms,
            SendPush        = req.SendPush
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Job alert created." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        var alert  = await _db.JobAlerts.FindAsync(id);
        if (alert is null || alert.WorkerId != worker?.Id) return NotFound();
        _db.JobAlerts.Remove(alert);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    public record CreateAlertRequest(Guid? SkillCategoryId, string? City,
        decimal? MinWage, decimal? MaxWage, bool SendSms = true, bool SendPush = true);
}

[ApiController]
[Route("api/referrals")]
[Authorize]
[Produces("application/json")]
public class ReferralsController : ControllerBase
{
    private readonly IReferralService _referral;
    public ReferralsController(IReferralService referral) => _referral = referral;

    [HttpGet("my-code")]
    public async Task<IActionResult> GetMyCode()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var referral = await _referral.GetOrCreateReferralCodeAsync(userId);
        return Ok(new { referral.Code, referral.RewardAmount, referral.Status });
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromQuery] string code)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var success = await _referral.ApplyReferralCodeAsync(code, userId);
        return success
            ? Ok(new { message = "Referral applied! Reward credited to referrer's wallet." })
            : BadRequest(new { message = "Invalid or already-used referral code." });
    }
}
