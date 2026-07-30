using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Services;

public interface IPayrollService
{
    Task<PayrollRecord> GenerateAsync(Guid bookingId, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);
    Task<PayrollRecord> ApproveAsync(Guid payrollId, CancellationToken ct = default);
    Task<PayrollRecord> MarkPaidAsync(Guid payrollId, string paymentReference, CancellationToken ct = default);
}

public class PayrollService : IPayrollService
{
    private readonly ApplicationDbContext _db;

    public PayrollService(ApplicationDbContext db) => _db = db;

    public async Task<PayrollRecord> GenerateAsync(Guid bookingId, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default)
    {
        var booking = await _db.Bookings
            .Include(b => b.Worker)
            .Include(b => b.Employer)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new InvalidOperationException("Booking not found.");

        // Count present/half-day attendance in the period
        var attendance = await _db.AttendanceRecords
            .Where(a => a.BookingId == bookingId && a.Date >= periodStart && a.Date <= periodEnd)
            .ToListAsync(ct);

        var daysWorked = attendance.Count(a => a.Status == AttendanceStatus.Present)
                       + attendance.Count(a => a.Status == AttendanceStatus.HalfDay) * 0.5m;

        var gross     = daysWorked * booking.AgreedWage;
        var existing  = await _db.PayrollRecords
            .FirstOrDefaultAsync(p => p.BookingId == bookingId
                && p.PeriodStart == periodStart && p.PeriodEnd == periodEnd, ct);

        if (existing is not null)
        {
            existing.DaysWorked  = (int)daysWorked;
            existing.GrossAmount = gross;
            existing.NetAmount   = gross - existing.Deductions;
            await _db.SaveChangesAsync(ct);
            return existing;
        }

        var record = new PayrollRecord
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            WorkerId  = booking.WorkerId,
            EmployerId = booking.EmployerId,
            PeriodStart = periodStart,
            PeriodEnd   = periodEnd,
            DaysWorked  = (int)daysWorked,
            DailyWage   = booking.AgreedWage,
            GrossAmount = gross,
            Deductions  = 0,
            NetAmount   = gross,
            Status      = PayrollStatus.Draft
        };

        _db.PayrollRecords.Add(record);
        await _db.SaveChangesAsync(ct);
        return record;
    }

    public async Task<PayrollRecord> ApproveAsync(Guid payrollId, CancellationToken ct = default)
    {
        var record = await _db.PayrollRecords.FindAsync([payrollId], ct)
            ?? throw new InvalidOperationException("Payroll record not found.");
        record.Status = PayrollStatus.Approved;
        await _db.SaveChangesAsync(ct);
        return record;
    }

    public async Task<PayrollRecord> MarkPaidAsync(Guid payrollId, string paymentReference, CancellationToken ct = default)
    {
        var record = await _db.PayrollRecords.FindAsync([payrollId], ct)
            ?? throw new InvalidOperationException("Payroll record not found.");
        record.Status = PayrollStatus.Paid;
        record.PaymentReference = paymentReference;
        record.PaidAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return record;
    }
}
