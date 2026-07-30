using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;

namespace ShramSetu.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SkillCategory> SkillCategories => Set<SkillCategory>();
    public DbSet<Worker> Workers => Set<Worker>();
    public DbSet<WorkerDocument> WorkerDocuments => Set<WorkerDocument>();
    public DbSet<EmployerAccount> EmployerAccounts => Set<EmployerAccount>();
    public DbSet<SourcingRequest> SourcingRequests => Set<SourcingRequest>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<JobPost> JobPosts => Set<JobPost>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    // Phase 2
    public DbSet<WorkerAvailability> WorkerAvailabilities => Set<WorkerAvailability>();
    public DbSet<SavedWorker> SavedWorkers => Set<SavedWorker>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<PushToken> PushTokens => Set<PushToken>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<EmployerSubscription> EmployerSubscriptions => Set<EmployerSubscription>();

    // Phase 3
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();
    public DbSet<BackgroundCheck> BackgroundChecks => Set<BackgroundCheck>();
    public DbSet<WorkforceTeam> WorkforceTeams => Set<WorkforceTeam>();
    public DbSet<WorkforceTeamMember> WorkforceTeamMembers => Set<WorkforceTeamMember>();
    public DbSet<WorkerMatchScore> WorkerMatchScores => Set<WorkerMatchScore>();

    // Sprint 1  Trust & Safety
    public DbSet<Dispute> Disputes => Set<Dispute>();
    public DbSet<DisputeEvidence> DisputeEvidences => Set<DisputeEvidence>();
    public DbSet<EmployerReview> EmployerReviews => Set<EmployerReview>();
    public DbSet<UserReport> UserReports => Set<UserReport>();
    public DbSet<WorkerBadge> WorkerBadges => Set<WorkerBadge>();

    // Sprint 2  Worker Welfare
    public DbSet<SalaryAdvance> SalaryAdvances => Set<SalaryAdvance>();
    public DbSet<WorkerWallet> WorkerWallets => Set<WorkerWallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<WorkerPortfolioPhoto> WorkerPortfolioPhotos => Set<WorkerPortfolioPhoto>();

    // Sprint 3  Growth
    public DbSet<JobAlert> JobAlerts => Set<JobAlert>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    // Sprint 4  Operations
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<WorkContract> WorkContracts => Set<WorkContract>();
    public DbSet<WageRateCard> WageRateCards => Set<WageRateCard>();

    // Sprint 5/6  Technical
    public DbSet<AppVersion> AppVersions => Set<AppVersion>();

    // Sprint 7  Admin Power
    public DbSet<UserBan> UserBans => Set<UserBan>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<PlatformFee> PlatformFees => Set<PlatformFee>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

    // Sprint 8  Worker Experience
    public DbSet<WorkerOnboarding> WorkerOnboardings => Set<WorkerOnboarding>();
    public DbSet<SkillAssessment> SkillAssessments => Set<SkillAssessment>();
    public DbSet<AssessmentQuestion> AssessmentQuestions => Set<AssessmentQuestion>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();
    public DbSet<WorkerOfTheMonth> WorkerOfTheMonths => Set<WorkerOfTheMonth>();

    // Sprint 9  Employer Experience
    public DbSet<JobPostTemplate> JobPostTemplates => Set<JobPostTemplate>();
    public DbSet<GstInvoice> GstInvoices => Set<GstInvoice>();

    // Sprint 10  Compliance
    public DbSet<ComplianceCheck> ComplianceChecks => Set<ComplianceCheck>();
    public DbSet<MinimumWageConfig> MinimumWageConfigs => Set<MinimumWageConfig>();

    // Sprint 11  Session & Mobile
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<OnboardingSlide> OnboardingSlides => Set<OnboardingSlide>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Worker
        builder.Entity<Worker>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.ExpectedDailyWage).HasColumnType("decimal(18,2)");
            e.Property(w => w.ExpectedMonthlyWage).HasColumnType("decimal(18,2)");
            e.HasOne(w => w.SkillCategory)
             .WithMany(s => s.Workers)
             .HasForeignKey(w => w.SkillCategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // WorkerDocument
        builder.Entity<WorkerDocument>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasOne(d => d.Worker)
             .WithMany(w => w.Documents)
             .HasForeignKey(d => d.WorkerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // EmployerAccount
        builder.Entity<EmployerAccount>(e =>
        {
            e.HasKey(ea => ea.Id);
        });

        // SourcingRequest
        builder.Entity<SourcingRequest>(e =>
        {
            e.HasKey(sr => sr.Id);
            e.Property(sr => sr.BudgetPerDay).HasColumnType("decimal(18,2)");
            e.HasOne(sr => sr.Employer)
             .WithMany(ea => ea.SourcingRequests)
             .HasForeignKey(sr => sr.EmployerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(sr => sr.SkillCategory)
             .WithMany(sc => sc.SourcingRequests)
             .HasForeignKey(sr => sr.SkillCategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Booking
        builder.Entity<Booking>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.AgreedWage).HasColumnType("decimal(18,2)");
            e.HasOne(b => b.Worker)
             .WithMany(w => w.Bookings)
             .HasForeignKey(b => b.WorkerId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.Employer)
             .WithMany(ea => ea.Bookings)
             .HasForeignKey(b => b.EmployerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Review
        builder.Entity<Review>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasOne(r => r.Worker)
             .WithMany(w => w.Reviews)
             .HasForeignKey(r => r.WorkerId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Employer)
             .WithMany(ea => ea.Reviews)
             .HasForeignKey(r => r.EmployerId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Booking)
             .WithOne(b => b.Review)
             .HasForeignKey<Review>(r => r.BookingId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Notification
        builder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
        });

        // OtpCode
        builder.Entity<OtpCode>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.Phone);
        });

        // JobPost
        builder.Entity<JobPost>(e =>
        {
            e.HasKey(j => j.Id);
            e.Property(j => j.DailyWage).HasColumnType("decimal(18,2)");
            e.HasOne(j => j.Employer)
             .WithMany(ea => ea.JobPosts)
             .HasForeignKey(j => j.EmployerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(j => j.SkillCategory)
             .WithMany(sc => sc.JobPosts)
             .HasForeignKey(j => j.SkillCategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // JobApplication
        builder.Entity<JobApplication>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.JobPostId, a.WorkerId }).IsUnique();
            e.HasOne(a => a.JobPost)
             .WithMany(j => j.Applications)
             .HasForeignKey(a => a.JobPostId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Worker)
             .WithMany(w => w.JobApplications)
             .HasForeignKey(a => a.WorkerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ?? Phase 2 ??????????????????????????????????????????????????????????

        builder.Entity<WorkerAvailability>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasOne(a => a.Worker)
             .WithMany(w => w.Availabilities)
             .HasForeignKey(a => a.WorkerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SavedWorker>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.EmployerId, s.WorkerId }).IsUnique();
            e.HasOne(s => s.Employer)
             .WithMany(ea => ea.SavedWorkers)
             .HasForeignKey(s => s.EmployerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Worker)
             .WithMany(w => w.SavedByEmployers)
             .HasForeignKey(s => s.WorkerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ChatMessage>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.RoomKey);
            e.HasIndex(c => c.SentAt);
        });

        builder.Entity<PushToken>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.UserId);
            e.HasIndex(p => p.Token).IsUnique();
        });

        builder.Entity<SubscriptionPlan>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.PriceMonthly).HasColumnType("decimal(18,2)");
            e.Property(p => p.PriceYearly).HasColumnType("decimal(18,2)");
        });

        builder.Entity<EmployerSubscription>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.AmountPaid).HasColumnType("decimal(18,2)");
            e.HasOne(s => s.Employer)
             .WithMany(ea => ea.Subscriptions)
             .HasForeignKey(s => s.EmployerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Plan)
             .WithMany(p => p.Subscriptions)
             .HasForeignKey(s => s.PlanId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ?? Phase 3 ??????????????????????????????????????????????????????????

        builder.Entity<AttendanceRecord>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.HoursWorked).HasColumnType("decimal(6,2)");
            e.HasIndex(a => new { a.BookingId, a.Date });
            e.HasOne(a => a.Booking)
             .WithMany()
             .HasForeignKey(a => a.BookingId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Worker)
             .WithMany(w => w.AttendanceRecords)
             .HasForeignKey(a => a.WorkerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PayrollRecord>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.DailyWage).HasColumnType("decimal(18,2)");
            e.Property(p => p.GrossAmount).HasColumnType("decimal(18,2)");
            e.Property(p => p.Deductions).HasColumnType("decimal(18,2)");
            e.Property(p => p.NetAmount).HasColumnType("decimal(18,2)");
            e.HasOne(p => p.Booking)
             .WithMany()
             .HasForeignKey(p => p.BookingId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Worker)
             .WithMany()
             .HasForeignKey(p => p.WorkerId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Employer)
             .WithMany()
             .HasForeignKey(p => p.EmployerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BackgroundCheck>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasOne(b => b.Worker)
             .WithMany(w => w.BackgroundChecks)
             .HasForeignKey(b => b.WorkerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WorkforceTeam>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasOne(t => t.Employer)
             .WithMany(ea => ea.Teams)
             .HasForeignKey(t => t.EmployerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WorkforceTeamMember>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.TeamId, m.WorkerId });
            e.HasOne(m => m.Team)
             .WithMany(t => t.Members)
             .HasForeignKey(m => m.TeamId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Worker)
             .WithMany(w => w.TeamMemberships)
             .HasForeignKey(m => m.WorkerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WorkerMatchScore>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasOne(m => m.Worker)
             .WithMany()
             .HasForeignKey(m => m.WorkerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.JobPost)
             .WithMany()
             .HasForeignKey(m => m.JobPostId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(m => m.SourcingRequest)
             .WithMany()
             .HasForeignKey(m => m.SourcingRequestId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ?? Sprint 1: Trust & Safety ??????????????????????????????????????????

        builder.Entity<Dispute>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasOne(d => d.Booking)
             .WithMany()
             .HasForeignKey(d => d.BookingId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<DisputeEvidence>(e =>
        {
            e.HasKey(ev => ev.Id);
            e.HasOne(ev => ev.Dispute)
             .WithMany(d => d.Evidence)
             .HasForeignKey(ev => ev.DisputeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<EmployerReview>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => new { r.BookingId, r.WorkerId }).IsUnique();
            e.HasOne(r => r.Employer)
             .WithMany(ea => ea.ReviewsReceived)
             .HasForeignKey(r => r.EmployerId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Worker)
             .WithMany(w => w.EmployerReviews)
             .HasForeignKey(r => r.WorkerId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Booking)
             .WithMany()
             .HasForeignKey(r => r.BookingId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserReport>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasOne(r => r.JobPost)
             .WithMany()
             .HasForeignKey(r => r.JobPostId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<WorkerBadge>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasOne(b => b.Worker)
             .WithMany(w => w.Badges)
             .HasForeignKey(b => b.WorkerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ?? Sprint 2: Worker Welfare ??????????????????????????????????????????

        builder.Entity<SalaryAdvance>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Amount).HasColumnType("decimal(18,2)");
            e.Property(a => a.AmountRepaid).HasColumnType("decimal(18,2)");
            e.HasOne(a => a.Worker)
             .WithMany(w => w.SalaryAdvances)
             .HasForeignKey(a => a.WorkerId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Employer)
             .WithMany(ea => ea.SalaryAdvances)
             .HasForeignKey(a => a.EmployerId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Booking)
             .WithMany()
             .HasForeignKey(a => a.BookingId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<WorkerWallet>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Balance).HasColumnType("decimal(18,2)");
            e.HasIndex(w => w.WorkerId).IsUnique();
            e.HasOne(w => w.Worker)
             .WithMany()
             .HasForeignKey(w => w.WorkerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WalletTransaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            e.Property(t => t.BalanceAfter).HasColumnType("decimal(18,2)");
            e.HasOne(t => t.Wallet)
             .WithMany(w => w.Transactions)
             .HasForeignKey(t => t.WalletId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WorkerPortfolioPhoto>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasOne(p => p.Worker)
             .WithMany(w => w.PortfolioPhotos)
             .HasForeignKey(p => p.WorkerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ?? Sprint 3: Growth ??????????????????????????????????????????????????

        builder.Entity<JobAlert>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.MinWage).HasColumnType("decimal(18,2)");
            e.Property(a => a.MaxWage).HasColumnType("decimal(18,2)");
            e.HasOne(a => a.Worker)
             .WithMany(w => w.JobAlerts)
             .HasForeignKey(a => a.WorkerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.SkillCategory)
             .WithMany()
             .HasForeignKey(a => a.SkillCategoryId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Referral>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.RewardAmount).HasColumnType("decimal(18,2)");
            e.HasIndex(r => r.Code).IsUnique();
        });

        builder.Entity<NotificationPreference>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.UserId).IsUnique();
        });

        // ?? Sprint 4: Operations ??????????????????????????????????????????????

        builder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.EntityType, a.EntityId });
            e.HasIndex(a => a.OccurredAt);
        });

        builder.Entity<WorkContract>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.BookingId).IsUnique();
            e.HasOne(c => c.Booking)
             .WithMany()
             .HasForeignKey(c => c.BookingId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WageRateCard>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.MinDailyWage).HasColumnType("decimal(18,2)");
            e.Property(w => w.MaxDailyWage).HasColumnType("decimal(18,2)");
            e.Property(w => w.RecommendedDailyWage).HasColumnType("decimal(18,2)");
            e.HasOne(w => w.SkillCategory)
             .WithMany()
             .HasForeignKey(w => w.SkillCategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ?? Sprint 5/6: Technical ?????????????????????????????????????????????

        builder.Entity<AppVersion>(e =>
        {
            e.HasKey(v => v.Id);
            e.HasIndex(v => v.Platform).IsUnique();
        });

        // ?? Sprint 7: Admin Power ?????????????????????????????????????????????

        builder.Entity<UserBan>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => b.UserId);
        });

        builder.Entity<Announcement>(e =>
        {
            e.HasKey(a => a.Id);
        });

        builder.Entity<PlatformFee>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Amount).HasColumnType("decimal(18,2)");
            e.Property(f => f.CommissionRate).HasColumnType("decimal(5,2)");
            e.HasOne(f => f.Booking)
             .WithMany()
             .HasForeignKey(f => f.BookingId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(f => f.Subscription)
             .WithMany()
             .HasForeignKey(f => f.SubscriptionId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<FeatureFlag>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => f.Name).IsUnique();
        });

        // ?? Sprint 8: Worker Experience ???????????????????????????????????????

        builder.Entity<WorkerOnboarding>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.WorkerId).IsUnique();
            e.HasOne(o => o.Worker)
             .WithMany()
             .HasForeignKey(o => o.WorkerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Ignore(o => o.CompletenessScore);
            e.Ignore(o => o.IsCompleted);
        });

        builder.Entity<SkillAssessment>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.WorkerId, a.SkillCategoryId }).IsUnique();
            e.HasOne(a => a.Worker)
             .WithMany(w => w.SkillAssessments)
             .HasForeignKey(a => a.WorkerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.SkillCategory)
             .WithMany()
             .HasForeignKey(a => a.SkillCategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AssessmentQuestion>(e =>
        {
            e.HasKey(q => q.Id);
            e.HasOne(q => q.SkillCategory)
             .WithMany()
             .HasForeignKey(q => q.SkillCategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EmergencyContact>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasOne(c => c.Worker)
             .WithMany(w => w.EmergencyContacts)
             .HasForeignKey(c => c.WorkerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WorkerOfTheMonth>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasIndex(w => new { w.Month, w.Year }).IsUnique();
            e.HasOne(w => w.Worker)
             .WithMany()
             .HasForeignKey(w => w.WorkerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ?? Sprint 9: Employer Experience ?????????????????????????????????????

        builder.Entity<JobPostTemplate>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.DailyWage).HasColumnType("decimal(18,2)");
            e.HasOne(t => t.Employer)
             .WithMany(ea => ea.JobPostTemplates)
             .HasForeignKey(t => t.EmployerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.SkillCategory)
             .WithMany()
             .HasForeignKey(t => t.SkillCategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GstInvoice>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.BaseAmount).HasColumnType("decimal(18,2)");
            e.Property(g => g.CgstRate).HasColumnType("decimal(5,2)");
            e.Property(g => g.SgstRate).HasColumnType("decimal(5,2)");
            e.Property(g => g.CgstAmount).HasColumnType("decimal(18,2)");
            e.Property(g => g.SgstAmount).HasColumnType("decimal(18,2)");
            e.Property(g => g.TotalAmount).HasColumnType("decimal(18,2)");
            e.HasIndex(g => g.InvoiceNumber).IsUnique();
            e.HasOne(g => g.Subscription)
             .WithMany()
             .HasForeignKey(g => g.SubscriptionId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ?? Sprint 10: Compliance ?????????????????????????????????????????????

        builder.Entity<ComplianceCheck>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasOne(c => c.Booking)
             .WithMany()
             .HasForeignKey(c => c.BookingId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MinimumWageConfig>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.MinDailyWage).HasColumnType("decimal(18,2)");
            e.HasIndex(m => new { m.State, m.SkillCategoryId });
            e.HasOne(m => m.SkillCategory)
             .WithMany()
             .HasForeignKey(m => m.SkillCategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ?? Sprint 11: Session & Mobile ???????????????????????????????????????

        builder.Entity<UserSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.UserId);
            e.HasIndex(s => s.SessionToken).IsUnique();
        });

        builder.Entity<OnboardingSlide>(e =>
        {
            e.HasKey(s => s.Id);
        });

        builder.Entity<Testimonial>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.MonthlyEarnings).HasColumnType("decimal(18,2)");
            e.HasOne(t => t.Worker)
             .WithMany()
             .HasForeignKey(t => t.WorkerId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
