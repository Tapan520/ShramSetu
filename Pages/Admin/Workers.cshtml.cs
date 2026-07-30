using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Admin;

[Authorize(Roles = "Admin")]
public class WorkersModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public WorkersModel(ApplicationDbContext db) => _db = db;

    public IList<Worker> Workers { get; set; } = new List<Worker>();
    public string StatusFilter { get; set; } = "Pending";

    public async Task OnGetAsync(string? statusFilter)
    {
        StatusFilter = statusFilter ?? "Pending";

        var status = Enum.TryParse<VerificationStatus>(StatusFilter, out var parsed)
            ? parsed
            : VerificationStatus.Pending;

        Workers = await _db.Workers
            .Include(w => w.SkillCategory)
            .Include(w => w.Documents)
            .Where(w => w.KycStatus == status)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync(Guid workerId, string action)
    {
        var worker = await _db.Workers.FindAsync(workerId);
        if (worker is null)
            return NotFound();

        worker.KycStatus = action switch
        {
            "Verify" => VerificationStatus.Verified,
            "Reject" => VerificationStatus.Rejected,
            "UnderReview" => VerificationStatus.UnderReview,
            _ => worker.KycStatus
        };

        if (action == "Verify")
        {
            foreach (var doc in _db.WorkerDocuments.Where(d => d.WorkerId == workerId))
                doc.IsVerified = true;
        }

        await _db.SaveChangesAsync();
        return RedirectToPage(new { statusFilter = StatusFilter });
    }
}
