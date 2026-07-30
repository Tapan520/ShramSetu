using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Pages.Account;

[Authorize(Roles = "Employer,Admin")]
public class ReviewModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public ReviewModel(ApplicationDbContext db) => _db = db;

    public Booking? Booking { get; set; }

    [BindProperty]
    public ReviewInputModel Input { get; set; } = new();

    public async Task OnGetAsync(Guid bookingId)
    {
        await LoadBookingAsync(bookingId);
        Input.BookingId = bookingId;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadBookingAsync(Input.BookingId);

        if (!ModelState.IsValid) return Page();
        if (Booking is null) return NotFound();

        // Guard: already reviewed
        if (Booking.Review is not null)
        {
            TempData["Success"] = "You have already submitted a review for this booking.";
            return RedirectToPage("/Account/Bookings");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null) return Forbid();

        var review = new Review
        {
            Id = Guid.NewGuid(),
            WorkerId = Booking.WorkerId,
            EmployerId = employer.Id,
            BookingId = Input.BookingId,
            Rating = Input.Rating,
            Comment = Input.Comment
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Thank you! Your review has been submitted.";
        return RedirectToPage("/Account/Bookings");
    }

    private async Task LoadBookingAsync(Guid bookingId)
    {
        Booking = await _db.Bookings
            .Include(b => b.Worker).ThenInclude(w => w.SkillCategory)
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
    }

    public class ReviewInputModel
    {
        [Required]
        public Guid BookingId { get; set; }

        [Required, Range(1, 5, ErrorMessage = "Please select a star rating.")]
        public int Rating { get; set; }

        [Display(Name = "Comment")]
        public string? Comment { get; set; }
    }
}
