namespace ShramSetu.Services;

/// <summary>
/// No-op push notification service used when Firebase credentials are not configured.
/// Logs a warning instead of throwing so the application starts cleanly in development.
/// </summary>
public class NoOpPushNotificationService : IPushNotificationService
{
    private readonly ILogger<NoOpPushNotificationService> _logger;

    public NoOpPushNotificationService(ILogger<NoOpPushNotificationService> logger)
        => _logger = logger;

    public Task SendAsync(string userId, string title, string body,
        IDictionary<string, string>? data = null, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "Push notifications are disabled (no Firebase credentials). Skipped: [{Title}] for user {UserId}",
            title, userId);
        return Task.CompletedTask;
    }

    public Task SendToManyAsync(IEnumerable<string> userIds, string title, string body,
        IDictionary<string, string>? data = null, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "Push notifications are disabled (no Firebase credentials). Skipped: [{Title}] for {Count} user(s)",
            title, userIds.Count());
        return Task.CompletedTask;
    }
}
