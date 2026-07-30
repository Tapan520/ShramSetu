using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>Salary advance / micro-loan given to a worker by an employer.</summary>
public class SalaryAdvance
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public Guid EmployerId { get; set; }
    public EmployerAccount Employer { get; set; } = null!;

    public Guid? BookingId { get; set; }
    public Booking? Booking { get; set; }

    public decimal Amount { get; set; }
    public decimal AmountRepaid { get; set; }
    public decimal AmountOutstanding => Amount - AmountRepaid;

    public LoanStatus Status { get; set; } = LoanStatus.Active;
    public string? Notes { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RepaidAt { get; set; }
}
