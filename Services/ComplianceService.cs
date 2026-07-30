using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Services;

public interface IComplianceService
{
    Task<List<ComplianceCheck>> CheckBookingAsync(Guid bookingId, CancellationToken ct = default);
}

public class ComplianceService : IComplianceService
{
    private readonly ApplicationDbContext _db;

    public ComplianceService(ApplicationDbContext db) => _db = db;

    public async Task<List<ComplianceCheck>> CheckBookingAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await _db.Bookings
            .Include(b => b.Worker).ThenInclude(w => w.SkillCategory)
            .Include(b => b.Employer)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

        if (booking is null) return [];

        var results = new List<ComplianceCheck>();

        // 1. Minimum Wage Check
        var minWage = await _db.MinimumWageConfigs
            .Where(m => m.State == booking.Worker.LocationState
                     && m.SkillCategoryId == booking.Worker.SkillCategoryId
                     && m.EffectiveFrom <= DateTime.UtcNow)
            .OrderByDescending(m => m.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (minWage is not null)
        {
            var wageStatus = booking.AgreedWage >= minWage.MinDailyWage
                ? ComplianceStatus.Compliant
                : ComplianceStatus.NonCompliant;

            results.Add(new ComplianceCheck
            {
                Id         = Guid.NewGuid(),
                BookingId  = bookingId,
                CheckType  = ComplianceCheckType.MinimumWage,
                Status     = wageStatus,
                Details    = $"Min wage for {booking.Worker.SkillCategory.Name} in {booking.Worker.LocationState}: ₹{minWage.MinDailyWage}/day. Agreed: ₹{booking.AgreedWage}/day.",
                Recommendation = wageStatus == ComplianceStatus.NonCompliant
                    ? $"Increase agreed wage to at least ₹{minWage.MinDailyWage}/day to comply with state minimum wage rules."
                    : null
            });
        }

        // 2. ESIC Eligibility (wage ? ₹21,000/month = eligible)
        var monthlyWage = booking.AgreedWage * 26; // approx
        var esicEligible = monthlyWage <= 21000;
        results.Add(new ComplianceCheck
        {
            Id        = Guid.NewGuid(),
            BookingId = bookingId,
            CheckType = ComplianceCheckType.ESIC,
            Status    = ComplianceStatus.Compliant,
            Details   = esicEligible
                ? $"Worker is ESIC eligible (monthly ? ₹{monthlyWage:N0} ? ₹21,000 threshold)."
                : $"Worker wage (? ₹{monthlyWage:N0}/mo) exceeds ESIC threshold. ESIC contribution not required.",
        });

        // 3. PF Eligibility (booking duration ? 20 days)
        var pfEligible = booking.DurationDays >= 20;
        results.Add(new ComplianceCheck
        {
            Id        = Guid.NewGuid(),
            BookingId = bookingId,
            CheckType = ComplianceCheckType.PF,
            Status    = pfEligible ? ComplianceStatus.Warning : ComplianceStatus.Compliant,
            Details   = pfEligible
                ? $"Booking duration ({booking.DurationDays} days) ? 20 days. Consider PF enrolment if employer has 20+ employees."
                : $"Short booking ({booking.DurationDays} days). PF typically not applicable.",
        });

        // Persist results
        var existing = _db.ComplianceChecks.Where(c => c.BookingId == bookingId);
        _db.ComplianceChecks.RemoveRange(existing);
        _db.ComplianceChecks.AddRange(results);
        await _db.SaveChangesAsync(ct);

        return results;
    }
}
