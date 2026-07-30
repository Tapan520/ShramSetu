using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class PayrollRecord
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public Guid EmployerId { get; set; }
    public EmployerAccount Employer { get; set; } = null!;

    /// <summary>Payroll period start.</summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>Payroll period end.</summary>
    public DateTime PeriodEnd { get; set; }

    public int DaysWorked { get; set; }
    public decimal DailyWage { get; set; }
    public decimal GrossAmount { get; set; }

    /// <summary>Any deductions (advances, penalties).</summary>
    public decimal Deductions { get; set; }
    public decimal NetAmount { get; set; }

    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;
    public string? PaymentReference { get; set; }
    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
