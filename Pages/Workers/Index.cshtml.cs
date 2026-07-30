using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Core.Models;
using ShramSetu.Data;

namespace ShramSetu.Pages.Workers;

public class WorkersIndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public WorkersIndexModel(ApplicationDbContext db) => _db = db;

    public IList<SkillCategory> Categories { get; set; } = new List<SkillCategory>();
    public IList<WorkerSummary> Workers { get; set; } = new List<WorkerSummary>();

    // Filter params
    public Guid? SkillCategoryId { get; set; }
    public string? City { get; set; }
    public decimal? MaxWage { get; set; }
    public int? MinExperience { get; set; }
    public double? MinRating { get; set; }
    public bool VerifiedOnly { get; set; }
    public string? SortBy { get; set; }

    public async Task OnGetAsync(
        Guid? skillCategoryId,
        string? city,
        decimal? maxWage,
        int? minExperience,
        double? minRating,
        bool verifiedOnly = false,
        string? sortBy = "newest")
    {
        SkillCategoryId = skillCategoryId;
        City = city;
        MaxWage = maxWage;
        MinExperience = minExperience;
        MinRating = minRating;
        VerifiedOnly = verifiedOnly;
        SortBy = sortBy;

        Categories = await _db.SkillCategories.OrderBy(c => c.Name).ToListAsync();

        var query = _db.Workers
            .Include(w => w.SkillCategory)
            .AsQueryable();

        if (skillCategoryId.HasValue)
            query = query.Where(w => w.SkillCategoryId == skillCategoryId.Value);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(w => w.LocationCity != null && w.LocationCity.Contains(city));

        if (maxWage.HasValue)
            query = query.Where(w => w.ExpectedDailyWage <= maxWage.Value);

        if (minExperience.HasValue)
            query = query.Where(w => w.YearsOfExperience >= minExperience.Value);

        if (verifiedOnly)
            query = query.Where(w => w.KycStatus == VerificationStatus.Verified);

        // Build summary projections with ratings and completed job count
        var workerList = await query.ToListAsync();
        var workerIds = workerList.Select(w => w.Id).ToList();

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

        var reviewDict = reviewStats.ToDictionary(x => x.WorkerId, x => (x.Avg, x.Count));
        var completedDict = completedCounts.ToDictionary(x => x.WorkerId, x => x.Count);

        var summaries = workerList.Select(w => new WorkerSummary
        {
            Worker = w,
            AverageRating = reviewDict.TryGetValue(w.Id, out var rv) ? rv.Avg : 0,
            ReviewCount = reviewDict.TryGetValue(w.Id, out var rc) ? rc.Count : 0,
            CompletedJobCount = completedDict.TryGetValue(w.Id, out var cc) ? cc : 0
        });

        if (minRating.HasValue)
            summaries = summaries.Where(s => s.AverageRating >= minRating.Value);

        Workers = (sortBy switch
        {
            "rating"   => summaries.OrderByDescending(s => s.AverageRating),
            "wage_asc" => summaries.OrderBy(s => s.Worker.ExpectedDailyWage),
            "wage_desc"=> summaries.OrderByDescending(s => s.Worker.ExpectedDailyWage),
            "experience"=> summaries.OrderByDescending(s => s.Worker.YearsOfExperience),
            "jobs"     => summaries.OrderByDescending(s => s.CompletedJobCount),
            _          => summaries.OrderByDescending(s => s.Worker.CreatedAt)
        }).ToList();
    }
}
