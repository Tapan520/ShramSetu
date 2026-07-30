namespace ShramSetu.Services;

public interface IPushNotificationService
{
    Task SendAsync(string userId, string title, string body,
        IDictionary<string, string>? data = null, CancellationToken ct = default);

    Task SendToManyAsync(IEnumerable<string> userIds, string title, string body,
        IDictionary<string, string>? data = null, CancellationToken ct = default);
}
