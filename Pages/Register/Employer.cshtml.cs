using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Register;

public class EmployerModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public EmployerModel(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public EmployerInputModel Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = new IdentityUser
        {
            UserName = Input.Email,
            Email = Input.Email,
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

        await _userManager.AddToRoleAsync(user, "Employer");

        var employer = new EmployerAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = Input.Name,
            Type = Enum.Parse<EmployerType>(Input.Type),
            CompanyName = Input.CompanyName,
            Phone = Input.Phone,
            Email = Input.Email
        };

        _db.EmployerAccounts.Add(employer);
        await _db.SaveChangesAsync();

        await _signInManager.SignInAsync(user, isPersistent: false);

        TempData["Success"] = "Welcome to ShramSetu! You can now search and hire workers.";
        return RedirectToPage("/Workers/Index");
    }

    public class EmployerInputModel
    {
        [Required, Display(Name = "Full Name / Contact Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = "Individual";

        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Required, Phone, Display(Name = "Mobile Number")]
        public string Phone { get; set; } = string.Empty;

        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(8), Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
