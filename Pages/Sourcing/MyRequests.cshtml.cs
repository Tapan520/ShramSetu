using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Pages.Sourcing;

[Authorize(Roles = "Employer,Admin")]
public class MyRequestsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public MyRequestsModel(ApplicationDbContext db) => _db = db;

    public IList<SourcingRequest> Requests { get; set; } = new List<SourcingRequest>();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);

        if (employer is not null)
        {
            Requests = await _db.SourcingRequests
                .Include(r => r.SkillCategory)
                .Where(r => r.EmployerId == employer.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
