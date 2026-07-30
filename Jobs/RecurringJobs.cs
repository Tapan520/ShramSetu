using Hangfire;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Enums;
using ShramSetu.Data;
using ShramSetu.Services;

namespace ShramSetu.Jobs;

/// <summary>All recurring background jobs registered with Hangfire.</summary>
public class RecurringJobsSetup
{
    public static void RegisterAll()
    {
        // Run job alert dispatch every hour
        RecurringJob.AddOrUpdate<JobAlertDispatchJob>(
            "job-alert-dispatch",
            job => job.RunAsync(CancellationToken.None),
            Cron.Hourly);

        // Daily: clean expired OTPs and sessions
        RecurringJob.AddOrUpdate<CleanupJob>(
            "daily-cleanup",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(2, 0));   // 2:00 AM UTC

        // Daily: send payroll reminders for bookings ending today
        RecurringJob.AddOrUpdate<PayrollReminderJob>(
            "payroll-reminder",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(8, 0));   // 8:00 AM UTC

        // Every 6 hours: check SLA breach on open disputes
        RecurringJob.AddOrUpdate<SlaBreachJob>(
            "sla-breach-check",
            job => job.RunAsync(CancellationToken.None),
            Cron.HourInterval(6));

        // Weekly: send job digest email to workers
        RecurringJob.AddOrUpdate<WeeklyDigestJob>(
            "weekly-digest",
            job => job.RunAsync(CancellationToken.None),
            Cron.Weekly(DayOfWeek.Monday, 8, 0));
    }
}

// ?? Job 1: Dispatch Job Alerts for recently posted jobs ??????????????????????

public class JobAlertDispatchJob
{
    private readonly ApplicationDbContext _db;
    private readonly IJobAlertService _alertService;
    private readonly ILogger<JobAlertDispatchJob> _logger;

    public JobAlertDispatchJob(ApplicationDbContext db, IJobAlertService alertService,
        ILogger<JobAlertDispatchJob> logger)
    {
        _db = db; _alertService = alertService; _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddHours(-1);
        var newJobs = await _db.JobPosts
            .Where(j => j.Status == JobPostStatus.Open && j.CreatedAt >= since)
            .Select(j => j.Id)
            .ToListAsync(ct);

        _logger.LogInformation("JobAlertDispatch: {Count} new jobs to dispatch", newJobs.Count);

        foreach (var jobId in newJobs)
            await _alertService.TriggerAlertsForJobAsync(jobId, ct);
    }
}

// ?? Job 2: Cleanup expired OTPs and stale sessions ???????????????????????????

public class CleanupJob
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CleanupJob> _logger;

    public CleanupJob(ApplicationDbContext db, ILogger<CleanupJob> logger)
    {
        _db = db; _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        // Delete OTPs older than 1 day
        var otpCutoff = DateTime.UtcNow.AddDays(-1);
        var expiredOtps = await _db.OtpCodes
            .Where(o => o.ExpiresAt < otpCutoff)
            .ToListAsync(ct);
        _db.OtpCodes.RemoveRange(expiredOtps);

        // Expire active sessions older than 30 days
        var sessionCutoff = DateTime.UtcNow.AddDays(-30);
        var staleSessions = await _db.UserSessions
            .Where(s => s.Status == Core.Enums.SessionStatus.Active && s.LastActiveAt < sessionCutoff)
            .ToListAsync(ct);
        foreach (var s in staleSessions) s.Status = Core.Enums.SessionStatus.Expired;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Cleanup: removed {Otp} OTPs, expired {Session} sessions",
            expiredOtps.Count, staleSessions.Count);
    }
}

// ?? Job 3: Payroll Reminder for bookings ending today ????????????????????????

public class PayrollReminderJob
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly INotificationService _notify;
    private readonly ILogger<PayrollReminderJob> _logger;

    public PayrollReminderJob(ApplicationDbContext db, IEmailService email,
        INotificationService notify, ILogger<PayrollReminderJob> logger)
    {
        _db = db; _email = email; _notify = notify; _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var today   = DateTime.UtcNow.Date;
        var endings = await _db.Bookings
            .Include(b => b.Employer)
            .Include(b => b.Worker)
            .Where(b => b.Status == BookingStatus.InProgress
                && b.StartDate.AddDays(b.DurationDays).Date == today)
            .ToListAsync(ct);

        foreach (var booking in endings)
        {
            var msg = $"Reminder: Booking with {booking.Worker.FullName} ends today. " +
                      "Please approve payroll on ShramSetu.";

            if (!string.IsNullOrEmpty(booking.Employer.Email))
                await _email.SendAsync(booking.Employer.Email, booking.Employer.Name,
                    "? Payroll Reminder  ShramSetu", $"<p>{msg}</p>", ct);

            await _notify.SendAsync(booking.Employer.Phone, msg, NotificationChannel.SMS, ct);
        }

        _logger.LogInformation("PayrollReminder: notified {Count} employers", endings.Count);
    }
}

// ?? Job 4: SLA Breach Check on Open Disputes ?????????????????????????????????

public class SlaBreachJob
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<SlaBreachJob> _logger;

    public SlaBreachJob(ApplicationDbContext db, IEmailService email, ILogger<SlaBreachJob> logger)
    {
        _db = db; _email = email; _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var slaCutoff = DateTime.UtcNow.AddHours(-48);
        var breached  = await _db.Disputes
            .Where(d => d.Status == DisputeStatus.Open && d.CreatedAt < slaCutoff)
            .ToListAsync(ct);

        if (breached.Any())
        {
            var adminEmail = "admin@shramsetu.in";
            var body = $"<p><strong>{breached.Count} dispute(s)</strong> have exceeded the 48-hour SLA:</p><ul>" +
                string.Join("", breached.Select(d => $"<li>{d.Title} (raised {d.CreatedAt:dd MMM HH:mm})</li>")) +
                "</ul><p>Please review and resolve them in the Admin Dashboard.</p>";

            await _email.SendAsync(adminEmail, "ShramSetu Admin",
                $"?? {breached.Count} SLA Breach(es)  ShramSetu Admin Alert", body, ct);

            _logger.LogWarning("SlaBreachJob: {Count} disputes have breached 48h SLA", breached.Count);
        }
    }
}

// ?? Job 5: Weekly Job Digest for Workers ?????????????????????????????????????

public class WeeklyDigestJob
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<WeeklyDigestJob> _logger;

    public WeeklyDigestJob(ApplicationDbContext db, IEmailService email, ILogger<WeeklyDigestJob> logger)
    {
        _db = db; _email = email; _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var workers = await _db.Workers
            .Include(w => w.SkillCategory)
            .Where(w => !w.IsDeleted && w.IsAvailable && w.UserId != null)
            .ToListAsync(ct);

        int sent = 0;
        foreach (var worker in workers)
        {
            // Find matching open jobs in worker's city
            var jobs = await _db.JobPosts
                .Include(j => j.SkillCategory)
                .Where(j => j.Status == JobPostStatus.Open
                    && j.SkillCategoryId == worker.SkillCategoryId
                    && (j.LocationCity == null || j.LocationCity == worker.LocationCity))
                .Take(5)
                .ToListAsync(ct);

            if (!jobs.Any()) continue;

            // Get email from Identity
            var user = await _db.Users.FindAsync([worker.UserId], ct);
            if (user?.Email is null) continue;

            var jobItems = string.Join("", jobs.Select(j =>
                $"<li><strong>{j.Title}</strong>  {j.LocationCity}  ₹{j.DailyWage}/day</li>"));

            await _email.SendAsync(user.Email, worker.FullName,
                $"?? {jobs.Count} New Jobs Matching Your Skills  ShramSetu",
                $"<p>Hi {worker.FullName},</p><p>Here are new jobs matching your {worker.SkillCategory.Name} skills:</p><ul>{jobItems}</ul><p><a href='https://shramsetu.in/jobs'>View All Jobs ?</a></p>",
                ct);
            sent++;
        }

        _logger.LogInformation("WeeklyDigest: sent to {Count} workers", sent);
    }
}
