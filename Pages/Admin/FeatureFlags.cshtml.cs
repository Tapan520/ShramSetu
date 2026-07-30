using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Admin;

[Authorize(Roles = "Admin")]
public class FeatureFlagsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public FeatureFlagsModel(ApplicationDbContext db) => _db = db;

    public IList<FeatureFlag> Flags { get; set; } = new List<FeatureFlag>();

    [BindProperty]
    public FlagInput Input { get; set; } = new();

    public async Task OnGetAsync()
        => Flags = await _db.FeatureFlags.OrderBy(f => f.Name).ToListAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var flag   = await _db.FeatureFlags.FirstOrDefaultAsync(f => f.Name == Input.Name);
        if (flag is null)
            _db.FeatureFlags.Add(new FeatureFlag { Id = Guid.NewGuid(), Name = Input.Name,
                Description = Input.Description ?? string.Empty,
                Status = Input.Enabled ? FeatureFlagStatus.Enabled : FeatureFlagStatus.Disabled,
                UpdatedByUserId = userId });
        else
        {
            flag.Description      = Input.Description ?? flag.Description;
            flag.Status           = Input.Enabled ? FeatureFlagStatus.Enabled : FeatureFlagStatus.Disabled;
            flag.UpdatedByUserId  = userId;
            flag.UpdatedAt        = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = "Flag saved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid id, bool enable)
    {
        var flag = await _db.FeatureFlags.FindAsync(id);
        if (flag is not null)
        {
            flag.Status       = enable ? FeatureFlagStatus.Enabled : FeatureFlagStatus.Disabled;
            flag.UpdatedAt    = DateTime.UtcNow;
            flag.UpdatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public class FlagInput
    {
        [Required] public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
