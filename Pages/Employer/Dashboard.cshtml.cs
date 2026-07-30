using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Employer;

[Authorize(Roles = "Employer,Admin")]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public DashboardModel(ApplicationDbContext db) => _db = db;

    public string EmployerName { get; set; } = string.Empty;
    public int ActiveBookings { get; set; }
    public int OpenJobPosts { get; set; }
    public int PendingApplications { get; set; }
    public int SavedWorkerCount { get; set; }
    public decimal TotalSpent { get; set; }
    public string SubscriptionTier { get; set; } = "Free";
    public List<(string WorkerName, string SkillCategory, string Status)> RecentBookings { get; set; } = [];
    public List<(string WorkerName, string JobTitle, string Status)> RecentApplications { get; set; } = [];

    public async Task OnGetAsync()
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null) return;

        EmployerName = employer.Name;

        ActiveBookings = await _db.Bookings.CountAsync(b => b.EmployerId == employer.Id
            && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress));

        OpenJobPosts = await _db.JobPosts.CountAsync(j => j.EmployerId == employer.Id && j.Status == JobPostStatus.Open);

        PendingApplications = await _db.JobApplications
            .CountAsync(a => a.JobPost.EmployerId == employer.Id && a.Status == JobApplicationStatus.Applied);

        SavedWorkerCount = await _db.SavedWorkers.CountAsync(s => s.EmployerId == employer.Id);

        var completedWages = await _db.Bookings
            .Where(b => b.EmployerId == employer.Id && b.Status == BookingStatus.Completed)
            .Select(b => b.AgreedWage)
            .ToListAsync();
        TotalSpent = completedWages.Sum();

        var activeSub = await _db.EmployerSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.EmployerId == employer.Id && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();
        SubscriptionTier = activeSub?.Plan?.Name ?? "Free";

        RecentBookings = await _db.Bookings
            .Include(b => b.Worker).ThenInclude(w => w.SkillCategory)
            .Where(b => b.EmployerId == employer.Id
                && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress))
            .OrderByDescending(b => b.CreatedAt).Take(5)
            .Select(b => ValueTuple.Create(b.Worker.FullName, b.Worker.SkillCategory.Name, b.Status.ToString()))
            .ToListAsync();

        RecentApplications = await _db.JobApplications
            .Include(a => a.Worker)
            .Include(a => a.JobPost)
            .Where(a => a.JobPost.EmployerId == employer.Id && a.Status == JobApplicationStatus.Applied)
            .OrderByDescending(a => a.AppliedAt).Take(5)
            .Select(a => ValueTuple.Create(a.Worker.FullName, a.JobPost.Title, a.Status.ToString()))
            .ToListAsync();
    }
}
