using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Data;
using System.ComponentModel.DataAnnotations;

namespace ShramSetu.Pages.SuperAdmin;

[Authorize(Roles = "SuperAdmin,Admin")]
public class UsersModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _db;

    public UsersModel(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    public List<UserRowDto> Users { get; set; } = [];
    public List<string> AllRoles { get; set; } = [];

    [BindProperty] public CreateUserInput Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid) { await LoadAsync(); return Page(); }

        var existing = await _userManager.FindByEmailAsync(Input.Email);
        if (existing is not null)
        {
            ModelState.AddModelError("Input.Email", "Email already registered.");
            await LoadAsync();
            return Page();
        }

        var user = new IdentityUser { UserName = Input.Email, Email = Input.Email, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            await LoadAsync();
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(Input.Role))
            await _userManager.AddToRoleAsync(user, Input.Role);

        TempData["Success"] = $"User '{Input.Email}' created successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        // Prevent deleting self
        if (user.Email == User.Identity!.Name)
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToPage();
        }

        await _userManager.DeleteAsync(user);
        TempData["Success"] = $"User '{user.Email}' deleted.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostChangeRoleAsync(string userId, string newRole)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        if (!string.IsNullOrWhiteSpace(newRole))
            await _userManager.AddToRoleAsync(user, newRole);

        TempData["Success"] = $"Role updated for '{user.Email}'.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdatePhoneAsync(string userId, string newPhone)
    {
        if (string.IsNullOrWhiteSpace(newPhone))
        {
            TempData["Error"] = "Phone number cannot be empty.";
            return RedirectToPage();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        // Update IdentityUser phone
        user.PhoneNumber = newPhone;
        await _userManager.UpdateAsync(user);

        // Update Worker profile if exists
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is not null)
        {
            worker.Phone = newPhone;
        }

        // Update Employer profile if exists
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is not null)
        {
            employer.Phone = newPhone;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Phone number updated for '{user.Email}'.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(string userId, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var token  = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? $"Password reset for '{user.Email}'."
            : string.Join(", ", result.Errors.Select(e => e.Description));

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        AllRoles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();

        var workers   = await _db.Workers.ToDictionaryAsync(w => w.UserId ?? "", w => w);
        var employers = await _db.EmployerAccounts.ToDictionaryAsync(e => e.UserId, e => e);

        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        Users = [];
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);

            string name  = "—";
            string phone = u.PhoneNumber ?? "—";

            if (workers.TryGetValue(u.Id, out var w))
            {
                name  = w.FullName;
                phone = w.Phone;
            }
            else if (employers.TryGetValue(u.Id, out var e))
            {
                name  = e.Name;
                phone = e.Phone;
            }

            Users.Add(new UserRowDto
            {
                Id         = u.Id,
                Email      = u.Email ?? "",
                Name       = name,
                Phone      = phone,
                Roles      = roles.ToList(),
                LockoutEnd = u.LockoutEnd
            });
        }
    }

    public class UserRowDto
    {
        public string Id        { get; set; } = "";
        public string Email     { get; set; } = "";
        public string Name      { get; set; } = "—";
        public string Phone     { get; set; } = "—";
        public List<string> Roles { get; set; } = [];
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool IsLocked => LockoutEnd.HasValue && LockoutEnd > DateTimeOffset.UtcNow;
        public string PrimaryRole => Roles.FirstOrDefault() ?? "—";
    }

    public class CreateUserInput
    {
        [Required, EmailAddress]
        public string Email    { get; set; } = "";
        [Required, MinLength(8)]
        public string Password { get; set; } = "";
        public string? Role    { get; set; }
    }
}
