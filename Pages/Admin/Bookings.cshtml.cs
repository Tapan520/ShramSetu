using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
public class BookingsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public BookingsModel(ApplicationDbContext db) => _db = db;

    public IList<Booking> Bookings { get; set; } = new List<Booking>();
    public string StatusFilter { get; set; } = "Requested";

    public async Task OnGetAsync(string? statusFilter)
    {
        StatusFilter = statusFilter ?? "Requested";

        var status = Enum.TryParse<BookingStatus>(StatusFilter, out var parsed)
            ? parsed
            : BookingStatus.Requested;

        Bookings = await _db.Bookings
            .Include(b => b.Worker).ThenInclude(w => w.SkillCategory)
            .Include(b => b.Employer)
            .Where(b => b.Status == status)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync(Guid bookingId, string newStatus)
    {
        var booking = await _db.Bookings.FindAsync(bookingId);
        if (booking is null) return NotFound();

        if (Enum.TryParse<BookingStatus>(newStatus, out var status))
            booking.Status = status;

        await _db.SaveChangesAsync();
        return RedirectToPage(new { statusFilter = StatusFilter });
    }
}
