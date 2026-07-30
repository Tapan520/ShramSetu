using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;
using System.Security.Claims;

namespace ShramSetu.Pages.Account;

[Authorize]
public class BookingsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public BookingsModel(ApplicationDbContext db) => _db = db;

    public IList<Booking> Bookings { get; set; } = new List<Booking>();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null)
        {
            Bookings = new List<Booking>();
            return;
        }

        Bookings = await _db.Bookings
            .Include(b => b.Worker).ThenInclude(w => w.SkillCategory)
            .Include(b => b.Review)
            .Where(b => b.EmployerId == employer.Id)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }
}
