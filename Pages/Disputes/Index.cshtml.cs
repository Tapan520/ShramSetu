using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Pages.Disputes;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public IndexModel(ApplicationDbContext db) => _db = db;

    public IList<Dispute> Disputes { get; set; } = new List<Dispute>();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Disputes = await _db.Disputes
            .Where(d => d.RaisedByUserId == userId || d.AgainstUserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }
}
