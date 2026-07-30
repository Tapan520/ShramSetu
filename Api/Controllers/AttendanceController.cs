using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Api.Dtos;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize(Roles = "Employer,Admin")]
[Produces("application/json")]
public class AttendanceController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AttendanceController(ApplicationDbContext db) => _db = db;

    /// <summary>Get attendance records for a booking.</summary>
    [HttpGet("booking/{bookingId:guid}")]
    public async Task<ActionResult<IList<AttendanceDto>>> GetByBooking(Guid bookingId)
    {
        var records = await _db.AttendanceRecords
            .Include(a => a.Worker)
            .Where(a => a.BookingId == bookingId)
            .OrderBy(a => a.Date)
            .ToListAsync();

        return Ok(records.Select(ToDto));
    }

    /// <summary>Mark or update attendance for a worker on a specific date.</summary>
    [HttpPost]
    public async Task<ActionResult<AttendanceDto>> Mark([FromBody] MarkAttendanceRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (!Enum.TryParse<AttendanceStatus>(req.Status, out var status))
            return BadRequest(new { message = $"Invalid status '{req.Status}'." });

        var markedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var existing = await _db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.BookingId == req.BookingId
                && a.WorkerId == req.WorkerId
                && a.Date.Date == req.Date.Date);

        TimeSpan? checkIn  = req.CheckInTime  is not null ? TimeSpan.Parse(req.CheckInTime)  : null;
        TimeSpan? checkOut = req.CheckOutTime is not null ? TimeSpan.Parse(req.CheckOutTime) : null;
        var hours = (checkIn.HasValue && checkOut.HasValue)
            ? (decimal)(checkOut.Value - checkIn.Value).TotalHours
            : (status == AttendanceStatus.HalfDay ? 4m : status == AttendanceStatus.Present ? 8m : 0m);

        if (existing is not null)
        {
            existing.Status          = status;
            existing.CheckInTime     = checkIn;
            existing.CheckOutTime    = checkOut;
            existing.HoursWorked     = hours;
            existing.Notes           = req.Notes;
            existing.MarkedAt        = DateTime.UtcNow;
            existing.MarkedByUserId  = markedByUserId;
            await _db.SaveChangesAsync();
            await _db.Entry(existing).Reference(a => a.Worker).LoadAsync();
            return Ok(ToDto(existing));
        }

        var worker = await _db.Workers.FindAsync(req.WorkerId);
        if (worker is null) return NotFound(new { message = "Worker not found." });

        var record = new AttendanceRecord
        {
            Id             = Guid.NewGuid(),
            BookingId      = req.BookingId,
            WorkerId       = req.WorkerId,
            Date           = req.Date.Date,
            Status         = status,
            CheckInTime    = checkIn,
            CheckOutTime   = checkOut,
            HoursWorked    = hours,
            Notes          = req.Notes,
            MarkedByUserId = markedByUserId
        };

        _db.AttendanceRecords.Add(record);
        await _db.SaveChangesAsync();
        await _db.Entry(record).Reference(a => a.Worker).LoadAsync();

        return CreatedAtAction(nameof(GetByBooking), new { bookingId = req.BookingId }, ToDto(record));
    }

    private static AttendanceDto ToDto(AttendanceRecord a) => new(
        a.Id, a.WorkerId, a.Worker.FullName, a.Date,
        a.Status.ToString(),
        a.CheckInTime?.ToString(@"hh\:mm"),
        a.CheckOutTime?.ToString(@"hh\:mm"),
        a.HoursWorked, a.Notes);
}
