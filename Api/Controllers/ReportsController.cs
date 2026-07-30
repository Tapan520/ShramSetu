using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ReportsController(ApplicationDbContext db) => _db = db;

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Submit([FromBody] SubmitReportRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (!Enum.TryParse<ReportType>(req.Type, out var type))
            return BadRequest(new { message = $"Invalid report type '{req.Type}'." });

        _db.UserReports.Add(new UserReport
        {
            Id                = Guid.NewGuid(),
            ReportedByUserId  = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            ReportedUserId    = req.ReportedUserId,
            JobPostId         = req.JobPostId,
            Type              = type,
            Details           = req.Details
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Report submitted. Our team will review it." });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] string status = "Pending")
    {
        var s = Enum.TryParse<ReportStatus>(status, out var ps) ? ps : ReportStatus.Pending;
        var reports = await _db.UserReports
            .Where(r => r.Status == s)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return Ok(reports);
    }

    [HttpPost("{id:guid}/action")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TakeAction(Guid id, [FromQuery] string status, [FromQuery] string? notes)
    {
        var report = await _db.UserReports.FindAsync(id);
        if (report is null) return NotFound();
        if (Enum.TryParse<ReportStatus>(status, out var s)) report.Status = s;
        report.AdminNotes = notes;
        report.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    public record SubmitReportRequest(
        [System.ComponentModel.DataAnnotations.Required] string ReportedUserId,
        [System.ComponentModel.DataAnnotations.Required] string Type,
        [System.ComponentModel.DataAnnotations.Required] string Details,
        Guid? JobPostId);
}
