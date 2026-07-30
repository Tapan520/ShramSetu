using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;
using ShramSetu.Services;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize(Roles = "Worker")]
[Produces("application/json")]
public class WalletController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWalletService _wallet;

    public WalletController(ApplicationDbContext db, IWalletService wallet)
    {
        _db = db;
        _wallet = wallet;
    }

    [HttpGet]
    public async Task<IActionResult> GetWallet()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return NotFound();

        var wallet = await _wallet.GetOrCreateWalletAsync(worker.Id);
        var txns = await _db.WalletTransactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.TransactedAt)
            .Take(30)
            .ToListAsync();

        return Ok(new { wallet.Balance, wallet.UpiId, wallet.BankAccountNumber, Transactions = txns });
    }

    [HttpPut("bank-details")]
    public async Task<IActionResult> UpdateBankDetails([FromBody] BankDetailsRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return NotFound();

        var wallet = await _wallet.GetOrCreateWalletAsync(worker.Id);
        wallet.UpiId             = req.UpiId;
        wallet.BankAccountNumber = req.BankAccountNumber;
        wallet.IfscCode          = req.IfscCode;
        wallet.UpdatedAt         = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    public record BankDetailsRequest(string? UpiId, string? BankAccountNumber, string? IfscCode);
}

[ApiController]
[Route("api/advances")]
[Authorize(Roles = "Employer,Admin")]
[Produces("application/json")]
public class SalaryAdvancesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWalletService _wallet;

    public SalaryAdvancesController(ApplicationDbContext db, IWalletService wallet)
    {
        _db = db;
        _wallet = wallet;
    }

    [HttpPost]
    public async Task<IActionResult> Issue([FromBody] IssueAdvanceRequest req)
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null) return BadRequest(new { message = "Employer profile not found." });

        var worker = await _db.Workers.FindAsync(req.WorkerId);
        if (worker is null) return NotFound(new { message = "Worker not found." });

        var advance = new SalaryAdvance
        {
            Id         = Guid.NewGuid(),
            WorkerId   = req.WorkerId,
            EmployerId = employer.Id,
            BookingId  = req.BookingId,
            Amount     = req.Amount,
            Notes      = req.Notes
        };

        _db.SalaryAdvances.Add(advance);

        // Credit worker's wallet
        await _wallet.CreditAsync(req.WorkerId, req.Amount,
            $"Salary advance from {employer.Name}", advance.Id.ToString());

        await _db.SaveChangesAsync();
        return Ok(new { advance.Id, advance.Amount, advance.Status });
    }

    [HttpPost("{id:guid}/repay")]
    public async Task<IActionResult> RecordRepayment(Guid id, [FromBody] RepaymentRequest req)
    {
        var advance = await _db.SalaryAdvances.FindAsync(id);
        if (advance is null) return NotFound();

        advance.AmountRepaid += req.Amount;
        if (advance.AmountRepaid >= advance.Amount)
        {
            advance.Status   = LoanStatus.Repaid;
            advance.RepaidAt = DateTime.UtcNow;
        }

        // Debit from wallet
        await _wallet.DebitAsync(advance.WorkerId, req.Amount,
            "Advance repayment", advance.Id.ToString());

        await _db.SaveChangesAsync();
        return Ok(new { advance.AmountRepaid, advance.AmountOutstanding, advance.Status });
    }

    public record IssueAdvanceRequest(Guid WorkerId, decimal Amount, Guid? BookingId, string? Notes);
    public record RepaymentRequest(decimal Amount);
}
