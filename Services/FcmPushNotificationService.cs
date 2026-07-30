using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Data;

namespace ShramSetu.Services;

/// <summary>
/// Sends push notifications via Firebase Cloud Messaging (FCM).
/// Requires Firebase:CredentialPath pointing to a service-account JSON file,
/// OR Firebase:CredentialJson containing the JSON string directly.
/// </summary>
public class FcmPushNotificationService : IPushNotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<FcmPushNotificationService> _logger;

    public FcmPushNotificationService(
        ApplicationDbContext db,
        ILogger<FcmPushNotificationService> logger,
        IConfiguration config)
    {
        _db = db;
        _logger = logger;

        if (FirebaseApp.DefaultInstance is null)
        {
            var credJson = config["Firebase:CredentialJson"];
            var credPath = config["Firebase:CredentialPath"];

            GoogleCredential cred = !string.IsNullOrWhiteSpace(credJson)
                ? GoogleCredential.FromJson(credJson)
                : GoogleCredential.FromFile(credPath
                    ?? throw new InvalidOperationException("Firebase credential not configured."));

            FirebaseApp.Create(new AppOptions { Credential = cred });
        }
    }

    public async Task SendAsync(string userId, string title, string body,
        IDictionary<string, string>? data = null, CancellationToken ct = default)
    {
        var tokens = await _db.PushTokens
            .Where(t => t.UserId == userId && t.IsActive)
            .Select(t => t.Token)
            .ToListAsync(ct);

        if (!tokens.Any()) return;

        await DispatchAsync(tokens, title, body, data, ct);
    }

    public async Task SendToManyAsync(IEnumerable<string> userIds, string title, string body,
        IDictionary<string, string>? data = null, CancellationToken ct = default)
    {
        var ids = userIds.ToList();
        var tokens = await _db.PushTokens
            .Where(t => ids.Contains(t.UserId) && t.IsActive)
            .Select(t => t.Token)
            .ToListAsync(ct);

        if (!tokens.Any()) return;

        await DispatchAsync(tokens, title, body, data, ct);
    }

    private async Task DispatchAsync(IList<string> tokens, string title, string body,
        IDictionary<string, string>? data, CancellationToken ct)
    {
        // FCM supports max 500 tokens per MulticastMessage
        foreach (var batch in tokens.Chunk(500))
        {
            var msg = new MulticastMessage
            {
                Tokens = batch.ToList(),
                Notification = new Notification { Title = title, Body = body },
                Data = data as IReadOnlyDictionary<string, string>
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(msg, ct);
                _logger.LogInformation("Push sent {Success}/{Total}", response.SuccessCount, batch.Length);

                // Deactivate tokens that are no longer valid
                for (int i = 0; i < response.Responses.Count; i++)
                {
                    if (!response.Responses[i].IsSuccess)
                    {
                        var badToken = batch[i];
                        var pt = await _db.PushTokens.FirstOrDefaultAsync(t => t.Token == badToken, ct);
                        if (pt is not null) pt.IsActive = false;
                    }
                }
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FCM dispatch failed");
            }
        }
    }
}
