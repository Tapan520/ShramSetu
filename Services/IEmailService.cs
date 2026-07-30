namespace ShramSetu.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default);
    Task SendBookingConfirmationAsync(string toEmail, string toName, string workerName, string startDate, decimal wage, CancellationToken ct = default);
    Task SendPayslipAsync(string toEmail, string toName, byte[] payslipPdf, string period, CancellationToken ct = default);
    Task SendPasswordResetOtpAsync(string toEmail, string toName, string otp, CancellationToken ct = default);
    Task SendWelcomeAsync(string toEmail, string toName, string role, CancellationToken ct = default);
}
