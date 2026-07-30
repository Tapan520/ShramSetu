using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Services;

public interface IJobAlertService
{
    Task TriggerAlertsForJobAsync(Guid jobPostId, CancellationToken ct = default);
}

public class JobAlertService : IJobAlertService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notify;
    private readonly IPushNotificationService? _push;
    private readonly ILogger<JobAlertService> _logger;

    public JobAlertService(
        ApplicationDbContext db,
        INotificationService notify,
        ILogger<JobAlertService> logger,
        IPushNotificationService? push = null)
    {
        _db     = db;
        _notify = notify;
        _push   = push;
        _logger = logger;
    }

    public async Task TriggerAlertsForJobAsync(Guid jobPostId, CancellationToken ct = default)
    {
        var job = await _db.JobPosts
            .Include(j => j.SkillCategory)
            .FirstOrDefaultAsync(j => j.Id == jobPostId, ct);

        if (job is null) return;

        // Find matching active alerts
        var alerts = await _db.JobAlerts
            .Include(a => a.Worker)
            .Where(a => a.IsActive
                && (!a.SkillCategoryId.HasValue || a.SkillCategoryId == job.SkillCategoryId)
                && (!a.MinWage.HasValue || job.DailyWage >= a.MinWage.Value)
                && (!a.MaxWage.HasValue || job.DailyWage <= a.MaxWage.Value)
                && (string.IsNullOrEmpty(a.City) ||
                    (job.LocationCity != null && job.LocationCity.Contains(a.City))))
            .ToListAsync(ct);

        _logger.LogInformation("Job {JobId} matched {Count} alerts", jobPostId, alerts.Count);

        foreach (var alert in alerts)
        {
            var worker = alert.Worker;
            if (worker.UserId is null) continue;

            var msg = $"New {job.SkillCategory.Name} job in {job.LocationCity ?? "your area"} " +
                      $"paying ₹{job.DailyWage}/day. Apply now on ShramSetu!";

            if (alert.SendSms)
                await _notify.SendAsync(worker.Phone, msg, NotificationChannel.SMS, ct);

            if (alert.SendPush && _push is not null)
                await _push.SendAsync(worker.UserId, "New Job Alert ??", msg,
                    new Dictionary<string, string> { ["jobPostId"] = jobPostId.ToString() }, ct);

            alert.LastTriggeredAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
