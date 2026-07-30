using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Disputes;

[Authorize]
public class DetailModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public DetailModel(ApplicationDbContext db) => _db = db;

    public Dispute? Dispute { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        Dispute = await _db.Disputes
            .Include(d => d.Evidence)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (Dispute is null) return NotFound();

        // Only parties involved or admin can view
        if (Dispute.RaisedByUserId != userId
            && Dispute.AgainstUserId != userId
            && !User.IsInRole("Admin"))
            return Forbid();

        return Page();
    }

    public async Task<IActionResult> OnPostResolveAsync(Guid id, string resolution)
    {
        if (!User.IsInRole("Admin")) return Forbid();

        var dispute = await _db.Disputes.FindAsync(id);
        if (dispute is null) return NotFound();

        dispute.Status     = DisputeStatus.Resolved;
        dispute.Resolution = resolution;
        dispute.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Dispute marked as resolved.";
        return RedirectToPage(new { id });
    }
}
