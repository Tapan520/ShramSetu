using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AnalyticsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public AnalyticsModel(ApplicationDbContext db) => _db = db;

    // KPI tiles
    public int TotalWorkers { get; set; }
    public int VerifiedWorkers { get; set; }
    public int PendingVerifications { get; set; }
    public int OpenSourcingRequests { get; set; }
    public int TotalBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int InProgressBookings { get; set; }
    public int TotalEmployers { get; set; }
    public int TotalReviews { get; set; }
    public double PlatformAverageRating { get; set; }

    // Chart data
    public Dictionary<string, int> WorkersBySkill { get; set; } = new();
    public Dictionary<string, int> BookingsByStatus { get; set; } = new();

    // Monthly worker registrations  last 6 months
    public List<string> MonthLabels { get; set; } = new();
    public List<int> MonthlyRegistrations { get; set; } = new();
    public List<int> MonthlyBookings { get; set; } = new();

    public async Task OnGetAsync()
    {
        TotalWorkers = await _db.Workers.CountAsync();
        VerifiedWorkers = await _db.Workers.CountAsync(w => w.KycStatus == VerificationStatus.Verified);
        PendingVerifications = await _db.Workers.CountAsync(w => w.KycStatus == VerificationStatus.Pending);
        OpenSourcingRequests = await _db.SourcingRequests.CountAsync(r => r.Status == SourcingStatus.Open);
        TotalBookings = await _db.Bookings.CountAsync();
        CompletedBookings = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Completed);
        InProgressBookings = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.InProgress);
        TotalEmployers = await _db.EmployerAccounts.CountAsync();
        TotalReviews = await _db.Reviews.CountAsync();
        PlatformAverageRating = TotalReviews > 0
            ? await _db.Reviews.AverageAsync(r => (double)r.Rating)
            : 0;

        WorkersBySkill = await _db.Workers
            .Include(w => w.SkillCategory)
            .GroupBy(w => w.SkillCategory.Name)
            .Select(g => new { Skill = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToDictionaryAsync(x => x.Skill, x => x.Count);

        BookingsByStatus = Enum.GetValues<BookingStatus>()
            .ToDictionary(
                s => s.ToString(),
                s => _db.Bookings.Count(b => b.Status == s));

        // Monthly trend  last 6 months (UTC)
        var from = DateTime.UtcNow.AddMonths(-5);
        var startOfFrom = new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < 6; i++)
        {
            var month = startOfFrom.AddMonths(i);
            var next  = month.AddMonths(1);
            MonthLabels.Add(month.ToString("MMM yy"));
            MonthlyRegistrations.Add(await _db.Workers.CountAsync(w => w.CreatedAt >= month && w.CreatedAt < next));
            MonthlyBookings.Add(await _db.Bookings.CountAsync(b => b.CreatedAt >= month && b.CreatedAt < next));
        }
    }
}
