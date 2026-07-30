using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class WorkerDocument
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public DocumentType Type { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public bool IsVerified { get; set; } = false;
    public string? AdminNotes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }
}
