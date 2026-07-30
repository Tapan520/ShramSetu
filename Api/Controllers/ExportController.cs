using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Data;
using ShramSetu.Services;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/export")]
[Authorize(Roles = "Admin,Employer")]
[Produces("application/json")]
public class ExportController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPdfService _pdf;

    public ExportController(ApplicationDbContext db, IPdfService pdf)
    {
        _db  = db;
        _pdf = pdf;
    }

    /// <summary>Download a payslip PDF for a payroll record.</summary>
    [HttpGet("payslip/{payrollId:guid}")]
    public async Task<IActionResult> DownloadPayslip(Guid payrollId)
    {
        var record = await _db.PayrollRecords
            .Include(r => r.Worker)
            .Include(r => r.Employer)
            .FirstOrDefaultAsync(r => r.Id == payrollId);

        if (record is null) return NotFound();

        var bytes = _pdf.GeneratePayslip(record);
        return File(bytes, "application/pdf",
            $"payslip_{record.Worker.FullName.Replace(" ", "_")}_{record.PeriodStart:yyyyMM}.pdf");
    }

    /// <summary>Export workers list as CSV.</summary>
    [HttpGet("workers/csv")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportWorkersCsv()
    {
        var workers = await _db.Workers
            .Include(w => w.SkillCategory)
            .Where(w => !w.IsDeleted)
            .OrderBy(w => w.FullName)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Id,FullName,Phone,Skill,City,State,Experience,DailyWage,KycStatus,IsAvailable,Joined");
        foreach (var w in workers)
            csv.AppendLine($"{w.Id},{w.FullName},{w.Phone},{w.SkillCategory.Name}," +
                           $"{w.LocationCity},{w.LocationState},{w.YearsOfExperience}," +
                           $"{w.ExpectedDailyWage},{w.KycStatus},{w.IsAvailable}," +
                           $"{w.CreatedAt:yyyy-MM-dd}");

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "workers.csv");
    }

    /// <summary>Export bookings list as CSV.</summary>
    [HttpGet("bookings/csv")]
    public async Task<IActionResult> ExportBookingsCsv([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var query = _db.Bookings
            .Include(b => b.Worker)
            .Include(b => b.Employer)
            .AsQueryable();

        if (!User.IsInRole("Admin"))
        {
            var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employer is null) return Forbid();
            query = query.Where(b => b.EmployerId == employer.Id);
        }

        if (from.HasValue) query = query.Where(b => b.CreatedAt >= from.Value);
        if (to.HasValue)   query = query.Where(b => b.CreatedAt <= to.Value);

        var bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Id,Worker,Employer,StartDate,Duration,Wage,Type,Status,Created");
        foreach (var b in bookings)
            csv.AppendLine($"{b.Id},{b.Worker.FullName},{b.Employer.Name}," +
                           $"{b.StartDate:yyyy-MM-dd},{b.DurationDays},{b.AgreedWage}," +
                           $"{b.Type},{b.Status},{b.CreatedAt:yyyy-MM-dd}");

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "bookings.csv");
    }

    /// <summary>Export attendance records as CSV.</summary>
    [HttpGet("attendance/csv")]
    public async Task<IActionResult> ExportAttendanceCsv([FromQuery] Guid bookingId)
    {
        var records = await _db.AttendanceRecords
            .Include(a => a.Worker)
            .Where(a => a.BookingId == bookingId)
            .OrderBy(a => a.Date)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Date,Worker,Status,CheckIn,CheckOut,HoursWorked,Notes");
        foreach (var a in records)
            csv.AppendLine($"{a.Date:yyyy-MM-dd},{a.Worker.FullName},{a.Status}," +
                           $"{a.CheckInTime},{a.CheckOutTime},{a.HoursWorked},{a.Notes}");

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"attendance_{bookingId}.csv");
    }
}
