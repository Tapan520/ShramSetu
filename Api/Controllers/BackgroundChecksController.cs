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
[Route("api/background-checks")]
[Authorize(Roles = "Admin,Employer")]
[Produces("application/json")]
public class BackgroundChecksController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public BackgroundChecksController(ApplicationDbContext db) => _db = db;

    /// <summary>Request a background check for a worker.</summary>
    [HttpPost]
    public async Task<ActionResult<BackgroundCheckDto>> Request([FromBody] RequestBackgroundCheckRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (!Enum.TryParse<BackgroundCheckType>(req.CheckType, out var checkType))
            return BadRequest(new { message = $"Invalid CheckType '{req.CheckType}'." });

        var worker = await _db.Workers.FindAsync(req.WorkerId);
        if (worker is null) return NotFound(new { message = "Worker not found." });

        var check = new BackgroundCheck
        {
            Id                = Guid.NewGuid(),
            WorkerId          = req.WorkerId,
            CheckType         = checkType,
            Status            = BackgroundCheckStatus.Pending,
            ProviderName      = req.ProviderName,
            RequestedAt       = DateTime.UtcNow,
            RequestedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        _db.BackgroundChecks.Add(check);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByWorker), new { workerId = req.WorkerId },
            ToDto(check, worker.FullName));
    }

    /// <summary>Get all background checks for a worker.</summary>
    [HttpGet("worker/{workerId:guid}")]
    public async Task<ActionResult<IList<BackgroundCheckDto>>> GetByWorker(Guid workerId)
    {
        var worker = await _db.Workers.FindAsync(workerId);
        if (worker is null) return NotFound();

        var checks = await _db.BackgroundChecks
            .Where(c => c.WorkerId == workerId)
            .OrderByDescending(c => c.RequestedAt)
            .ToListAsync();

        return Ok(checks.Select(c => ToDto(c, worker.FullName)));
    }

    /// <summary>Admin updates check status/result (called by webhook or manually).</summary>
    [HttpPost("{id:guid}/result")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BackgroundCheckDto>> UpdateResult(
        Guid id,
        [FromQuery] string status,
        [FromQuery] string? summary,
        [FromQuery] string? providerReference,
        [FromQuery] string? reportUrl)
    {
        var check = await _db.BackgroundChecks.Include(c => c.Worker).FirstOrDefaultAsync(c => c.Id == id);
        if (check is null) return NotFound();

        if (Enum.TryParse<BackgroundCheckStatus>(status, out var s)) check.Status = s;
        check.ResultSummary      = summary ?? check.ResultSummary;
        check.ProviderReference  = providerReference ?? check.ProviderReference;
        check.ReportUrl          = reportUrl ?? check.ReportUrl;
        if (check.Status is BackgroundCheckStatus.Passed or BackgroundCheckStatus.Failed)
            check.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToDto(check, check.Worker.FullName));
    }

    private static BackgroundCheckDto ToDto(BackgroundCheck c, string workerName) => new(
        c.Id, c.WorkerId, workerName, c.CheckType.ToString(), c.Status.ToString(),
        c.ProviderReference, c.ProviderName, c.ResultSummary, c.ReportUrl,
        c.RequestedAt, c.CompletedAt);
}
