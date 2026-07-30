using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Workers;

[Authorize(Roles = "Worker")]
public class MyProfileModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public MyProfileModel(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public Worker? Worker { get; set; }

    [BindProperty]
    public ProfileInputModel Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadWorkerAsync();
        if (Worker is not null)
            Input = ProfileInputModel.FromWorker(Worker);
    }

    public async Task<IActionResult> OnPostSaveProfileAsync()
    {
        await LoadWorkerAsync();
        if (Worker is null) return NotFound();

        if (!ModelState.IsValid) return Page();

        Worker.FullName = Input.FullName;
        Worker.Phone = Input.Phone;
        Worker.SubSkills = Input.SubSkills;
        Worker.YearsOfExperience = Input.YearsOfExperience;
        Worker.ExpectedDailyWage = Input.ExpectedDailyWage;
        Worker.ExpectedMonthlyWage = Input.ExpectedMonthlyWage;
        Worker.LocationCity = Input.LocationCity;
        Worker.LocationState = Input.LocationState;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Profile updated successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAvailabilityAsync()
    {
        await LoadWorkerAsync();
        if (Worker is null) return NotFound();

        Worker.IsAvailable = !Worker.IsAvailable;
        await _db.SaveChangesAsync();
        TempData["Success"] = Worker.IsAvailable ? "You are now marked as Available." : "You are now marked as Unavailable.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUploadDocumentAsync(string docType, IFormFile docFile)
    {
        await LoadWorkerAsync();
        if (Worker is null) return NotFound();

        if (docFile is { Length: > 0 })
        {
            var uploads = Path.Combine(_env.WebRootPath, "uploads", "documents");
            Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(docFile.FileName)}";
            using var stream = System.IO.File.Create(Path.Combine(uploads, fileName));
            await docFile.CopyToAsync(stream);

            _db.WorkerDocuments.Add(new WorkerDocument
            {
                Id = Guid.NewGuid(),
                WorkerId = Worker.Id,
                Type = Enum.Parse<DocumentType>(docType),
                FileUrl = $"/uploads/documents/{fileName}"
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Document uploaded. Awaiting admin verification.";
        }

        return RedirectToPage();
    }

    private async Task LoadWorkerAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Worker = await _db.Workers
            .Include(w => w.SkillCategory)
            .Include(w => w.Documents)
            .FirstOrDefaultAsync(w => w.UserId == userId);
    }

    public class ProfileInputModel
    {
        [Required, Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, Phone, Display(Name = "Mobile Number")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Sub-skills")]
        public string? SubSkills { get; set; }

        [Required, Range(0, 60), Display(Name = "Years of Experience")]
        public int YearsOfExperience { get; set; }

        [Required, Range(0, 100000), Display(Name = "Expected Daily Wage (?)")]
        public decimal ExpectedDailyWage { get; set; }

        [Range(0, 3000000), Display(Name = "Expected Monthly Wage (?)")]
        public decimal ExpectedMonthlyWage { get; set; }

        [Display(Name = "City")]
        public string? LocationCity { get; set; }

        [Display(Name = "State")]
        public string? LocationState { get; set; }

        public static ProfileInputModel FromWorker(Worker w) => new()
        {
            FullName = w.FullName,
            Phone = w.Phone,
            SubSkills = w.SubSkills,
            YearsOfExperience = w.YearsOfExperience,
            ExpectedDailyWage = w.ExpectedDailyWage,
            ExpectedMonthlyWage = w.ExpectedMonthlyWage,
            LocationCity = w.LocationCity,
            LocationState = w.LocationState
        };
    }
}
