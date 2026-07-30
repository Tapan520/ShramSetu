using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Pages.Jobs;

public class DetailModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public DetailModel(ApplicationDbContext db) => _db = db;

    public JobPost? Job { get; set; }
    public int ApplicationCount { get; set; }
    public bool AlreadyApplied { get; set; }
    public string? MyApplicationStatus { get; set; }

    public async Task OnGetAsync(Guid id)
    {
        Job = await _db.JobPosts
            .Include(j => j.Employer)
            .Include(j => j.SkillCategory)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (Job is null) return;

        ApplicationCount = await _db.JobApplications.CountAsync(a => a.JobPostId == id);

        if (User.IsInRole("Worker"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
            if (worker is not null)
            {
                var app = await _db.JobApplications
                    .FirstOrDefaultAsync(a => a.JobPostId == id && a.WorkerId == worker.Id);
                AlreadyApplied = app is not null;
                MyApplicationStatus = app?.Status.ToString();
            }
        }
    }
}
