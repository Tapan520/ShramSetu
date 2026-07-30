using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Services;

public class OtpService : IOtpService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notify;
    private readonly ILogger<OtpService> _logger;

    // OTP valid for 10 minutes; lock out after 5 bad attempts
    private static readonly TimeSpan Expiry      = TimeSpan.FromMinutes(10);
    private const int MaxAttempts = 5;

    public OtpService(ApplicationDbContext db, INotificationService notify, ILogger<OtpService> logger)
    {
        _db = db;
        _notify = notify;
        _logger = logger;
    }

    public async Task<string> SendOtpAsync(string phone, CancellationToken ct = default)
    {
        // Invalidate any existing active OTPs for this phone
        var existing = await _db.OtpCodes
            .Where(o => o.Phone == phone && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);
        foreach (var old in existing) old.IsUsed = true;

        // Generate secure 6-digit code
        var code = GenerateCode();
        var hash = HashCode(code);

        _db.OtpCodes.Add(new OtpCode
        {
            Id        = Guid.NewGuid(),
            Phone     = phone,
            CodeHash  = hash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(Expiry)
        });

        await _db.SaveChangesAsync(ct);

        // Send via SMS (or log in dev)
        await _notify.SendAsync(phone,
            $"Your ShramSetu OTP is {code}. Valid for 10 minutes. Do not share it.",
            NotificationChannel.SMS, ct);

        // In dev, return the plaintext code so Swagger/Postman can test without real SMS
        return code;
    }

    public async Task<bool> VerifyOtpAsync(string phone, string code, CancellationToken ct = default)
    {
        var otp = await _db.OtpCodes
            .Where(o => o.Phone == phone && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (otp is null)
        {
            _logger.LogWarning("OTP verify failed  no active OTP for {Phone}", phone);
            return false;
        }

        if (otp.Attempts >= MaxAttempts)
        {
            _logger.LogWarning("OTP locked out for {Phone}", phone);
            return false;
        }

        var hash = HashCode(code);
        if (!string.Equals(otp.CodeHash, hash, StringComparison.Ordinal))
        {
            otp.Attempts++;
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning("OTP mismatch for {Phone}. Attempt {N}", phone, otp.Attempts);
            return false;
        }

        otp.IsUsed = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ?? Helpers ???????????????????????????????????????????????????????????????

    private static string GenerateCode()
    {
        // Cryptographically random 6-digit code (000000999999)
        var bytes = RandomNumberGenerator.GetBytes(4);
        var num   = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
        return num.ToString("D6");
    }

    private static string HashCode(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }
}
