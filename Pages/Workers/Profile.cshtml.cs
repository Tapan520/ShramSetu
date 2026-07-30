using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Workers;

public class ProfileModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public ProfileModel(ApplicationDbContext db) => _db = db;

    public Worker? Worker { get; set; }
    public IList<Review> Reviews { get; set; } = new List<Review>();
    public double AverageRating { get; set; }
    public int CompletedJobCount { get; set; }
    public int TotalBookingCount { get; set; }

    public async Task OnGetAsync(Guid id)
    {
        Worker = await _db.Workers
            .Include(w => w.SkillCategory)
            .Include(w => w.Documents)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (Worker is not null)
        {
            Reviews = await _db.Reviews
                .Where(r => r.WorkerId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            AverageRating = Reviews.Any() ? Reviews.Average(r => r.Rating) : 0;

            CompletedJobCount = await _db.Bookings
                .CountAsync(b => b.WorkerId == id && b.Status == BookingStatus.Completed);

            TotalBookingCount = await _db.Bookings
                .CountAsync(b => b.WorkerId == id);
        }
    }
}
