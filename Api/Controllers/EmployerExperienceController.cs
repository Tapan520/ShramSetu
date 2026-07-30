using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;
using ShramSetu.Services;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/employer")]
[Authorize(Roles = "Employer,Admin")]
[Produces("application/json")]
public class EmployerExperienceController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IComplianceService _compliance;
    private readonly IGstInvoiceService _gst;

    public EmployerExperienceController(
        ApplicationDbContext db, IComplianceService compliance, IGstInvoiceService gst)
    {
        _db = db; _compliance = compliance; _gst = gst;
    }

    // ?? Job Post Templates ????????????????????????????????????????????????????

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates()
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null) return Ok(Array.Empty<object>());

        var templates = await _db.JobPostTemplates
            .Include(t => t.SkillCategory)
            .Where(t => t.EmployerId == employer.Id)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return Ok(templates.Select(t => new { t.Id, t.Name, t.Title, t.Description,
            SkillCategory = t.SkillCategory.Name, t.LocationCity, t.DailyWage, t.DurationDays, t.VacancyCount }));
    }

    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] TemplateRequest req)
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null) return BadRequest(new { message = "Employer profile not found." });

        _db.JobPostTemplates.Add(new JobPostTemplate
        {
            Id = Guid.NewGuid(), EmployerId = employer.Id, Name = req.Name,
            Title = req.Title, Description = req.Description, SkillCategoryId = req.SkillCategoryId,
            LocationCity = req.LocationCity, LocationState = req.LocationState,
            DailyWage = req.DailyWage, DurationDays = req.DurationDays, VacancyCount = req.VacancyCount
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Template saved." });
    }

    [HttpDelete("templates/{id:guid}")]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        var t = await _db.JobPostTemplates.FindAsync(id);
        if (t is null || t.EmployerId != employer?.Id) return NotFound();
        _db.JobPostTemplates.Remove(t);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ?? GPS Radius Worker Search ??????????????????????????????????????????????

    [HttpGet("workers/nearby")]
    public async Task<IActionResult> GetNearbyWorkers(
        [FromQuery] double lat, [FromQuery] double lng,
        [FromQuery] double radiusKm = 10,
        [FromQuery] Guid? skillCategoryId = null)
    {
        // Haversine approximation using bounding box (SQLite compatible)
        double latDelta = radiusKm / 111.0;
        double lngDelta = radiusKm / (111.0 * Math.Cos(lat * Math.PI / 180));

        var workers = await _db.Workers
            .Include(w => w.SkillCategory)
            .Where(w => !w.IsDeleted && w.IsAvailable
                && w.Latitude.HasValue && w.Longitude.HasValue
                && w.Latitude >= lat - latDelta && w.Latitude <= lat + latDelta
                && w.Longitude >= lng - lngDelta && w.Longitude <= lng + lngDelta
                && (!skillCategoryId.HasValue || w.SkillCategoryId == skillCategoryId))
            .ToListAsync();

        // Precise Haversine filter
        var results = workers
            .Select(w => new
            {
                w.Id, w.FullName, w.PhotoUrl, SkillCategory = w.SkillCategory.Name,
                w.LocationCity, w.ExpectedDailyWage, w.KycStatus,
                DistanceKm = HaversineKm(lat, lng, w.Latitude!.Value, w.Longitude!.Value)
            })
            .Where(x => x.DistanceKm <= radiusKm)
            .OrderBy(x => x.DistanceKm)
            .ToList();

        return Ok(results);
    }

    // ?? Compliance Check ??????????????????????????????????????????????????????

    [HttpGet("compliance/{bookingId:guid}")]
    public async Task<IActionResult> GetCompliance(Guid bookingId)
    {
        var checks = await _compliance.CheckBookingAsync(bookingId);
        return Ok(checks);
    }

    // ?? GST Invoice ???????????????????????????????????????????????????????????

    [HttpPost("gst-invoice/{subscriptionId:guid}")]
    public async Task<IActionResult> GenerateGstInvoice(Guid subscriptionId,
        [FromQuery] string? gstin, [FromQuery] string? address)
    {
        var invoice = await _gst.GenerateForSubscriptionAsync(
            subscriptionId, gstin ?? string.Empty, address ?? "India");
        return Ok(new { invoice.Id, invoice.InvoiceNumber, invoice.TotalAmount, invoice.Status });
    }

    [HttpGet("gst-invoice/{id:guid}/pdf")]
    public async Task<IActionResult> DownloadGstInvoice(Guid id)
    {
        var invoice = await _db.GstInvoices.Include(g => g.Subscription).FirstOrDefaultAsync(g => g.Id == id);
        if (invoice is null) return NotFound();
        var pdf = _gst.GeneratePdf(invoice);
        return File(pdf, "application/pdf", $"invoice_{invoice.InvoiceNumber}.pdf");
    }

    // ?? Minimum Wage Config (Admin only) ??????????????????????????????????????

    [HttpGet("minimum-wages")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMinWages([FromQuery] string? state, [FromQuery] Guid? skillCategoryId)
    {
        var query = _db.MinimumWageConfigs.Include(m => m.SkillCategory).AsQueryable();
        if (!string.IsNullOrEmpty(state)) query = query.Where(m => m.State == state);
        if (skillCategoryId.HasValue) query = query.Where(m => m.SkillCategoryId == skillCategoryId.Value);
        return Ok(await query.OrderBy(m => m.State).ToListAsync());
    }

    [HttpPost("minimum-wages")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetMinWage([FromBody] MinWageRequest req)
    {
        _db.MinimumWageConfigs.Add(new MinimumWageConfig
        {
            Id = Guid.NewGuid(), State = req.State, SkillCategoryId = req.SkillCategoryId,
            MinDailyWage = req.MinDailyWage, EffectiveFrom = req.EffectiveFrom, Reference = req.Reference
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Minimum wage saved." });
    }

    // ?? Session Management ????????????????????????????????????????????????????

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public record TemplateRequest(string Name, string Title, string Description,
        Guid SkillCategoryId, string? LocationCity, string? LocationState,
        decimal DailyWage, int DurationDays, int VacancyCount);
    public record MinWageRequest(string State, Guid SkillCategoryId,
        decimal MinDailyWage, DateTime EffectiveFrom, string? Reference);
}
