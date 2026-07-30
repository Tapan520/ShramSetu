using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;
using ShramSetu.Services;

namespace ShramSetu.Pages.Jobs;

[Authorize(Roles = "Employer,Admin")]
public class ApplicationsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notify;

    public ApplicationsModel(ApplicationDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    public JobPost? Job { get; set; }
    public IList<JobApplication> Applications { get; set; } = new List<JobApplication>();

    public async Task OnGetAsync(Guid id)
    {
        await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostAsync(Guid id, Guid applicationId, string action)
    {
        await LoadAsync(id);

        var app = Applications.FirstOrDefault(a => a.Id == applicationId);
        if (app is null) return NotFound();

        if (!Enum.TryParse<JobApplicationStatus>(action, out var newStatus))
            return BadRequest();

        app.Status = newStatus;
        app.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Notify worker
        if (app.Worker.UserId is not null && Job is not null)
        {
            var msg = newStatus switch
            {
                JobApplicationStatus.Accepted    => $"Congratulations! Your application for '{Job.Title}' has been accepted.",
                JobApplicationStatus.Shortlisted => $"Good news! You've been shortlisted for '{Job.Title}'.",
                JobApplicationStatus.Rejected    => $"Your application for '{Job.Title}' was not selected this time.",
                _                                => $"Your application status updated to {newStatus}."
            };
            await _notify.SendAsync(app.Worker.UserId, msg, NotificationChannel.SMS);
        }

        TempData["Success"] = $"Application marked as {newStatus}.";
        return RedirectToPage(new { id });
    }

    private async Task LoadAsync(Guid id)
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);

        Job = await _db.JobPosts
            .Include(j => j.Employer)
            .Include(j => j.SkillCategory)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (Job is null) return;

        Applications = await _db.JobApplications
            .Include(a => a.Worker)
            .Where(a => a.JobPostId == id)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();
    }
}
