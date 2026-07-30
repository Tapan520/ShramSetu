using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/employer-reviews")]
[Produces("application/json")]
public class EmployerReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public EmployerReviewsController(ApplicationDbContext db) => _db = db;

    /// <summary>Get all reviews for an employer.</summary>
    [HttpGet("employer/{employerId:guid}")]
    public async Task<IActionResult> GetForEmployer(Guid employerId)
    {
        var reviews = await _db.EmployerReviews
            .Where(r => r.EmployerId == employerId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new { r.Id, r.Rating, r.Comment, r.CreatedAt,
                WorkerName = r.Worker.FullName })
            .ToListAsync();

        var avg = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
        return Ok(new { AverageRating = avg, ReviewCount = reviews.Count, Reviews = reviews });
    }

    /// <summary>Worker submits a review for an employer after a completed booking.</summary>
    [HttpPost]
    [Authorize(Roles = "Worker")]
    public async Task<IActionResult> Submit([FromBody] SubmitEmployerReviewRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return NotFound(new { message = "Worker profile not found." });

        var booking = await _db.Bookings
            .FirstOrDefaultAsync(b => b.Id == req.BookingId && b.WorkerId == worker.Id);
        if (booking is null) return NotFound(new { message = "Booking not found." });

        if (booking.Status != Core.Enums.BookingStatus.Completed)
            return BadRequest(new { message = "Can only review a completed booking." });

        var duplicate = await _db.EmployerReviews
            .AnyAsync(r => r.BookingId == req.BookingId && r.WorkerId == worker.Id);
        if (duplicate) return Conflict(new { message = "Review already submitted." });

        _db.EmployerReviews.Add(new EmployerReview
        {
            Id         = Guid.NewGuid(),
            EmployerId = booking.EmployerId,
            WorkerId   = worker.Id,
            BookingId  = req.BookingId,
            Rating     = req.Rating,
            Comment    = req.Comment
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Review submitted successfully." });
    }

    public record SubmitEmployerReviewRequest(
        [System.ComponentModel.DataAnnotations.Required] Guid BookingId,
        [System.ComponentModel.DataAnnotations.Range(1, 5)] int Rating,
        string? Comment);
}
