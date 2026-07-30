using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ShramSetu.Services;

/// <summary>Production email service using MailKit (SMTP / SendGrid).</summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string toName, string subject,
        string htmlBody, CancellationToken ct = default)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _config["Email:SenderName"] ?? "ShramSetu",
                _config["Email:SenderEmail"] ?? "noreply@shramsetu.in"));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = WrapInTemplate(subject, htmlBody) };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _config["Email:SmtpHost"] ?? "smtp.sendgrid.net",
                int.Parse(_config["Email:SmtpPort"] ?? "587"),
                SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(
                _config["Email:SmtpUser"] ?? "apikey",
                _config["Email:SmtpPassword"] ?? "", ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            // Don't throw  email failure should not break main flow
        }
    }

    public Task SendBookingConfirmationAsync(string toEmail, string toName,
        string workerName, string startDate, decimal wage, CancellationToken ct = default)
        => SendAsync(toEmail, toName, "? Booking Confirmed  ShramSetu", $"""
            <h2>Booking Confirmed!</h2>
            <p>Dear {toName},</p>
            <p>Your booking with <strong>{workerName}</strong> has been confirmed.</p>
            <table style="border-collapse:collapse;width:100%">
              <tr><td style="padding:8px;border:1px solid #ddd"><strong>Start Date</strong></td><td style="padding:8px;border:1px solid #ddd">{startDate}</td></tr>
              <tr><td style="padding:8px;border:1px solid #ddd"><strong>Agreed Daily Wage</strong></td><td style="padding:8px;border:1px solid #ddd">₹{wage:N2}</td></tr>
            </table>
            <p style="margin-top:16px">Log in to ShramSetu to view full booking details.</p>
            """, ct);

    public async Task SendPayslipAsync(string toEmail, string toName,
        byte[] payslipPdf, string period, CancellationToken ct = default)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _config["Email:SenderName"] ?? "ShramSetu",
                _config["Email:SenderEmail"] ?? "noreply@shramsetu.in"));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = $"?? Your Payslip for {period}  ShramSetu";

            var builder = new BodyBuilder
            {
                HtmlBody = WrapInTemplate("Payslip", $"<p>Dear {toName},</p><p>Please find your payslip for <strong>{period}</strong> attached.</p>")
            };
            builder.Attachments.Add($"Payslip_{period}.pdf", payslipPdf, new ContentType("application", "pdf"));
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _config["Email:SmtpHost"] ?? "smtp.sendgrid.net",
                int.Parse(_config["Email:SmtpPort"] ?? "587"),
                SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(
                _config["Email:SmtpUser"] ?? "apikey",
                _config["Email:SmtpPassword"] ?? "", ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payslip to {Email}", toEmail);
        }
    }

    public Task SendPasswordResetOtpAsync(string toEmail, string toName,
        string otp, CancellationToken ct = default)
        => SendAsync(toEmail, toName, "?? Password Reset OTP  ShramSetu", $"""
            <h2>Password Reset Request</h2>
            <p>Dear {toName},</p>
            <p>Your one-time password (OTP) to reset your ShramSetu password is:</p>
            <div style="font-size:32px;font-weight:bold;letter-spacing:8px;
                        background:#f0f4ff;padding:16px;text-align:center;
                        border-radius:8px;color:#1a56db;margin:16px 0">{otp}</div>
            <p>This OTP is valid for <strong>10 minutes</strong>. Do not share it with anyone.</p>
            <p>If you did not request this, please ignore this email.</p>
            """, ct);

    public Task SendWelcomeAsync(string toEmail, string toName,
        string role, CancellationToken ct = default)
        => SendAsync(toEmail, toName, "?? Welcome to ShramSetu!", $"""
            <h2>Welcome to ShramSetu, {toName}!</h2>
            <p>You have successfully registered as a <strong>{role}</strong>.</p>
            <p>ShramSetu connects skilled workers with employers across India.</p>
            <ul>
              <li>Complete your profile to start receiving job offers</li>
              <li>Upload your KYC documents to get verified</li>
              <li>Set your availability to let employers find you</li>
            </ul>
            <p style="margin-top:16px">
              <a href="https://shramsetu.in/workers/onboarding"
                 style="background:#1a56db;color:white;padding:12px 24px;
                        border-radius:6px;text-decoration:none">
                Complete Your Profile ?
              </a>
            </p>
            """, ct);

    private static string WrapInTemplate(string title, string body) => $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="font-family:Arial,sans-serif;background:#f9fafb;margin:0;padding:0">
          <div style="max-width:600px;margin:32px auto;background:white;border-radius:12px;overflow:hidden;box-shadow:0 4px 12px rgba(0,0,0,0.08)">
            <div style="background:#1a56db;padding:24px 32px">
              <h1 style="color:white;margin:0;font-size:24px">??? ShramSetu</h1>
              <p style="color:#bfdbfe;margin:4px 0 0">Connecting Workers with Opportunity</p>
            </div>
            <div style="padding:32px">{body}</div>
            <div style="background:#f3f4f6;padding:16px 32px;text-align:center">
              <p style="color:#6b7280;font-size:12px;margin:0">
                 {DateTime.Now.Year} ShramSetu  <a href="https://shramsetu.in/privacy" style="color:#6b7280">Privacy Policy</a>
              </p>
            </div>
          </div>
        </body>
        </html>
        """;
}

/// <summary>Console stub used in development when SMTP is not configured.</summary>
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;
    public ConsoleEmailService(ILogger<ConsoleEmailService> logger) => _logger = logger;

    public Task SendAsync(string toEmail, string toName, string subject,
        string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL] To:{To} | Subject:{Subject}", toEmail, subject);
        return Task.CompletedTask;
    }

    public Task SendBookingConfirmationAsync(string toEmail, string toName,
        string workerName, string startDate, decimal wage, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL] BookingConfirm ? {Email} Worker:{Worker}", toEmail, workerName);
        return Task.CompletedTask;
    }

    public Task SendPayslipAsync(string toEmail, string toName,
        byte[] payslipPdf, string period, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL] Payslip ? {Email} Period:{Period}", toEmail, period);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetOtpAsync(string toEmail, string toName,
        string otp, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL] PasswordReset ? {Email} OTP:{Otp}", toEmail, otp);
        return Task.CompletedTask;
    }

    public Task SendWelcomeAsync(string toEmail, string toName,
        string role, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL] Welcome ? {Email} Role:{Role}", toEmail, role);
        return Task.CompletedTask;
    }
}
