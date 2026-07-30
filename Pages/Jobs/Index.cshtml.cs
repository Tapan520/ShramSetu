using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Jobs;

public class JobListItem
{
    public JobPost Job { get; init; } = null!;
    public int ApplicationCount { get; init; }
}

public class JobsIndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public JobsIndexModel(ApplicationDbContext db) => _db = db;

    public IList<JobListItem> Jobs { get; set; } = new List<JobListItem>();
    public IList<SkillCategory> Categories { get; set; } = new List<SkillCategory>();
    public Guid? SkillCategoryId { get; set; }
    public string? City { get; set; }
    public decimal? MinWage { get; set; }
    public decimal? MaxWage { get; set; }
    public string SortBy { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    private const int PageSize = 12;

    public async Task OnGetAsync(Guid? skillCategoryId, string? city,
        decimal? minWage, decimal? maxWage, string? sortBy, int page = 1)
    {
        SkillCategoryId = skillCategoryId;
        City = city;
        MinWage = minWage;
        MaxWage = maxWage;
        SortBy = sortBy ?? "newest";
        Page = Math.Max(1, page);

        Categories = await _db.SkillCategories.OrderBy(c => c.Name).ToListAsync();

        var query = _db.JobPosts
            .Include(j => j.Employer)
            .Include(j => j.SkillCategory)
            .Where(j => j.Status == JobPostStatus.Open)
            .AsQueryable();

        if (skillCategoryId.HasValue) query = query.Where(j => j.SkillCategoryId == skillCategoryId.Value);
        if (!string.IsNullOrWhiteSpace(city)) query = query.Where(j => j.LocationCity != null && j.LocationCity.Contains(city));
        if (minWage.HasValue) query = query.Where(j => j.DailyWage >= minWage.Value);
        if (maxWage.HasValue) query = query.Where(j => j.DailyWage <= maxWage.Value);

        query = SortBy switch
        {
            "wage_asc"  => query.OrderBy(j => j.DailyWage),
            "wage_desc" => query.OrderByDescending(j => j.DailyWage),
            "start"     => query.OrderBy(j => j.StartDate),
            _           => query.OrderByDescending(j => j.CreatedAt)
        };

        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

        var posts = await query
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var jobIds = posts.Select(j => j.Id).ToList();
        var appCounts = await _db.JobApplications
            .Where(a => jobIds.Contains(a.JobPostId))
            .GroupBy(a => a.JobPostId)
            .Select(g => new { JobPostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.JobPostId, x => x.Count);

        Jobs = posts.Select(j => new JobListItem
        {
            Job = j,
            ApplicationCount = appCounts.GetValueOrDefault(j.Id)
        }).ToList();
    }
}
