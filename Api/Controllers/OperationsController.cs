using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;
using ShramSetu.Services;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/contracts")]
[Authorize]
[Produces("application/json")]
public class ContractsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPdfService _pdf;

    public ContractsController(ApplicationDbContext db, IPdfService pdf)
    {
        _db  = db;
        _pdf = pdf;
    }

    [HttpGet("booking/{bookingId:guid}")]
    public async Task<IActionResult> GetForBooking(Guid bookingId)
    {
        var contract = await _db.WorkContracts
            .Include(c => c.Booking).ThenInclude(b => b.Worker)
            .Include(c => c.Booking).ThenInclude(b => b.Employer)
            .FirstOrDefaultAsync(c => c.BookingId == bookingId);

        if (contract is null) return NotFound();
        return Ok(contract);
    }

    [HttpPost("booking/{bookingId:guid}/generate")]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<IActionResult> Generate(Guid bookingId)
    {
        var booking = await _db.Bookings
            .Include(b => b.Worker)
            .Include(b => b.Employer)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null) return NotFound();

        var existing = await _db.WorkContracts.FirstOrDefaultAsync(c => c.BookingId == bookingId);
        if (existing is not null)
            return Ok(new { message = "Contract already exists.", existing.Id, existing.Status });

        var content = GenerateContractText(booking);
        var contract = new WorkContract
        {
            Id        = Guid.NewGuid(),
            BookingId = bookingId,
            Content   = content,
            Status    = ContractStatus.Draft,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        _db.WorkContracts.Add(contract);
        await _db.SaveChangesAsync();

        return Ok(new { contract.Id, contract.Status, message = "Contract generated." });
    }

    [HttpPost("{id:guid}/sign")]
    public async Task<IActionResult> Sign(Guid id)
    {
        var contract = await _db.WorkContracts.Include(c => c.Booking).FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker   = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);

        if (worker?.Id == contract.Booking.WorkerId)
        {
            contract.WorkerSignedAt      = DateTime.UtcNow;
            contract.WorkerSignatureRef  = $"WORKER_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
        else if (employer?.Id == contract.Booking.EmployerId || User.IsInRole("Admin"))
        {
            contract.EmployerSignedAt      = DateTime.UtcNow;
            contract.EmployerSignatureRef  = $"EMPLOYER_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
        else return Forbid();

        if (contract.WorkerSignedAt.HasValue && contract.EmployerSignedAt.HasValue)
            contract.Status = ContractStatus.Signed;

        await _db.SaveChangesAsync();
        return Ok(new { contract.Status, contract.WorkerSignedAt, contract.EmployerSignedAt });
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var contract = await _db.WorkContracts
            .Include(c => c.Booking).ThenInclude(b => b.Worker)
            .Include(c => c.Booking).ThenInclude(b => b.Employer)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null) return NotFound();

        var bytes = _pdf.GenerateWorkContract(contract);
        return File(bytes, "application/pdf", $"contract_{id}.pdf");
    }

    private static string GenerateContractText(Booking b) =>
        $"""
        WORK AGREEMENT

        This agreement is entered into between:

        EMPLOYER: {b.Employer.Name} ({b.Employer.Phone})
        WORKER:   {b.Worker.FullName} ({b.Worker.Phone})

        Terms:
        - Start Date:    {b.StartDate:dd MMM yyyy}
        - Duration:      {b.DurationDays} day(s)
        - Agreed Wage:   ₹{b.AgreedWage} per day
        - Total Value:   ₹{b.AgreedWage * b.DurationDays}

        Notes: {b.Notes ?? "None"}

        Both parties agree to the terms and conditions of ShramSetu platform.
        Disputes shall be raised via the ShramSetu app within 7 days of completion.
        """;
}

[ApiController]
[Route("api/wage-rates")]
[Produces("application/json")]
public class WageRateCardsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public WageRateCardsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? skillCategoryId, [FromQuery] string? city)
    {
        var query = _db.WageRateCards
            .Include(w => w.SkillCategory)
            .Where(w => w.EffectiveTo == null || w.EffectiveTo >= DateTime.UtcNow)
            .AsQueryable();

        if (skillCategoryId.HasValue) query = query.Where(w => w.SkillCategoryId == skillCategoryId.Value);
        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(w => w.City.Contains(city));

        var rates = await query.OrderBy(w => w.SkillCategory.Name).ToListAsync();
        return Ok(rates.Select(r => new {
            r.Id, Skill = r.SkillCategory.Name, r.City, r.State,
            r.MinDailyWage, r.MaxDailyWage, r.RecommendedDailyWage, r.EffectiveFrom
        }));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Upsert([FromBody] WageRateRequest req)
    {
        var existing = await _db.WageRateCards
            .FirstOrDefaultAsync(w => w.SkillCategoryId == req.SkillCategoryId
                && w.City == req.City && (w.EffectiveTo == null || w.EffectiveTo >= DateTime.UtcNow));

        if (existing is not null)
        {
            existing.MinDailyWage         = req.MinDailyWage;
            existing.MaxDailyWage         = req.MaxDailyWage;
            existing.RecommendedDailyWage = req.RecommendedDailyWage;
            existing.UpdatedAt            = DateTime.UtcNow;
            existing.UpdatedByUserId      = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        else
        {
            _db.WageRateCards.Add(new WageRateCard
            {
                Id                    = Guid.NewGuid(),
                SkillCategoryId       = req.SkillCategoryId,
                City                  = req.City,
                State                 = req.State,
                MinDailyWage          = req.MinDailyWage,
                MaxDailyWage          = req.MaxDailyWage,
                RecommendedDailyWage  = req.RecommendedDailyWage,
                UpdatedByUserId       = User.FindFirstValue(ClaimTypes.NameIdentifier)
            });
        }
        await _db.SaveChangesAsync();
        return Ok(new { message = "Wage rate saved." });
    }

    public record WageRateRequest(Guid SkillCategoryId, string City, string? State,
        decimal MinDailyWage, decimal MaxDailyWage, decimal RecommendedDailyWage);
}

[ApiController]
[Route("api/notification-preferences")]
[Authorize]
[Produces("application/json")]
public class NotificationPreferencesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public NotificationPreferencesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var prefs  = await _db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId)
                  ?? new NotificationPreference { UserId = userId };
        return Ok(prefs);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] NotificationPreference prefs)
    {
        var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var existing = await _db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        if (existing is null)
        {
            prefs.Id = Guid.NewGuid();
            prefs.UserId = userId;
            prefs.UpdatedAt = DateTime.UtcNow;
            _db.NotificationPreferences.Add(prefs);
        }
        else
        {
            existing.BookingUpdates_SMS       = prefs.BookingUpdates_SMS;
            existing.BookingUpdates_Push      = prefs.BookingUpdates_Push;
            existing.BookingUpdates_WhatsApp  = prefs.BookingUpdates_WhatsApp;
            existing.JobAlerts_SMS            = prefs.JobAlerts_SMS;
            existing.JobAlerts_Push           = prefs.JobAlerts_Push;
            existing.PaymentNotifications_SMS = prefs.PaymentNotifications_SMS;
            existing.PaymentNotifications_Push= prefs.PaymentNotifications_Push;
            existing.ChatMessages_Push        = prefs.ChatMessages_Push;
            existing.SystemAnnouncements_SMS  = prefs.SystemAnnouncements_SMS;
            existing.SystemAnnouncements_Push = prefs.SystemAnnouncements_Push;
            existing.UpdatedAt                = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
