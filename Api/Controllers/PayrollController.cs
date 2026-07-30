using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Api.Dtos;
using ShramSetu.Data;
using ShramSetu.Services;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/payroll")]
[Authorize(Roles = "Employer,Admin")]
[Produces("application/json")]
public class PayrollController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPayrollService _payroll;

    public PayrollController(ApplicationDbContext db, IPayrollService payroll)
    {
        _db = db;
        _payroll = payroll;
    }

    /// <summary>Generate a payroll record from attendance data for a given period.</summary>
    [HttpPost("generate")]
    public async Task<ActionResult<PayrollDto>> Generate([FromBody] GeneratePayrollRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var record = await _payroll.GenerateAsync(req.BookingId, req.PeriodStart, req.PeriodEnd);
        await _db.Entry(record).Reference(r => r.Worker).LoadAsync();
        await _db.Entry(record).Reference(r => r.Employer).LoadAsync();
        return Ok(ToDto(record));
    }

    /// <summary>Get payroll records for a booking.</summary>
    [HttpGet("booking/{bookingId:guid}")]
    public async Task<ActionResult<IList<PayrollDto>>> GetByBooking(Guid bookingId)
    {
        var records = await _db.PayrollRecords
            .Include(r => r.Worker)
            .Include(r => r.Employer)
            .Where(r => r.BookingId == bookingId)
            .OrderByDescending(r => r.PeriodStart)
            .ToListAsync();
        return Ok(records.Select(ToDto));
    }

    /// <summary>Approve a payroll record.</summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<PayrollDto>> Approve(Guid id)
    {
        var record = await _payroll.ApproveAsync(id);
        await _db.Entry(record).Reference(r => r.Worker).LoadAsync();
        await _db.Entry(record).Reference(r => r.Employer).LoadAsync();
        return Ok(ToDto(record));
    }

    /// <summary>Mark payroll as paid.</summary>
    [HttpPost("{id:guid}/mark-paid")]
    public async Task<ActionResult<PayrollDto>> MarkPaid(Guid id, [FromQuery] string paymentReference)
    {
        var record = await _payroll.MarkPaidAsync(id, paymentReference);
        await _db.Entry(record).Reference(r => r.Worker).LoadAsync();
        await _db.Entry(record).Reference(r => r.Employer).LoadAsync();
        return Ok(ToDto(record));
    }

    private static PayrollDto ToDto(Core.Entities.PayrollRecord r) => new(
        r.Id, r.BookingId, r.Worker.FullName, r.Employer.Name,
        r.PeriodStart, r.PeriodEnd, r.DaysWorked, r.DailyWage,
        r.GrossAmount, r.Deductions, r.NetAmount,
        r.Status.ToString(), r.PaymentReference, r.PaidAt);
}
