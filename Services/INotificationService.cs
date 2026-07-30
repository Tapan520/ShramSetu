using ShramSetu.Core.Enums;

namespace ShramSetu.Services;

public interface INotificationService
{
    Task SendAsync(string recipient, string message, NotificationChannel channel, CancellationToken ct = default);
}
