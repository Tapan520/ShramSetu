using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Api.Dtos;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/workers")]
[Produces("application/json")]
public class WorkersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public WorkersController(ApplicationDbContext db) => _db = db;

    /// <summary>Get all skill categories.</summary>
    [HttpGet("categories")]
    public async Task<ActionResult<IList<SkillCategoryDto>>> GetCategories()
    {
        var cats = await _db.SkillCategories.OrderBy(c => c.Name).ToListAsync();
        return Ok(cats.Select(c => new SkillCategoryDto(c.Id, c.Name, c.IconCssClass)));
    }

    /// <summary>Search / browse workers with optional filters and pagination.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<WorkerCardDto>>> Search(
        [FromQuery] Guid? skillCategoryId,
        [FromQuery] string? city,
        [FromQuery] decimal? maxWage,
        [FromQuery] int? minExperience,
        [FromQuery] double? minRating,
        [FromQuery] bool verifiedOnly = false,
        [FromQuery] string sortBy = "newest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page     = Math.Max(1, page);

        var query = _db.Workers.Include(w => w.SkillCategory).AsQueryable();

        if (skillCategoryId.HasValue) query = query.Where(w => w.SkillCategoryId == skillCategoryId.Value);
        if (!string.IsNullOrWhiteSpace(city)) query = query.Where(w => w.LocationCity != null && w.LocationCity.Contains(city));
        if (maxWage.HasValue)      query = query.Where(w => w.ExpectedDailyWage <= maxWage.Value);
        if (minExperience.HasValue) query = query.Where(w => w.YearsOfExperience >= minExperience.Value);
        if (verifiedOnly)           query = query.Where(w => w.KycStatus == VerificationStatus.Verified);

        var workers = await query.ToListAsync();
        var workerIds = workers.Select(w => w.Id).ToList();

        var reviewStats = await _db.Reviews
            .Where(r => workerIds.Contains(r.WorkerId))
            .GroupBy(r => r.WorkerId)
            .Select(g => new { WorkerId = g.Key, Avg = g.Average(r => r.Rating), Count = g.Count() })
            .ToListAsync();

        var completedCounts = await _db.Bookings
            .Where(b => workerIds.Contains(b.WorkerId) && b.Status == BookingStatus.Completed)
            .GroupBy(b => b.WorkerId)
            .Select(g => new { WorkerId = g.Key, Count = g.Count() })
            .ToListAsync();

        var reviewDict    = reviewStats.ToDictionary(x => x.WorkerId, x => (x.Avg, x.Count));
        var completedDict = completedCounts.ToDictionary(x => x.WorkerId, x => x.Count);

        var summaries = workers.Select(w =>
        {
            reviewDict.TryGetValue(w.Id, out var rv);
            completedDict.TryGetValue(w.Id, out var cc);
            return new
            {
                Worker = w,
                AverageRating = rv.Avg,
                ReviewCount = rv.Count,
                CompletedJobCount = cc
            };
        });

        if (minRating.HasValue)
            summaries = summaries.Where(s => s.AverageRating >= minRating.Value);

        summaries = sortBy switch
        {
            "rating"    => summaries.OrderByDescending(s => s.AverageRating),
            "wage_asc"  => summaries.OrderBy(s => s.Worker.ExpectedDailyWage),
            "wage_desc" => summaries.OrderByDescending(s => s.Worker.ExpectedDailyWage),
            "experience"=> summaries.OrderByDescending(s => s.Worker.YearsOfExperience),
            "jobs"      => summaries.OrderByDescending(s => s.CompletedJobCount),
            _           => summaries.OrderByDescending(s => s.Worker.CreatedAt)
        };

        var totalCount = summaries.Count();
        var items = summaries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new WorkerCardDto(
                s.Worker.Id,
                s.Worker.FullName,
                s.Worker.SkillCategory.Name,
                s.Worker.LocationCity,
                s.Worker.LocationState,
                s.Worker.YearsOfExperience,
                s.Worker.ExpectedDailyWage,
                s.Worker.KycStatus.ToString(),
                s.Worker.IsAvailable,
                s.Worker.PhotoUrl,
                s.AverageRating,
                s.ReviewCount,
                s.CompletedJobCount))
            .ToList();

        return Ok(new PagedResult<WorkerCardDto>(items, page, pageSize, totalCount));
    }

    /// <summary>Get a single worker's full public profile.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkerProfileDto>> GetProfile(Guid id)
    {
        var w = await _db.Workers
            .Include(w => w.SkillCategory)
            .Include(w => w.Documents)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (w is null) return NotFound();

        var reviews = await _db.Reviews
            .Where(r => r.WorkerId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .ToListAsync();

        var avgRating  = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
        var completed  = await _db.Bookings.CountAsync(b => b.WorkerId == id && b.Status == BookingStatus.Completed);
        var totalBooks = await _db.Bookings.CountAsync(b => b.WorkerId == id);

        return Ok(new WorkerProfileDto(
            w.Id, w.FullName, w.Phone,
            w.SkillCategory.Name, w.SkillCategoryId,
            w.SubSkills,
            w.YearsOfExperience, w.ExpectedDailyWage, w.ExpectedMonthlyWage,
            w.LocationCity, w.LocationState,
            w.IsAvailable, w.PhotoUrl,
            w.KycStatus.ToString(),
            avgRating, reviews.Count, completed, totalBooks,
            reviews.Select(r => new ReviewDto(r.Id, r.Rating, r.Comment, r.CreatedAt)).ToList(),
            w.Documents.Select(d => new DocumentDto(d.Id, d.Type.ToString(), d.FileUrl, d.IsVerified)).ToList()
        ));
    }

    /// <summary>Get the authenticated worker's own profile.</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Worker")]
    public async Task<ActionResult<WorkerProfileDto>> GetMyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var w = await _db.Workers.Include(w => w.SkillCategory).Include(w => w.Documents)
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (w is null) return NotFound();

        return await GetProfile(w.Id);
    }

    /// <summary>Update the authenticated worker's profile.</summary>
    [HttpPut("me")]
    [Authorize(Roles = "Worker")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateWorkerProfileRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var w = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (w is null) return NotFound();

        w.FullName = req.FullName;
        w.Phone = req.Phone;
        w.SubSkills = req.SubSkills;
        w.YearsOfExperience = req.YearsOfExperience;
        w.ExpectedDailyWage = req.ExpectedDailyWage;
        w.ExpectedMonthlyWage = req.ExpectedMonthlyWage;
        w.LocationCity = req.LocationCity;
        w.LocationState = req.LocationState;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Toggle the worker's availability flag.</summary>
    [HttpPost("me/toggle-availability")]
    [Authorize(Roles = "Worker")]
    public async Task<ActionResult<object>> ToggleAvailability()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var w = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (w is null) return NotFound();

        w.IsAvailable = !w.IsAvailable;
        await _db.SaveChangesAsync();
        return Ok(new { isAvailable = w.IsAvailable });
    }
}
