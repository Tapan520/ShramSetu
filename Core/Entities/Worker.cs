using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class Worker
{
    public Guid Id { get; set; }

    // Identity user linkage (nullable  worker may register without an account initially)
    public string? UserId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    /// <summary>Hashed Aadhaar number  never store raw.</summary>
    public string? AadhaarHash { get; set; }

    public VerificationStatus KycStatus { get; set; } = VerificationStatus.Pending;

    public Guid SkillCategoryId { get; set; }
    public SkillCategory SkillCategory { get; set; } = null!;

    /// <summary>Comma-separated sub-skills (e.g. "plumbing,pipe fitting").</summary>
    public string? SubSkills { get; set; }

    public int YearsOfExperience { get; set; }
    public decimal ExpectedDailyWage { get; set; }
    public decimal ExpectedMonthlyWage { get; set; }

    public string? LocationCity { get; set; }
    public string? LocationState { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public bool IsAvailable { get; set; } = true;
    public string? PhotoUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WorkerDocument> Documents { get; set; } = new List<WorkerDocument>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    public ICollection<WorkerAvailability> Availabilities { get; set; } = new List<WorkerAvailability>();
    public ICollection<SavedWorker> SavedByEmployers { get; set; } = new List<SavedWorker>();
    public ICollection<BackgroundCheck> BackgroundChecks { get; set; } = new List<BackgroundCheck>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<WorkforceTeamMember> TeamMemberships { get; set; } = new List<WorkforceTeamMember>();
    public ICollection<WorkerBadge> Badges { get; set; } = new List<WorkerBadge>();
    public ICollection<WorkerPortfolioPhoto> PortfolioPhotos { get; set; } = new List<WorkerPortfolioPhoto>();
    public ICollection<JobAlert> JobAlerts { get; set; } = new List<JobAlert>();
    public ICollection<EmployerReview> EmployerReviews { get; set; } = new List<EmployerReview>();
    public ICollection<SalaryAdvance> SalaryAdvances { get; set; } = new List<SalaryAdvance>();
    public ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();
    public ICollection<SkillAssessment> SkillAssessments { get; set; } = new List<SkillAssessment>();
    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
