using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public DashboardModel(ApplicationDbContext db) => _db = db;

    public int TotalWorkers { get; set; }
    public int VerifiedWorkers { get; set; }
    public int PendingKyc { get; set; }
    public int TotalEmployers { get; set; }
    public int ActiveBookings { get; set; }
    public int OpenJobs { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int OpenDisputes { get; set; }

    public string BookingChartLabelsJson { get; set; } = "[]";
    public string BookingChartDataJson   { get; set; } = "[]";
    public string SkillLabelsJson        { get; set; } = "[]";
    public string SkillDataJson          { get; set; } = "[]";
    public string RevenueLabelsJson      { get; set; } = "[]";
    public string RevenueDataJson        { get; set; } = "[]";
    public string StatusLabelsJson       { get; set; } = "[]";
    public string StatusDataJson         { get; set; } = "[]";

    public List<(string FullName, string SkillCategory, string KycStatus)> RecentWorkers { get; set; } = [];
    public List<(string Title, string Type, DateTime CreatedAt)> RecentDisputes { get; set; } = [];

    public async Task OnGetAsync()
    {
        TotalWorkers    = await _db.Workers.CountAsync(w => !w.IsDeleted);
        VerifiedWorkers = await _db.Workers.CountAsync(w => !w.IsDeleted && w.KycStatus == VerificationStatus.Verified);
        PendingKyc      = await _db.Workers.CountAsync(w => !w.IsDeleted && w.KycStatus == VerificationStatus.Pending);
        TotalEmployers  = await _db.EmployerAccounts.CountAsync(e => !e.IsDeleted);
        ActiveBookings  = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.InProgress || b.Status == BookingStatus.Confirmed);
        OpenJobs        = await _db.JobPosts.CountAsync(j => j.Status == JobPostStatus.Open);
        OpenDisputes    = await _db.Disputes.CountAsync(d => d.Status == DisputeStatus.Open);

        var monthStart  = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        // Pull to memory first  SQLite does not support SUM on decimal columns in EF
        var monthlyFees = await _db.PlatformFees
            .Where(f => f.Status == PlatformFeeStatus.Collected && f.CreatedAt >= monthStart)
            .Select(f => f.Amount)
            .ToListAsync();
        MonthlyRevenue = monthlyFees.Sum();

        // Bookings last 30 days
        var since = DateTime.UtcNow.AddDays(-29).Date;
        var daily = await _db.Bookings
            .Where(b => b.CreatedAt >= since)
            .GroupBy(b => b.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var labels = Enumerable.Range(0, 30).Select(i => since.AddDays(i)).ToList();
        BookingChartLabelsJson = JsonSerializer.Serialize(labels.Select(d => d.ToString("dd MMM")));
        BookingChartDataJson   = JsonSerializer.Serialize(labels.Select(d => daily.FirstOrDefault(x => x.Date == d)?.Count ?? 0));

        // Skill distribution
        var skills = await _db.Workers
            .Where(w => !w.IsDeleted)
            .GroupBy(w => w.SkillCategory.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(10)
            .ToListAsync();
        SkillLabelsJson = JsonSerializer.Serialize(skills.Select(s => s.Name));
        SkillDataJson   = JsonSerializer.Serialize(skills.Select(s => s.Count));

        // Revenue last 6 months  pull to memory to avoid SQLite decimal SUM limitation
        var revMonths = Enumerable.Range(0, 6)
            .Select(i => DateTime.UtcNow.AddMonths(-i)).Reverse().ToList();
        var allRevFees = await _db.PlatformFees
            .Where(f => f.Status == PlatformFeeStatus.Collected && f.CreatedAt >= DateTime.UtcNow.AddMonths(-6))
            .Select(f => new { f.CreatedAt.Year, f.CreatedAt.Month, f.Amount })
            .ToListAsync();
        var revData = allRevFees
            .GroupBy(f => new { f.Year, f.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(f => f.Amount) })
            .ToList();
        RevenueLabelsJson = JsonSerializer.Serialize(revMonths.Select(m => m.ToString("MMM yy")));
        RevenueDataJson   = JsonSerializer.Serialize(revMonths.Select(m =>
            revData.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month)?.Total ?? 0));

        // Booking status split
        var statuses = await _db.Bookings
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();
        StatusLabelsJson = JsonSerializer.Serialize(statuses.Select(s => s.Status));
        StatusDataJson   = JsonSerializer.Serialize(statuses.Select(s => s.Count));

        // Recent workers
        RecentWorkers = await _db.Workers
            .Include(w => w.SkillCategory)
            .Where(w => !w.IsDeleted)
            .OrderByDescending(w => w.CreatedAt)
            .Take(5)
            .Select(w => ValueTuple.Create(w.FullName, w.SkillCategory.Name, w.KycStatus.ToString()))
            .ToListAsync();

        // Recent disputes
        RecentDisputes = await _db.Disputes
            .Where(d => d.Status == DisputeStatus.Open)
            .OrderByDescending(d => d.CreatedAt)
            .Take(5)
            .Select(d => ValueTuple.Create(d.Title, d.Type.ToString(), d.CreatedAt))
            .ToListAsync();
    }
}
