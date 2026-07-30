using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using ShramSetu.Core.Entities;
using ShramSetu.Data;
using ShramSetu.Services;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/worker")]
[Produces("application/json")]
public class WorkerExperienceController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IQrCodeService _qr;

    public WorkerExperienceController(ApplicationDbContext db, IQrCodeService qr)
    {
        _db = db;
        _qr = qr;
    }

    // ?? Onboarding Progress ???????????????????????????????????????????????????

    [HttpGet("onboarding")]
    [Authorize(Roles = "Worker")]
    public async Task<IActionResult> GetOnboardingStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return NotFound();

        var ob = await _db.WorkerOnboardings.FirstOrDefaultAsync(o => o.WorkerId == worker.Id)
              ?? new WorkerOnboarding { WorkerId = worker.Id };

        // Auto-compute from worker data
        ob.PhotoDone     = !string.IsNullOrEmpty(worker.PhotoUrl);
        ob.SkillsDone    = worker.SkillCategoryId != Guid.Empty;
        ob.LocationDone  = !string.IsNullOrEmpty(worker.LocationCity);
        ob.DocumentsDone = await _db.WorkerDocuments.AnyAsync(d => d.WorkerId == worker.Id);
        ob.BankDone      = await _db.WorkerWallets.AnyAsync(w => w.WorkerId == worker.Id
            && (w.UpiId != null || w.BankAccountNumber != null));

        return Ok(new
        {
            ob.PhotoDone, ob.SkillsDone, ob.LocationDone, ob.DocumentsDone, ob.BankDone,
            ob.CompletenessScore, ob.IsCompleted
        });
    }

    // ?? QR Code ???????????????????????????????????????????????????????????????

    [HttpGet("{workerId:guid}/qr")]
    public IActionResult GetQrCode(Guid workerId, [FromQuery] string? baseUrl)
    {
        var url    = $"{baseUrl?.TrimEnd('/') ?? "https://shramsetu.in"}/workers/profile/{workerId}";
        var base64 = _qr.GenerateBase64(url);
        return Ok(new { qrBase64 = base64, profileUrl = url });
    }

    // ?? Emergency Contacts ????????????????????????????????????????????????????

    [HttpGet("emergency-contacts")]
    [Authorize(Roles = "Worker")]
    public async Task<IActionResult> GetEmergencyContacts()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return NotFound();

        return Ok(await _db.EmergencyContacts.Where(c => c.WorkerId == worker.Id).ToListAsync());
    }

    [HttpPost("emergency-contacts")]
    [Authorize(Roles = "Worker")]
    public async Task<IActionResult> AddEmergencyContact([FromBody] EmergencyContactRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return NotFound();

        if (!Enum.TryParse<Core.Enums.EmergencyRelation>(req.Relation, out var rel))
            return BadRequest(new { message = $"Invalid relation '{req.Relation}'." });

        // Ensure only one primary
        if (req.IsPrimary)
        {
            var existing = await _db.EmergencyContacts
                .Where(c => c.WorkerId == worker.Id && c.IsPrimary)
                .ToListAsync();
            foreach (var e in existing) e.IsPrimary = false;
        }

        _db.EmergencyContacts.Add(new EmergencyContact
        {
            Id = Guid.NewGuid(), WorkerId = worker.Id,
            Name = req.Name, Phone = req.Phone, Relation = rel, IsPrimary = req.IsPrimary
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Emergency contact added." });
    }

    [HttpDelete("emergency-contacts/{id:guid}")]
    [Authorize(Roles = "Worker")]
    public async Task<IActionResult> DeleteEmergencyContact(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        var contact = await _db.EmergencyContacts.FindAsync(id);
        if (contact is null || contact.WorkerId != worker?.Id) return NotFound();
        _db.EmergencyContacts.Remove(contact);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ?? Skill Assessment ??????????????????????????????????????????????????????

    [HttpGet("assessments")]
    [Authorize(Roles = "Worker")]
    public async Task<IActionResult> GetAssessments()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return NotFound();

        return Ok(await _db.SkillAssessments
            .Include(a => a.SkillCategory)
            .Where(a => a.WorkerId == worker.Id)
            .ToListAsync());
    }

    [HttpGet("assessments/{skillCategoryId:guid}/questions")]
    [Authorize(Roles = "Worker")]
    public async Task<IActionResult> GetQuestions(Guid skillCategoryId)
    {
        // Return questions WITHOUT correct answers
        var questions = await _db.AssessmentQuestions
            .Where(q => q.SkillCategoryId == skillCategoryId && q.IsActive)
            .OrderBy(_ => Guid.NewGuid())  // randomise
            .Take(10)
            .Select(q => new { q.Id, q.QuestionText, q.OptionA, q.OptionB, q.OptionC, q.OptionD, q.Marks })
            .ToListAsync();
        return Ok(questions);
    }

    [HttpPost("assessments/{skillCategoryId:guid}/submit")]
    [Authorize(Roles = "Worker")]
    public async Task<IActionResult> SubmitAssessment(Guid skillCategoryId,
        [FromBody] AssessmentSubmission submission)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return NotFound();

        // Score the answers
        var questions = await _db.AssessmentQuestions
            .Where(q => q.SkillCategoryId == skillCategoryId && q.IsActive)
            .ToListAsync();

        int totalMarks    = questions.Sum(q => q.Marks);
        int earnedMarks   = 0;

        foreach (var ans in submission.Answers)
        {
            var q = questions.FirstOrDefault(x => x.Id == ans.QuestionId);
            if (q is not null && q.CorrectOption.Equals(ans.SelectedOption, StringComparison.OrdinalIgnoreCase))
                earnedMarks += q.Marks;
        }

        int score = totalMarks > 0 ? (int)((earnedMarks / (double)totalMarks) * 100) : 0;

        var assessment = await _db.SkillAssessments
            .FirstOrDefaultAsync(a => a.WorkerId == worker.Id && a.SkillCategoryId == skillCategoryId);

        bool passed = score >= 70;

        if (assessment is null)
        {
            assessment = new SkillAssessment
            {
                Id = Guid.NewGuid(), WorkerId = worker.Id, SkillCategoryId = skillCategoryId,
                Score = score, AttemptCount = 1,
                Status = passed ? Core.Enums.SkillAssessmentStatus.Passed : Core.Enums.SkillAssessmentStatus.Failed,
                LastAttemptAt = DateTime.UtcNow,
                PassedAt = passed ? DateTime.UtcNow : null
            };
            _db.SkillAssessments.Add(assessment);
        }
        else
        {
            assessment.Score          = Math.Max(assessment.Score, score);
            assessment.AttemptCount  += 1;
            assessment.LastAttemptAt  = DateTime.UtcNow;
            if (passed && assessment.Status != Core.Enums.SkillAssessmentStatus.Passed)
            {
                assessment.Status   = Core.Enums.SkillAssessmentStatus.Passed;
                assessment.PassedAt = DateTime.UtcNow;
            }
        }
        await _db.SaveChangesAsync();

        return Ok(new { score, passed, earnedMarks, totalMarks,
            message = passed ? "?? Congratulations! You passed and earned a Skill Verified badge." : $"Score: {score}/100. You need 70 to pass. Try again!" });
    }

    public record EmergencyContactRequest(string Name, string Phone, string Relation, bool IsPrimary);
    public record AssessmentSubmission(List<AnswerItem> Answers);
    public record AnswerItem(Guid QuestionId, string SelectedOption);
}
