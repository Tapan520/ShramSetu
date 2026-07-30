using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Services;

public interface IReferralService
{
    Task<Referral> GetOrCreateReferralCodeAsync(string userId, CancellationToken ct = default);
    Task<bool> ApplyReferralCodeAsync(string code, string newUserId, CancellationToken ct = default);
}

public class ReferralService : IReferralService
{
    private readonly ApplicationDbContext _db;
    private readonly IWalletService _wallet;

    public ReferralService(ApplicationDbContext db, IWalletService wallet)
    {
        _db     = db;
        _wallet = wallet;
    }

    public async Task<Referral> GetOrCreateReferralCodeAsync(string userId, CancellationToken ct = default)
    {
        var existing = await _db.Referrals
            .FirstOrDefaultAsync(r => r.ReferrerUserId == userId && r.ReferredUserId == null, ct);

        if (existing is not null) return existing;

        var code = GenerateCode(userId);
        var referral = new Referral
        {
            Id             = Guid.NewGuid(),
            ReferrerUserId = userId,
            Code           = code,
            RewardAmount   = 50  // ₹50 default reward
        };

        _db.Referrals.Add(referral);
        await _db.SaveChangesAsync(ct);
        return referral;
    }

    public async Task<bool> ApplyReferralCodeAsync(string code, string newUserId, CancellationToken ct = default)
    {
        var referral = await _db.Referrals
            .FirstOrDefaultAsync(r => r.Code == code
                && r.ReferredUserId == null
                && r.Status == ReferralStatus.Pending, ct);

        if (referral is null || referral.ReferrerUserId == newUserId) return false;

        referral.ReferredUserId = newUserId;
        referral.Status         = ReferralStatus.Verified;
        referral.VerifiedAt     = DateTime.UtcNow;

        // Credit referrer's wallet
        var worker = await _db.Workers
            .FirstOrDefaultAsync(w => w.UserId == referral.ReferrerUserId, ct);

        if (worker is not null)
        {
            await _wallet.CreditAsync(worker.Id, referral.RewardAmount,
                $"Referral reward for code {code}", referral.Id.ToString(), ct);
            referral.RewardCredited = true;
            referral.Status = ReferralStatus.Rewarded;
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static string GenerateCode(string userId)
    {
        var suffix = Math.Abs(userId.GetHashCode()) % 10000;
        return $"SS{suffix:D4}";
    }
}
