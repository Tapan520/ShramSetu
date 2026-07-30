using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class WalletTransaction
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public WorkerWallet Wallet { get; set; } = null!;

    public WalletTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }

    public string Description { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }   // PayrollRecord ID, advance ID, UPI ref etc.
    public DateTime TransactedAt { get; set; } = DateTime.UtcNow;
}
