using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class EmployerAccount
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public EmployerType Type { get; set; } = EmployerType.Individual;
    public string? CompanyName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<SourcingRequest> SourcingRequests { get; set; } = new List<SourcingRequest>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<JobPost> JobPosts { get; set; } = new List<JobPost>();
    public ICollection<SavedWorker> SavedWorkers { get; set; } = new List<SavedWorker>();
    public ICollection<EmployerSubscription> Subscriptions { get; set; } = new List<EmployerSubscription>();
    public ICollection<WorkforceTeam> Teams { get; set; } = new List<WorkforceTeam>();
    public ICollection<EmployerReview> ReviewsReceived { get; set; } = new List<EmployerReview>();
    public ICollection<SalaryAdvance> SalaryAdvances { get; set; } = new List<SalaryAdvance>();
    public ICollection<JobPostTemplate> JobPostTemplates { get; set; } = new List<JobPostTemplate>();
    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
