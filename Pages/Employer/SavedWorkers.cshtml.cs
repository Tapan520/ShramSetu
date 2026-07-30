using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Pages.Employer;

[Authorize(Roles = "Employer,Admin")]
public class SavedWorkersModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public SavedWorkersModel(ApplicationDbContext db) => _db = db;

    public IList<SavedWorker> Saved { get; set; } = new List<SavedWorker>();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync(Guid workerId)
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        var saved    = await _db.SavedWorkers
            .FirstOrDefaultAsync(s => s.EmployerId == employer!.Id && s.WorkerId == workerId);

        if (saved is not null)
        {
            _db.SavedWorkers.Remove(saved);
            await _db.SaveChangesAsync();
        }

        TempData["Success"] = "Worker removed from saved list.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null) return;

        Saved = await _db.SavedWorkers
            .Include(s => s.Worker).ThenInclude(w => w.SkillCategory)
            .Where(s => s.EmployerId == employer.Id)
            .OrderByDescending(s => s.SavedAt)
            .ToListAsync();
    }
}
