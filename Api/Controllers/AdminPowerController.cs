using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminPowerController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminPowerController(ApplicationDbContext db) => _db = db;

    // ?? User Bans ?????????????????????????????????????????????????????????????

    [HttpPost("ban")]
    public async Task<IActionResult> BanUser([FromBody] BanRequest req)
    {
        var existing = await _db.UserBans
            .FirstOrDefaultAsync(b => b.UserId == req.UserId && b.Status == BanStatus.Banned);
        if (existing is not null) return Conflict(new { message = "User is already banned." });

        _db.UserBans.Add(new UserBan
        {
            Id             = Guid.NewGuid(),
            UserId         = req.UserId,
            Reason         = req.Reason,
            BannedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            ExpiresAt      = req.ExpiresAt
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "User banned." });
    }

    [HttpPost("unban/{userId}")]
    public async Task<IActionResult> UnbanUser(string userId, [FromBody] UnbanRequest req)
    {
        var ban = await _db.UserBans.FirstOrDefaultAsync(b => b.UserId == userId && b.Status == BanStatus.Banned);
        if (ban is null) return NotFound(new { message = "No active ban found." });

        ban.Status          = BanStatus.Active;
        ban.LiftedAt        = DateTime.UtcNow;
        ban.LiftedByUserId  = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ban.LiftReason      = req.Reason;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Ban lifted." });
    }

    [HttpGet("bans")]
    public async Task<IActionResult> GetActiveBans()
    {
        var bans = await _db.UserBans
            .Where(b => b.Status == BanStatus.Banned)
            .OrderByDescending(b => b.BannedAt)
            .ToListAsync();
        return Ok(bans);
    }

    // ?? Announcements ?????????????????????????????????????????????????????????

    [HttpGet("announcements")]
    public async Task<IActionResult> GetAnnouncements()
    {
        var list = await _db.Announcements
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost("announcements")]
    public async Task<IActionResult> CreateAnnouncement([FromBody] AnnouncementRequest req)
    {
        if (!Enum.TryParse<AnnouncementTarget>(req.Target, out var target))
            return BadRequest(new { message = $"Invalid target '{req.Target}'." });

        _db.Announcements.Add(new Announcement
        {
            Id               = Guid.NewGuid(),
            Title            = req.Title,
            Body             = req.Body,
            Target           = target,
            SendPush         = req.SendPush,
            SendSms          = req.SendSms,
            ShowBanner       = req.ShowBanner,
            BannerCssClass   = req.BannerCssClass ?? "alert-info",
            CreatedByUserId  = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            ExpiresAt        = req.ExpiresAt
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Announcement created." });
    }

    [HttpDelete("announcements/{id:guid}")]
    public async Task<IActionResult> DeleteAnnouncement(Guid id)
    {
        var a = await _db.Announcements.FindAsync(id);
        if (a is null) return NotFound();
        a.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ?? Feature Flags ?????????????????????????????????????????????????????????

    [HttpGet("feature-flags")]
    public async Task<IActionResult> GetFlags()
        => Ok(await _db.FeatureFlags.OrderBy(f => f.Name).ToListAsync());

    [HttpPut("feature-flags/{name}")]
    public async Task<IActionResult> SetFlag(string name, [FromBody] SetFlagRequest req)
    {
        var flag = await _db.FeatureFlags.FirstOrDefaultAsync(f => f.Name == name);
        if (flag is null)
        {
            _db.FeatureFlags.Add(new FeatureFlag
            {
                Id          = Guid.NewGuid(),
                Name        = name,
                Description = req.Description ?? string.Empty,
                Status      = req.Enabled ? FeatureFlagStatus.Enabled : FeatureFlagStatus.Disabled,
                UpdatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!
            });
        }
        else
        {
            flag.Status           = req.Enabled ? FeatureFlagStatus.Enabled : FeatureFlagStatus.Disabled;
            flag.UpdatedByUserId  = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            flag.UpdatedAt        = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ?? Platform Revenue ??????????????????????????????????????????????????????

    [HttpGet("platform-fees")]
    public async Task<IActionResult> GetFees([FromQuery] string? status)
    {
        var query = _db.PlatformFees.AsQueryable();
        if (Enum.TryParse<PlatformFeeStatus>(status, out var s))
            query = query.Where(f => f.Status == s);

        var fees   = await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
        var total  = fees.Where(f => f.Status == PlatformFeeStatus.Collected).Sum(f => f.Amount);
        var pending = fees.Where(f => f.Status == PlatformFeeStatus.Pending).Sum(f => f.Amount);
        return Ok(new { total, pending, fees });
    }

    // ?? Worker of the Month ???????????????????????????????????????????????????

    [HttpPost("worker-of-month")]
    public async Task<IActionResult> NominateWorker([FromBody] WotmRequest req)
    {
        var existing = await _db.WorkerOfTheMonths
            .FirstOrDefaultAsync(w => w.Month == req.Month && w.Year == req.Year && w.IsActive);
        if (existing is not null) { existing.IsActive = false; }

        _db.WorkerOfTheMonths.Add(new WorkerOfTheMonth
        {
            Id                  = Guid.NewGuid(),
            WorkerId            = req.WorkerId,
            Month               = req.Month,
            Year                = req.Year,
            Reason              = req.Reason,
            NominatedByUserId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Worker of the Month nominated." });
    }

    // ?? Testimonials ??????????????????????????????????????????????????????????

    [HttpGet("testimonials")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTestimonials()
        => Ok(await _db.Testimonials.Include(t => t.Worker)
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new { t.Id, t.Headline, t.Story, t.PhotoUrl, t.MonthlyEarnings,
                WorkerName = t.Worker.FullName, SkillCategory = t.Worker.SkillCategory.Name })
            .ToListAsync());

    [HttpPost("testimonials")]
    public async Task<IActionResult> CreateTestimonial([FromBody] TestimonialRequest req)
    {
        _db.Testimonials.Add(new Testimonial
        {
            Id = Guid.NewGuid(), WorkerId = req.WorkerId, Headline = req.Headline,
            Story = req.Story, PhotoUrl = req.PhotoUrl, MonthlyEarnings = req.MonthlyEarnings,
            IsFeatured = req.IsFeatured, DisplayOrder = req.DisplayOrder
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Testimonial created." });
    }

    // ?? Onboarding Slides ?????????????????????????????????????????????????????

    [HttpGet("onboarding-slides")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSlides()
        => Ok(await _db.OnboardingSlides.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToListAsync());

    [HttpPost("onboarding-slides")]
    public async Task<IActionResult> CreateSlide([FromBody] SlideRequest req)
    {
        _db.OnboardingSlides.Add(new OnboardingSlide
        {
            Id = Guid.NewGuid(), Title = req.Title, Description = req.Description,
            ImageUrl = req.ImageUrl, CtaText = req.CtaText, CtaLink = req.CtaLink,
            DisplayOrder = req.DisplayOrder
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Slide created." });
    }

    // Request records
    public record BanRequest(string UserId, string Reason, DateTime? ExpiresAt);
    public record UnbanRequest(string? Reason);
    public record AnnouncementRequest(string Title, string Body, string Target,
        bool SendPush, bool SendSms, bool ShowBanner, string? BannerCssClass, DateTime? ExpiresAt);
    public record SetFlagRequest(bool Enabled, string? Description);
    public record WotmRequest(Guid WorkerId, int Month, int Year, string? Reason);
    public record TestimonialRequest(Guid WorkerId, string Headline, string Story,
        string? PhotoUrl, decimal? MonthlyEarnings, bool IsFeatured, int DisplayOrder);
    public record SlideRequest(string Title, string Description, string? ImageUrl,
        string? CtaText, string? CtaLink, int DisplayOrder);
}
