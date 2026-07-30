using System.ComponentModel.DataAnnotations;
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

[Authorize(Roles = "Worker")]
public class ApplyModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notify;

    public ApplyModel(ApplicationDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    public JobPost? Job { get; set; }

    [BindProperty]
    public ApplyInputModel Input { get; set; } = new();

    public async Task OnGetAsync(Guid id)
    {
        Job = await LoadJobAsync(id);
        Input.JobPostId = id;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Job = await LoadJobAsync(Input.JobPostId);
        if (Job is null) return NotFound();

        if (!ModelState.IsValid) return Page();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null)
        {
            ModelState.AddModelError(string.Empty, "Worker profile not found.");
            return Page();
        }

        if (Job.Status != JobPostStatus.Open)
        {
            ModelState.AddModelError(string.Empty, "This job is no longer accepting applications.");
            return Page();
        }

        var duplicate = await _db.JobApplications
            .AnyAsync(a => a.JobPostId == Input.JobPostId && a.WorkerId == worker.Id);
        if (duplicate)
        {
            ModelState.AddModelError(string.Empty, "You have already applied to this job.");
            return Page();
        }

        _db.JobApplications.Add(new JobApplication
        {
            Id        = Guid.NewGuid(),
            JobPostId = Input.JobPostId,
            WorkerId  = worker.Id,
            CoverNote = Input.CoverNote
        });
        await _db.SaveChangesAsync();

        // Notify employer
        if (Job.Employer.UserId is not null)
        {
            await _notify.SendAsync(
                Job.Employer.UserId,
                $"New application from {worker.FullName} for '{Job.Title}'.",
                NotificationChannel.SMS);
        }

        TempData["Success"] = "Application submitted! The employer will review it shortly.";
        return RedirectToPage("/Jobs/MyApplications");
    }

    private Task<JobPost?> LoadJobAsync(Guid id) =>
        _db.JobPosts
            .Include(j => j.Employer)
            .Include(j => j.SkillCategory)
            .FirstOrDefaultAsync(j => j.Id == id);

    public class ApplyInputModel
    {
        [Required]
        public Guid JobPostId { get; set; }

        [Display(Name = "Cover Note")]
        public string? CoverNote { get; set; }
    }
}
