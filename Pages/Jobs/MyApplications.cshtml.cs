using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Jobs;

[Authorize(Roles = "Worker")]
public class MyApplicationsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public MyApplicationsModel(ApplicationDbContext db) => _db = db;

    public IList<JobApplication> Applications { get; set; } = new List<JobApplication>();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return;

        Applications = await _db.JobApplications
            .Include(a => a.JobPost).ThenInclude(j => j.Employer)
            .Include(a => a.JobPost).ThenInclude(j => j.SkillCategory)
            .Where(a => a.WorkerId == worker.Id)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync(Guid applicationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        var app    = await _db.JobApplications.FirstOrDefaultAsync(a => a.Id == applicationId);

        if (app is null || app.WorkerId != worker?.Id) return Forbid();
        if (app.Status == JobApplicationStatus.Accepted)
        {
            TempData["Error"] = "Cannot withdraw an accepted application.";
            return RedirectToPage();
        }

        app.Status = JobApplicationStatus.Withdrawn;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Application withdrawn.";
        return RedirectToPage();
    }
}
