using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;
using ShramSetu.Services;

namespace ShramSetu.Pages.Workers;

[Authorize(Roles = "Worker")]
public class WalletModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IWalletService _wallet;
    public WalletModel(ApplicationDbContext db, IWalletService wallet) { _db = db; _wallet = wallet; }

    public decimal Balance { get; set; }
    public IList<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();

    [BindProperty]
    public BankInput Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return;

        var wallet = await _wallet.GetOrCreateWalletAsync(worker.Id);
        Balance      = wallet.Balance;
        Input.UpiId  = wallet.UpiId;
        Input.BankAccountNumber = wallet.BankAccountNumber;
        Input.IfscCode = wallet.IfscCode;

        Transactions = await _db.WalletTransactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.TransactedAt)
            .Take(50)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return NotFound();

        var wallet = await _wallet.GetOrCreateWalletAsync(worker.Id);
        wallet.UpiId             = Input.UpiId;
        wallet.BankAccountNumber = Input.BankAccountNumber;
        wallet.IfscCode          = Input.IfscCode;
        wallet.UpdatedAt         = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Bank details saved.";
        return RedirectToPage();
    }

    public class BankInput
    {
        public string? UpiId { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? IfscCode { get; set; }
    }
}
