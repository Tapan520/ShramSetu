using ShramSetu.Core.Enums;
using ShramSetu.Data;
using ShramSetu.Core.Entities;

namespace ShramSetu.Services;

/// <summary>
/// Stub implementation  logs to console and persists to DB.
/// Replace with Twilio / MSG91 for production.
/// </summary>
public class ConsoleNotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ConsoleNotificationService> _logger;

    public ConsoleNotificationService(ApplicationDbContext db, ILogger<ConsoleNotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SendAsync(string recipient, string message, NotificationChannel channel, CancellationToken ct = default)
    {
        _logger.LogInformation("[{Channel}] ? {Recipient}: {Message}", channel, recipient, message);

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipient,
            Channel = channel,
            Message = message,
            IsSent = true,
            SentAt = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);
    }
}
