using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Api.Dtos;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminController(ApplicationDbContext db) => _db = db;

    // ?? Workers ??????????????????????????????????????????????????????????????

    /// <summary>List workers filtered by KYC status.</summary>
    [HttpGet("workers")]
    public async Task<ActionResult<PagedResult<WorkerCardDto>>> GetWorkers(
        [FromQuery] string status = "Pending",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page     = Math.Max(1, page);

        var kycStatus = Enum.TryParse<VerificationStatus>(status, out var s) ? s : VerificationStatus.Pending;

        var total = await _db.Workers.CountAsync(w => w.KycStatus == kycStatus);
        var workers = await _db.Workers
            .Include(w => w.SkillCategory)
            .Where(w => w.KycStatus == kycStatus)
            .OrderBy(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = workers.Select(w => new WorkerCardDto(
            w.Id, w.FullName, w.SkillCategory.Name,
            w.LocationCity, w.LocationState,
            w.YearsOfExperience, w.ExpectedDailyWage,
            w.KycStatus.ToString(), w.IsAvailable, w.PhotoUrl,
            0, 0, 0)).ToList();

        return Ok(new PagedResult<WorkerCardDto>(items, page, pageSize, total));
    }

    /// <summary>Verify, reject or mark a worker's KYC as under review.</summary>
    [HttpPost("workers/{id:guid}/verify")]
    public async Task<IActionResult> VerifyWorker(Guid id, [FromBody] WorkerVerifyRequest req)
    {
        var worker = await _db.Workers.Include(w => w.Documents).FirstOrDefaultAsync(w => w.Id == id);
        if (worker is null) return NotFound();

        worker.KycStatus = req.Action switch
        {
            "Verify"      => VerificationStatus.Verified,
            "Reject"      => VerificationStatus.Rejected,
            "UnderReview" => VerificationStatus.UnderReview,
            _             => worker.KycStatus
        };

        if (req.Action == "Verify")
            foreach (var doc in worker.Documents)
                doc.IsVerified = true;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ?? Sourcing Requests ?????????????????????????????????????????????????????

    /// <summary>List sourcing requests by status.</summary>
    [HttpGet("sourcing")]
    public async Task<ActionResult<PagedResult<SourcingRequestDto>>> GetSourcingRequests(
        [FromQuery] string status = "Open",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page     = Math.Max(1, page);

        var s = Enum.TryParse<SourcingStatus>(status, out var ps) ? ps : SourcingStatus.Open;

        var total = await _db.SourcingRequests.CountAsync(r => r.Status == s);
        var items = await _db.SourcingRequests
            .Include(r => r.SkillCategory)
            .Include(r => r.Employer)
            .Where(r => r.Status == s)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<SourcingRequestDto>(
            items.Select(r => new SourcingRequestDto(
                r.Id, r.SkillCategory.Name, r.WorkerCount, r.DurationDays, r.BudgetPerDay,
                r.LocationCity, r.LocationState, r.Description,
                r.Status.ToString(), r.AdminNotes, r.CreatedAt, r.FulfilledAt)).ToList(),
            page, pageSize, total));
    }

    /// <summary>Update a sourcing request status + admin notes.</summary>
    [HttpPost("sourcing/{id:guid}/status")]
    public async Task<IActionResult> UpdateSourcingStatus(Guid id, [FromBody] UpdateSourcingStatusRequest req)
    {
        var r = await _db.SourcingRequests.FindAsync(id);
        if (r is null) return NotFound();

        if (Enum.TryParse<SourcingStatus>(req.Status, out var s))
            r.Status = s;

        if (!string.IsNullOrWhiteSpace(req.AdminNotes))
            r.AdminNotes = req.AdminNotes;

        if (r.Status == SourcingStatus.Fulfilled)
            r.FulfilledAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ?? Bookings ??????????????????????????????????????????????????????????????

    /// <summary>Update a booking's status.</summary>
    [HttpPost("bookings/{id:guid}/status")]
    public async Task<IActionResult> UpdateBookingStatus(Guid id, [FromBody] UpdateBookingStatusRequest req)
    {
        var booking = await _db.Bookings.FindAsync(id);
        if (booking is null) return NotFound();

        if (Enum.TryParse<BookingStatus>(req.Status, out var s))
            booking.Status = s;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ?? Analytics ?????????????????????????????????????????????????????????????

    /// <summary>Get platform-wide analytics summary.</summary>
    [HttpGet("analytics")]
    public async Task<ActionResult<AnalyticsSummaryDto>> GetAnalytics()
    {
        var totalWorkers      = await _db.Workers.CountAsync();
        var verifiedWorkers   = await _db.Workers.CountAsync(w => w.KycStatus == VerificationStatus.Verified);
        var pendingVerif      = await _db.Workers.CountAsync(w => w.KycStatus == VerificationStatus.Pending);
        var totalEmployers    = await _db.EmployerAccounts.CountAsync();
        var totalBookings     = await _db.Bookings.CountAsync();
        var completedBookings = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Completed);
        var openSourcing      = await _db.SourcingRequests.CountAsync(r => r.Status == SourcingStatus.Open);
        var totalReviews      = await _db.Reviews.CountAsync();
        var avgRating         = totalReviews > 0 ? await _db.Reviews.AverageAsync(r => (double)r.Rating) : 0;
        var totalJobPosts     = await _db.JobPosts.CountAsync();
        var openJobPosts      = await _db.JobPosts.CountAsync(j => j.Status == JobPostStatus.Open);
        var totalApplications = await _db.JobApplications.CountAsync();

        var workersBySkill = await _db.Workers
            .Include(w => w.SkillCategory)
            .GroupBy(w => w.SkillCategory.Name)
            .Select(g => new { Skill = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToDictionaryAsync(x => x.Skill, x => x.Count);

        var bookingsByStatus = Enum.GetValues<BookingStatus>()
            .ToDictionary(s => s.ToString(), s => _db.Bookings.Count(b => b.Status == s));

        return Ok(new AnalyticsSummaryDto(
            totalWorkers, verifiedWorkers, pendingVerif, totalEmployers,
            totalBookings, completedBookings, openSourcing,
            avgRating, totalReviews,
            totalJobPosts, openJobPosts, totalApplications,
            workersBySkill, bookingsByStatus));
    }
}
