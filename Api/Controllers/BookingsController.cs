using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Api.Dtos;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;
using ShramSetu.Services;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
[Produces("application/json")]
public class BookingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notify;

    public BookingsController(ApplicationDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    /// <summary>Get all bookings for the authenticated employer or worker.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<BookingDto>>> GetMyBookings(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page     = Math.Max(1, page);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        IQueryable<Booking> query;

        if (User.IsInRole("Worker"))
        {
            var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
            if (worker is null) return Ok(new PagedResult<BookingDto>([], page, pageSize, 0));

            query = _db.Bookings
                .Include(b => b.Worker).ThenInclude(w => w.SkillCategory)
                .Include(b => b.Employer)
                .Include(b => b.Review)
                .Where(b => b.WorkerId == worker.Id);
        }
        else
        {
            var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employer is null) return Ok(new PagedResult<BookingDto>([], page, pageSize, 0));

            query = _db.Bookings
                .Include(b => b.Worker).ThenInclude(w => w.SkillCategory)
                .Include(b => b.Employer)
                .Include(b => b.Review)
                .Where(b => b.EmployerId == employer.Id);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookingStatus>(status, out var bs))
            query = query.Where(b => b.Status == bs);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<BookingDto>(items.Select(ToDto).ToList(), page, pageSize, total));
    }

    /// <summary>Get a single booking by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDto>> GetBooking(Guid id)
    {
        var b = await _db.Bookings
            .Include(b => b.Worker).ThenInclude(w => w.SkillCategory)
            .Include(b => b.Employer)
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (b is null) return NotFound();
        return Ok(ToDto(b));
    }

    /// <summary>Create a new direct-contact booking (Employer only).</summary>
    [HttpPost]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<BookingDto>> CreateBooking([FromBody] CreateBookingRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null)
            return BadRequest(new { message = "No employer profile found. Please complete registration." });

        var booking = new Booking
        {
            Id         = Guid.NewGuid(),
            WorkerId   = req.WorkerId,
            EmployerId = employer.Id,
            Type       = BookingType.DirectContact,
            Status     = BookingStatus.Requested,
            StartDate  = req.StartDate,
            DurationDays = req.DurationDays,
            AgreedWage = req.AgreedWage,
            Notes      = req.Notes
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        // Notify worker
        var worker = await _db.Workers.FindAsync(req.WorkerId);
        if (worker?.UserId is not null)
        {
            await _notify.SendAsync(
                worker.UserId,
                $"New booking from {employer.Name} starting {req.StartDate:dd MMM yyyy} for {req.DurationDays} day(s).",
                NotificationChannel.SMS);
        }

        var created = await _db.Bookings
            .Include(b => b.Worker).ThenInclude(w => w.SkillCategory)
            .Include(b => b.Employer)
            .Include(b => b.Review)
            .FirstAsync(b => b.Id == booking.Id);

        return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, ToDto(created));
    }

    /// <summary>Submit a review for a completed booking (Employer only).</summary>
    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<ReviewDto>> AddReview(Guid id, [FromBody] CreateReviewRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var booking = await _db.Bookings.Include(b => b.Review).FirstOrDefaultAsync(b => b.Id == id);
        if (booking is null) return NotFound();
        if (booking.Review is not null) return Conflict(new { message = "Review already submitted." });
        if (booking.Status != BookingStatus.Completed)
            return BadRequest(new { message = "Can only review a completed booking." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null) return Forbid();

        var review = new Review
        {
            Id = Guid.NewGuid(),
            WorkerId   = booking.WorkerId,
            EmployerId = employer.Id,
            BookingId  = id,
            Rating     = req.Rating,
            Comment    = req.Comment
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBooking), new { id },
            new ReviewDto(review.Id, review.Rating, review.Comment, review.CreatedAt));
    }

    private static BookingDto ToDto(Booking b) => new(
        b.Id,
        b.WorkerId,
        b.Worker.FullName,
        b.Worker.SkillCategory.Name,
        b.Worker.PhotoUrl,
        b.Employer.Name,
        b.StartDate,
        b.DurationDays,
        b.AgreedWage,
        b.Type.ToString(),
        b.Status.ToString(),
        b.Notes,
        b.CreatedAt,
        b.Review is null ? null : new ReviewDto(b.Review.Id, b.Review.Rating, b.Review.Comment, b.Review.CreatedAt));
}
