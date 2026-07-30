using ShramSetu.Core.Enums;
using ShramSetu.Data;
using ShramSetu.Core.Entities;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace ShramSetu.Services;

/// <summary>
/// Production SMS sender via Twilio.
/// Set Twilio:AccountSid, Twilio:AuthToken and Twilio:FromNumber in appsettings / secrets.
/// WhatsApp messages are sent to "whatsapp:+91XXXXXXXXXX"  set Twilio:FromWhatsApp in config.
/// </summary>
public class TwilioNotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<TwilioNotificationService> _logger;
    private readonly IConfiguration _config;

    public TwilioNotificationService(
        ApplicationDbContext db,
        ILogger<TwilioNotificationService> logger,
        IConfiguration config)
    {
        _db = db;
        _logger = logger;
        _config = config;

        TwilioClient.Init(
            _config["Twilio:AccountSid"] ?? throw new InvalidOperationException("Twilio:AccountSid not configured."),
            _config["Twilio:AuthToken"]  ?? throw new InvalidOperationException("Twilio:AuthToken not configured."));
    }

    public async Task SendAsync(string recipient, string message, NotificationChannel channel, CancellationToken ct = default)
    {
        try
        {
            var (from, to) = channel switch
            {
                NotificationChannel.WhatsApp =>
                    ($"whatsapp:{_config["Twilio:FromWhatsApp"]}",
                     $"whatsapp:{recipient}"),
                _ =>
                    (_config["Twilio:FromNumber"]!, recipient)
            };

            await MessageResource.CreateAsync(
                body: message,
                from: new Twilio.Types.PhoneNumber(from),
                to:   new Twilio.Types.PhoneNumber(to));

            _logger.LogInformation("[{Channel}] Sent to {Recipient}", channel, recipient);

            _db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = recipient,
                Channel = channel,
                Message = message,
                IsSent = true,
                SentAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Channel}] Failed to send to {Recipient}", channel, recipient);

            _db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = recipient,
                Channel = channel,
                Message = message,
                IsSent = false,
                FailureReason = ex.Message
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
