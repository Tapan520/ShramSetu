using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Pages.Workers;

[Authorize(Roles = "Worker")]
public class OnboardingModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public OnboardingModel(ApplicationDbContext db) => _db = db;

    public int CurrentStep { get; set; } = 1;
    public List<SkillCategory> SkillCategories { get; set; } = [];

    [BindProperty]
    public StepInput Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        SkillCategories = await _db.SkillCategories.OrderBy(s => s.Name).ToListAsync();
        CurrentStep     = await ComputeStepAsync();
    }

    public async Task<IActionResult> OnPostPhotoAsync()
    {
        var worker = await GetWorkerAsync();
        if (worker is null) return NotFound();
        // In production: save file to blob storage and set worker.PhotoUrl
        worker.PhotoUrl = "/images/placeholder-worker.png";
        await _db.SaveChangesAsync();
        return RedirectToPage(new { step = 2 });
    }

    public async Task<IActionResult> OnPostSkillsAsync()
    {
        var worker = await GetWorkerAsync();
        if (worker is null) return NotFound();
        worker.SkillCategoryId    = Input.SkillCategoryId;
        worker.YearsOfExperience  = Input.YearsOfExperience;
        worker.ExpectedDailyWage  = Input.ExpectedDailyWage;
        await _db.SaveChangesAsync();
        return RedirectToPage(new { step = 3 });
    }

    public async Task<IActionResult> OnPostLocationAsync()
    {
        var worker = await GetWorkerAsync();
        if (worker is null) return NotFound();
        worker.LocationCity  = Input.LocationCity;
        worker.LocationState = Input.LocationState;
        await _db.SaveChangesAsync();
        return RedirectToPage(new { step = 4 });
    }

    public IActionResult OnGetSkipDocs() => RedirectToPage(new { step = 5 });

    public async Task<IActionResult> OnPostCompleteAsync()
    {
        var worker = await GetWorkerAsync();
        if (worker is null) return NotFound();

        var ob = await _db.WorkerOnboardings.FirstOrDefaultAsync(o => o.WorkerId == worker.Id)
              ?? _db.WorkerOnboardings.Add(new WorkerOnboarding { Id = Guid.NewGuid(), WorkerId = worker.Id }).Entity;

        ob.PhotoDone    = !string.IsNullOrEmpty(worker.PhotoUrl);
        ob.SkillsDone   = worker.SkillCategoryId != Guid.Empty;
        ob.LocationDone = !string.IsNullOrEmpty(worker.LocationCity);
        ob.DocumentsDone = await _db.WorkerDocuments.AnyAsync(d => d.WorkerId == worker.Id);
        ob.BankDone     = await _db.WorkerWallets.AnyAsync(w => w.WorkerId == worker.Id);
        if (ob.IsCompleted) ob.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Profile setup complete! Employers can now find you.";
        return RedirectToPage("/Workers/MyProfile");
    }

    private async Task<Worker?> GetWorkerAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
    }

    private async Task<int> ComputeStepAsync()
    {
        var worker = await GetWorkerAsync();
        if (worker is null) return 1;
        if (string.IsNullOrEmpty(worker.PhotoUrl)) return 1;
        if (worker.SkillCategoryId == Guid.Empty) return 2;
        if (string.IsNullOrEmpty(worker.LocationCity)) return 3;
        if (!await _db.WorkerDocuments.AnyAsync(d => d.WorkerId == worker.Id)) return 4;
        return 5;
    }

    public class StepInput
    {
        public Guid SkillCategoryId { get; set; }
        public int YearsOfExperience { get; set; }
        public decimal ExpectedDailyWage { get; set; }
        public string? LocationCity { get; set; }
        public string? LocationState { get; set; }
    }
}
