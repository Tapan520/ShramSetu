using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Admin;

[Authorize(Roles = "Admin")]
public class SourcingModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public SourcingModel(ApplicationDbContext db) => _db = db;

    public IList<SourcingRequest> Requests { get; set; } = new List<SourcingRequest>();
    public string StatusFilter { get; set; } = "Open";

    public async Task OnGetAsync(string? statusFilter)
    {
        StatusFilter = statusFilter ?? "Open";

        var status = Enum.TryParse<SourcingStatus>(StatusFilter, out var parsed)
            ? parsed
            : SourcingStatus.Open;

        Requests = await _db.SourcingRequests
            .Include(r => r.SkillCategory)
            .Include(r => r.Employer)
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync(Guid requestId, string action, string? adminNotes)
    {
        var request = await _db.SourcingRequests.FindAsync(requestId);
        if (request is null)
            return NotFound();

        request.Status = action switch
        {
            "InProgress" => SourcingStatus.InProgress,
            "Fulfilled" => SourcingStatus.Fulfilled,
            "Cancelled" => SourcingStatus.Cancelled,
            _ => request.Status
        };

        if (!string.IsNullOrWhiteSpace(adminNotes))
            request.AdminNotes = adminNotes;

        if (request.Status == SourcingStatus.Fulfilled)
            request.FulfilledAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToPage(new { statusFilter = StatusFilter });
    }
}
