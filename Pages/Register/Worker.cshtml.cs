using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Register;

public class WorkerModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public WorkerModel(
        ApplicationDbContext db,
        IWebHostEnvironment env,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager)
    {
        _db = db;
        _env = env;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public WorkerInputModel Input { get; set; } = new();

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

        // Create Identity account
        var user = new IdentityUser
        {
            UserName = Input.Phone + "@worker.shramsetu.in",
            Email = Input.Phone + "@worker.shramsetu.in",
            PhoneNumber = Input.Phone,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        await _userManager.AddToRoleAsync(user, "Worker");

        string? photoUrl = null;
        if (Input.Photo is { Length: > 0 })
            photoUrl = await SaveFileAsync(Input.Photo, "photos");

        string? aadhaarDocUrl = null;
        if (Input.AadhaarDocument is { Length: > 0 })
            aadhaarDocUrl = await SaveFileAsync(Input.AadhaarDocument, "documents");

        var worker = new Worker
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FullName = Input.FullName,
            Phone = Input.Phone,
            SkillCategoryId = Input.SkillCategoryId,
            SubSkills = Input.SubSkills,
            YearsOfExperience = Input.YearsOfExperience,
            ExpectedDailyWage = Input.ExpectedDailyWage,
            ExpectedMonthlyWage = Input.ExpectedMonthlyWage,
            LocationCity = Input.LocationCity,
            LocationState = Input.LocationState,
            PhotoUrl = photoUrl,
            KycStatus = VerificationStatus.Pending
        };

        _db.Workers.Add(worker);

        if (aadhaarDocUrl is not null)
        {
            _db.WorkerDocuments.Add(new WorkerDocument
            {
                Id = Guid.NewGuid(),
                WorkerId = worker.Id,
                Type = DocumentType.Aadhaar,
                FileUrl = aadhaarDocUrl
            });
        }

        await _db.SaveChangesAsync();
        await _signInManager.SignInAsync(user, isPersistent: false);

        TempData["Success"] = "Registration submitted! Our team will verify your documents shortly.";
        return RedirectToPage("/Workers/MyProfile");
    }

    private async Task<string> SaveFileAsync(IFormFile file, string folder)
    {
        var uploads = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(uploads);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploads, fileName);
        using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream);
        return $"/uploads/{folder}/{fileName}";
    }

    private async Task LoadCategoriesAsync()
    {
        SkillCategoryOptions = await _db.SkillCategories
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToListAsync();
    }

    public class WorkerInputModel
    {
        [Required, Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, Phone, Display(Name = "Mobile Number")]
        public string Phone { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(8), Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public Guid SkillCategoryId { get; set; }

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

        [Display(Name = "Profile Photo")]
        public IFormFile? Photo { get; set; }

        [Display(Name = "Aadhaar Card")]
        public IFormFile? AadhaarDocument { get; set; }
    }
}
