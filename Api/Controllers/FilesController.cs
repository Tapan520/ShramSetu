using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Data;
using ShramSetu.Services;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
[Produces("application/json")]
public class FilesController : ControllerBase
{
    private static readonly string[] ImageExts  = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] DocExts    = [".jpg", ".jpeg", ".png", ".pdf"];

    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IPdfService _pdf;

    public FilesController(ApplicationDbContext db, IFileStorageService storage, IPdfService pdf)
    {
        _db      = db;
        _storage = storage;
        _pdf     = pdf;
    }

    /// <summary>Upload worker profile photo.</summary>
    [HttpPost("worker/photo")]
    [Authorize(Roles = "Worker")]
    public async Task<IActionResult> UploadWorkerPhoto(IFormFile file)
    {
        if (!_storage.IsAllowed(file, ImageExts))
            return BadRequest(new { message = "Only JPG, PNG or WEBP images allowed." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return NotFound();

        // Delete old photo
        if (!string.IsNullOrEmpty(worker.PhotoUrl))
            await _storage.DeleteAsync(worker.PhotoUrl);

        worker.PhotoUrl = await _storage.SaveAsync(file, "worker-photos");
        await _db.SaveChangesAsync();

        return Ok(new { photoUrl = worker.PhotoUrl });
    }

    /// <summary>Upload worker KYC document.</summary>
    [HttpPost("worker/document")]
    [Authorize(Roles = "Worker")]
    public async Task<IActionResult> UploadWorkerDocument(IFormFile file,
        [FromQuery] string documentType = "Aadhaar")
    {
        if (!_storage.IsAllowed(file, DocExts))
            return BadRequest(new { message = "Only JPG, PNG or PDF files allowed." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return NotFound();

        var fileUrl = await _storage.SaveAsync(file, "kyc-documents");

        if (!Enum.TryParse<Core.Enums.DocumentType>(documentType, out var docType))
            docType = Core.Enums.DocumentType.Aadhaar;

        _db.WorkerDocuments.Add(new Core.Entities.WorkerDocument
        {
            Id         = Guid.NewGuid(),
            WorkerId   = worker.Id,
            Type       = docType,
            FileUrl    = fileUrl,
            UploadedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return Ok(new { fileUrl, documentType });
    }

    /// <summary>Upload portfolio photo.</summary>
    [HttpPost("worker/portfolio")]
    [Authorize(Roles = "Worker")]
    public async Task<IActionResult> UploadPortfolioPhoto(IFormFile file,
        [FromQuery] string? caption)
    {
        if (!_storage.IsAllowed(file, ImageExts))
            return BadRequest(new { message = "Only JPG, PNG or WEBP images allowed." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return NotFound();

        var fileUrl = await _storage.SaveAsync(file, "portfolio");
        var order   = await _db.WorkerPortfolioPhotos.CountAsync(p => p.WorkerId == worker.Id);

        _db.WorkerPortfolioPhotos.Add(new Core.Entities.WorkerPortfolioPhoto
        {
            Id = Guid.NewGuid(), WorkerId = worker.Id,
            PhotoUrl = fileUrl, Caption = caption, DisplayOrder = order + 1
        });
        await _db.SaveChangesAsync();

        return Ok(new { fileUrl });
    }

    /// <summary>Upload dispute evidence file.</summary>
    [HttpPost("dispute/{disputeId:guid}/evidence")]
    public async Task<IActionResult> UploadDisputeEvidence(Guid disputeId, IFormFile file)
    {
        if (!_storage.IsAllowed(file, DocExts))
            return BadRequest(new { message = "Only JPG, PNG or PDF files allowed." });

        var dispute = await _db.Disputes.FindAsync(disputeId);
        if (dispute is null) return NotFound();

        var fileUrl = await _storage.SaveAsync(file, "dispute-evidence");
        return Ok(new { fileUrl });
    }

    /// <summary>Download Worker CV as PDF.</summary>
    [HttpGet("worker/{workerId:guid}/cv")]
    public async Task<IActionResult> DownloadWorkerCv(Guid workerId)
    {
        var worker = await _db.Workers
            .Include(w => w.SkillCategory)
            .FirstOrDefaultAsync(w => w.Id == workerId && !w.IsDeleted);
        if (worker is null) return NotFound();

        var reviews = await _db.Reviews.Where(r => r.WorkerId == workerId).ToListAsync();
        var badges  = await _db.WorkerBadges.Where(b => b.WorkerId == workerId && b.IsActive).ToListAsync();

        var pdf = _pdf.GenerateWorkerCv(worker, reviews, badges);
        return File(pdf, "application/pdf",
            $"ShramSetu_CV_{worker.FullName.Replace(" ", "_")}.pdf");
    }
}
