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
[Route("api/jobs")]
[Produces("application/json")]
public class JobsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notify;

    public JobsController(ApplicationDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    // ?? Browse ????????????????????????????????????????????????????????????????

    /// <summary>Browse open job posts. Available to all (including unauthenticated).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<JobPostDto>>> Browse(
        [FromQuery] Guid? skillCategoryId,
        [FromQuery] string? city,
        [FromQuery] decimal? minWage,
        [FromQuery] decimal? maxWage,
        [FromQuery] string sortBy = "newest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page     = Math.Max(1, page);

        var query = _db.JobPosts
            .Include(j => j.Employer)
            .Include(j => j.SkillCategory)
            .Where(j => j.Status == JobPostStatus.Open)
            .AsQueryable();

        if (skillCategoryId.HasValue)
            query = query.Where(j => j.SkillCategoryId == skillCategoryId.Value);
        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(j => j.LocationCity != null && j.LocationCity.Contains(city));
        if (minWage.HasValue)
            query = query.Where(j => j.DailyWage >= minWage.Value);
        if (maxWage.HasValue)
            query = query.Where(j => j.DailyWage <= maxWage.Value);

        query = sortBy switch
        {
            "wage_asc"  => query.OrderBy(j => j.DailyWage),
            "wage_desc" => query.OrderByDescending(j => j.DailyWage),
            "start"     => query.OrderBy(j => j.StartDate),
            _           => query.OrderByDescending(j => j.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var jobIds = items.Select(j => j.Id).ToList();
        var appCounts = await _db.JobApplications
            .Where(a => jobIds.Contains(a.JobPostId))
            .GroupBy(a => a.JobPostId)
            .Select(g => new { JobPostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.JobPostId, x => x.Count);

        return Ok(new PagedResult<JobPostDto>(
            items.Select(j => ToDto(j, appCounts.GetValueOrDefault(j.Id))).ToList(),
            page, pageSize, total));
    }

    /// <summary>Get a single job post by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobPostDto>> GetJob(Guid id)
    {
        var job = await _db.JobPosts
            .Include(j => j.Employer)
            .Include(j => j.SkillCategory)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job is null) return NotFound();

        var appCount = await _db.JobApplications.CountAsync(a => a.JobPostId == id);
        return Ok(ToDto(job, appCount));
    }

    // ?? Employer: create / manage posts ??????????????????????????????????????

    /// <summary>Create a new job post (Employer only).</summary>
    [HttpPost]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<JobPostDto>> Create([FromBody] CreateJobPostRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null)
            return BadRequest(new { message = "Employer profile not found. Please complete registration." });

        var job = new JobPost
        {
            Id              = Guid.NewGuid(),
            EmployerId      = employer.Id,
            SkillCategoryId = req.SkillCategoryId,
            Title           = req.Title,
            Description     = req.Description,
            LocationCity    = req.LocationCity,
            LocationState   = req.LocationState,
            DailyWage       = req.DailyWage,
            DurationDays    = req.DurationDays,
            StartDate       = req.StartDate,
            VacancyCount    = req.VacancyCount
        };

        _db.JobPosts.Add(job);
        await _db.SaveChangesAsync();

        await _db.Entry(job).Reference(j => j.Employer).LoadAsync();
        await _db.Entry(job).Reference(j => j.SkillCategory).LoadAsync();

        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, ToDto(job, 0));
    }

    /// <summary>Get all posts by the authenticated employer.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<PagedResult<JobPostDto>>> GetMyPosts(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page     = Math.Max(1, page);

        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null)
            return Ok(new PagedResult<JobPostDto>([], page, pageSize, 0));

        var query = _db.JobPosts
            .Include(j => j.Employer)
            .Include(j => j.SkillCategory)
            .Where(j => j.EmployerId == employer.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<JobPostStatus>(status, out var s))
            query = query.Where(j => j.Status == s);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var jobIds   = items.Select(j => j.Id).ToList();
        var appCounts = await _db.JobApplications
            .Where(a => jobIds.Contains(a.JobPostId))
            .GroupBy(a => a.JobPostId)
            .Select(g => new { JobPostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.JobPostId, x => x.Count);

        return Ok(new PagedResult<JobPostDto>(
            items.Select(j => ToDto(j, appCounts.GetValueOrDefault(j.Id))).ToList(),
            page, pageSize, total));
    }

    /// <summary>Close a job post (Employer only).</summary>
    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<IActionResult> CloseJob(Guid id)
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        var job      = await _db.JobPosts.FirstOrDefaultAsync(j => j.Id == id);

        if (job is null) return NotFound();
        if (job.EmployerId != employer?.Id && !User.IsInRole("Admin")) return Forbid();

        job.Status   = JobPostStatus.Closed;
        job.ClosedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Get all applications for a job post (Employer only).</summary>
    [HttpGet("{id:guid}/applications")]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<PagedResult<JobApplicationDto>>> GetApplications(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page     = Math.Max(1, page);

        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        var job      = await _db.JobPosts
            .Include(j => j.SkillCategory)
            .Include(j => j.Employer)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job is null) return NotFound();
        if (job.EmployerId != employer?.Id && !User.IsInRole("Admin")) return Forbid();

        var total = await _db.JobApplications.CountAsync(a => a.JobPostId == id);
        var apps  = await _db.JobApplications
            .Include(a => a.Worker)
            .Where(a => a.JobPostId == id)
            .OrderByDescending(a => a.AppliedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<JobApplicationDto>(
            apps.Select(a => ToApplicationDto(a, job)).ToList(),
            page, pageSize, total));
    }

    /// <summary>Shortlist, accept or reject an application (Employer only).</summary>
    [HttpPost("applications/{applicationId:guid}/status")]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<IActionResult> UpdateApplicationStatus(
        Guid applicationId,
        [FromBody] UpdateApplicationStatusRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var app = await _db.JobApplications
            .Include(a => a.JobPost)
            .Include(a => a.Worker)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (app is null) return NotFound();

        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (app.JobPost.EmployerId != employer?.Id && !User.IsInRole("Admin")) return Forbid();

        if (!Enum.TryParse<JobApplicationStatus>(req.Status, out var newStatus))
            return BadRequest(new { message = $"Invalid status '{req.Status}'." });

        app.Status       = newStatus;
        app.ReviewedAt   = DateTime.UtcNow;
        app.EmployerNote = req.EmployerNote;
        await _db.SaveChangesAsync();

        // Notify worker
        if (app.Worker.UserId is not null)
        {
            var msg = newStatus switch
            {
                JobApplicationStatus.Accepted    => $"Congratulations! Your application for '{app.JobPost.Title}' has been accepted.",
                JobApplicationStatus.Shortlisted => $"You have been shortlisted for '{app.JobPost.Title}'.",
                JobApplicationStatus.Rejected    => $"Your application for '{app.JobPost.Title}' was not selected this time.",
                _                                => $"Your application for '{app.JobPost.Title}' status updated to {newStatus}."
            };
            await _notify.SendAsync(app.Worker.UserId, msg, NotificationChannel.SMS);
        }

        return NoContent();
    }

    // ?? Worker: apply / view own applications ?????????????????????????????????

    /// <summary>Apply to a job post (Worker only).</summary>
    [HttpPost("apply")]
    [Authorize(Roles = "Worker")]
    public async Task<ActionResult<JobApplicationDto>> Apply([FromBody] ApplyJobRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null)
            return BadRequest(new { message = "Worker profile not found." });

        var job = await _db.JobPosts
            .Include(j => j.Employer)
            .Include(j => j.SkillCategory)
            .FirstOrDefaultAsync(j => j.Id == req.JobPostId);

        if (job is null) return NotFound(new { message = "Job post not found." });
        if (job.Status != JobPostStatus.Open)
            return BadRequest(new { message = "This job is no longer accepting applications." });

        var duplicate = await _db.JobApplications
            .AnyAsync(a => a.JobPostId == req.JobPostId && a.WorkerId == worker.Id);
        if (duplicate)
            return Conflict(new { message = "You have already applied to this job." });

        var application = new JobApplication
        {
            Id        = Guid.NewGuid(),
            JobPostId = req.JobPostId,
            WorkerId  = worker.Id,
            CoverNote = req.CoverNote
        };

        _db.JobApplications.Add(application);
        await _db.SaveChangesAsync();

        // Notify employer
        if (job.Employer.UserId is not null)
        {
            await _notify.SendAsync(
                job.Employer.UserId,
                $"New application received for '{job.Title}' from {worker.FullName}.",
                NotificationChannel.SMS);
        }

        await _db.Entry(application).Reference(a => a.Worker).LoadAsync();
        return CreatedAtAction(nameof(GetJob), new { id = job.Id },
            ToApplicationDto(application, job));
    }

    /// <summary>Get the authenticated worker's own job applications.</summary>
    [HttpGet("applications/mine")]
    [Authorize(Roles = "Worker")]
    public async Task<ActionResult<PagedResult<JobApplicationDto>>> GetMyApplications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page     = Math.Max(1, page);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null)
            return Ok(new PagedResult<JobApplicationDto>([], page, pageSize, 0));

        var total = await _db.JobApplications.CountAsync(a => a.WorkerId == worker.Id);
        var apps  = await _db.JobApplications
            .Include(a => a.JobPost).ThenInclude(j => j.Employer)
            .Include(a => a.JobPost).ThenInclude(j => j.SkillCategory)
            .Include(a => a.Worker)
            .Where(a => a.WorkerId == worker.Id)
            .OrderByDescending(a => a.AppliedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<JobApplicationDto>(
            apps.Select(a => ToApplicationDto(a, a.JobPost)).ToList(),
            page, pageSize, total));
    }

    /// <summary>Withdraw a job application (Worker only).</summary>
    [HttpPost("applications/{applicationId:guid}/withdraw")]
    [Authorize(Roles = "Worker")]
    public async Task<IActionResult> WithdrawApplication(Guid applicationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        var app    = await _db.JobApplications.FirstOrDefaultAsync(a => a.Id == applicationId);

        if (app is null) return NotFound();
        if (app.WorkerId != worker?.Id) return Forbid();
        if (app.Status == JobApplicationStatus.Accepted)
            return BadRequest(new { message = "Cannot withdraw an accepted application." });

        app.Status = JobApplicationStatus.Withdrawn;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ?? Helpers ???????????????????????????????????????????????????????????????

    private static JobPostDto ToDto(JobPost j, int appCount) => new(
        j.Id, j.EmployerId, j.Employer.Name,
        j.SkillCategory.Name, j.Title, j.Description,
        j.LocationCity, j.LocationState,
        j.DailyWage, j.DurationDays, j.StartDate,
        j.VacancyCount, appCount,
        j.Status.ToString(), j.CreatedAt);

    private static JobApplicationDto ToApplicationDto(JobApplication a, JobPost job) => new(
        a.Id, job.Id, job.Title, job.SkillCategory.Name, job.Employer.Name,
        job.LocationCity, job.DailyWage, job.StartDate,
        a.WorkerId, a.Worker.FullName, a.Worker.PhotoUrl,
        a.CoverNote, a.Status.ToString(), a.AppliedAt, a.EmployerNote);
}
