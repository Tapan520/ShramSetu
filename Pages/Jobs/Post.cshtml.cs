using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Pages.Jobs;

[Authorize(Roles = "Employer,Admin")]
public class PostModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public PostModel(ApplicationDbContext db) => _db = db;

    [BindProperty]
    public JobInputModel Input { get; set; } = new();

    public IEnumerable<SelectListItem> SkillCategoryOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task OnGetAsync()
    {
        await LoadCategoriesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadCategoriesAsync();
        if (!ModelState.IsValid) return Page();

        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null)
        {
            ModelState.AddModelError(string.Empty, "Employer profile not found.");
            return Page();
        }

        _db.JobPosts.Add(new JobPost
        {
            Id              = Guid.NewGuid(),
            EmployerId      = employer.Id,
            SkillCategoryId = Input.SkillCategoryId,
            Title           = Input.Title,
            Description     = Input.Description,
            LocationCity    = Input.LocationCity,
            LocationState   = Input.LocationState,
            DailyWage       = Input.DailyWage,
            DurationDays    = Input.DurationDays,
            StartDate       = Input.StartDate,
            VacancyCount    = Input.VacancyCount
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = "Job posted successfully!";
        return RedirectToPage("/Jobs/Index");
    }

    private async Task LoadCategoriesAsync()
    {
        SkillCategoryOptions = await _db.SkillCategories
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToListAsync();
    }

    public class JobInputModel
    {
        [Required, MinLength(5), Display(Name = "Job Title")]
        public string Title { get; set; } = string.Empty;

        [Required, MinLength(20), Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required, Display(Name = "Skill Category")]
        public Guid SkillCategoryId { get; set; }

        [Display(Name = "City")]
        public string? LocationCity { get; set; }

        [Display(Name = "State")]
        public string? LocationState { get; set; }

        [Required, Range(1, 100000), Display(Name = "Daily Wage (?)")]
        public decimal DailyWage { get; set; }

        [Required, Range(1, 3650), Display(Name = "Duration (days)")]
        public int DurationDays { get; set; } = 1;

        [Required, Display(Name = "Start Date"), DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(3);

        [Range(1, 500), Display(Name = "Number of Vacancies")]
        public int VacancyCount { get; set; } = 1;
    }
}
