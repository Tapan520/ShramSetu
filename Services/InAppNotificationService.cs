using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;
using ShramSetu.Hubs;

namespace ShramSetu.Services;

public interface IInAppNotificationService
{
    Task SendAsync(string userId, string title, string message,
        string? actionUrl = null, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);
    Task MarkAllReadAsync(string userId, CancellationToken ct = default);
}

public class InAppNotificationService : IInAppNotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;

    public InAppNotificationService(ApplicationDbContext db, IHubContext<NotificationHub> hub)
    {
        _db  = db;
        _hub = hub;
    }

    public async Task SendAsync(string userId, string title, string message,
        string? actionUrl = null, CancellationToken ct = default)
    {
        // Persist to DB
        _db.Notifications.Add(new Notification
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            Title     = title,
            Message   = message,
            ActionUrl = actionUrl,
            IsRead    = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // Push real-time via SignalR
        await _hub.Clients.Group($"user_{userId}").SendAsync(
            "ReceiveNotification",
            new { title, message, actionUrl, at = DateTime.UtcNow },
            ct);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
        => await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task MarkAllReadAsync(string userId, CancellationToken ct = default)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }
}
