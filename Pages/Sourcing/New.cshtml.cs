using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Pages.Sourcing;

public class NewModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public NewModel(ApplicationDbContext db) => _db = db;

    [BindProperty]
    public SourcingInputModel Input { get; set; } = new();

    public IEnumerable<SelectListItem> SkillCategoryOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task OnGetAsync()
    {
        await LoadCategoriesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadCategoriesAsync();

        if (!ModelState.IsValid)
            return Page();

        // Create a guest employer record if the user is not logged in
        Guid employerId;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employer is null)
            {
                employer = new EmployerAccount
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Name = User.Identity.Name ?? "Unknown",
                    Phone = Input.ContactPhone
                };
                _db.EmployerAccounts.Add(employer);
            }
            employerId = employer.Id;
        }
        else
        {
            // Anonymous sourcing request  create a placeholder employer
            var employer = new EmployerAccount
            {
                Id = Guid.NewGuid(),
                UserId = "guest",
                Name = "Guest",
                Phone = Input.ContactPhone
            };
            _db.EmployerAccounts.Add(employer);
            employerId = employer.Id;
        }

        var request = new SourcingRequest
        {
            Id = Guid.NewGuid(),
            EmployerId = employerId,
            SkillCategoryId = Input.SkillCategoryId,
            WorkerCount = Input.WorkerCount,
            DurationDays = Input.DurationDays,
            BudgetPerDay = Input.BudgetPerDay,
            LocationCity = Input.LocationCity,
            LocationState = Input.LocationState,
            Description = Input.Description
        };

        _db.SourcingRequests.Add(request);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Your request has been submitted! We'll contact you on the provided number within 24 hours.";
        return RedirectToPage("/Index");
    }

    private async Task LoadCategoriesAsync()
    {
        SkillCategoryOptions = await _db.SkillCategories
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToListAsync();
    }

    public class SourcingInputModel
    {
        [Required]
        public Guid SkillCategoryId { get; set; }

        [Required, Range(1, 500), Display(Name = "Number of Workers")]
        public int WorkerCount { get; set; } = 1;

        [Required, Range(1, 3650), Display(Name = "Duration (days)")]
        public int DurationDays { get; set; } = 1;

        [Required, Range(0, 100000), Display(Name = "Budget per Day (?)")]
        public decimal BudgetPerDay { get; set; }

        [Display(Name = "City")]
        public string? LocationCity { get; set; }

        [Display(Name = "State")]
        public string? LocationState { get; set; }

        [Required, MinLength(20), Display(Name = "Description of Work")]
        public string Description { get; set; } = string.Empty;

        [Required, Phone, Display(Name = "Contact Phone Number")]
        public string ContactPhone { get; set; } = string.Empty;
    }
}
