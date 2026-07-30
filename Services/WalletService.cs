using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Services;

public interface IWalletService
{
    Task<WorkerWallet> GetOrCreateWalletAsync(Guid workerId, CancellationToken ct = default);
    Task CreditAsync(Guid workerId, decimal amount, string description, string? referenceId = null, CancellationToken ct = default);
    Task DebitAsync(Guid workerId, decimal amount, string description, string? referenceId = null, CancellationToken ct = default);
}

public class WalletService : IWalletService
{
    private readonly ApplicationDbContext _db;

    public WalletService(ApplicationDbContext db) => _db = db;

    public async Task<WorkerWallet> GetOrCreateWalletAsync(Guid workerId, CancellationToken ct = default)
    {
        var wallet = await _db.WorkerWallets.FirstOrDefaultAsync(w => w.WorkerId == workerId, ct);
        if (wallet is not null) return wallet;

        wallet = new WorkerWallet { Id = Guid.NewGuid(), WorkerId = workerId, Balance = 0 };
        _db.WorkerWallets.Add(wallet);
        await _db.SaveChangesAsync(ct);
        return wallet;
    }

    public async Task CreditAsync(Guid workerId, decimal amount, string description,
        string? referenceId = null, CancellationToken ct = default)
    {
        var wallet = await GetOrCreateWalletAsync(workerId, ct);
        wallet.Balance  += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        _db.WalletTransactions.Add(new WalletTransaction
        {
            Id           = Guid.NewGuid(),
            WalletId     = wallet.Id,
            Type         = WalletTransactionType.Credit,
            Amount       = amount,
            BalanceAfter = wallet.Balance,
            Description  = description,
            ReferenceId  = referenceId
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task DebitAsync(Guid workerId, decimal amount, string description,
        string? referenceId = null, CancellationToken ct = default)
    {
        var wallet = await GetOrCreateWalletAsync(workerId, ct);
        if (wallet.Balance < amount)
            throw new InvalidOperationException("Insufficient wallet balance.");

        wallet.Balance  -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        _db.WalletTransactions.Add(new WalletTransaction
        {
            Id           = Guid.NewGuid(),
            WalletId     = wallet.Id,
            Type         = WalletTransactionType.Debit,
            Amount       = amount,
            BalanceAfter = wallet.Balance,
            Description  = description,
            ReferenceId  = referenceId
        });
        await _db.SaveChangesAsync(ct);
    }
}
