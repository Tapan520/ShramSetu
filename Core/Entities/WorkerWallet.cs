namespace ShramSetu.Core.Entities;

/// <summary>Digital earnings wallet for a worker.</summary>
public class WorkerWallet
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public decimal Balance { get; set; }
    public string? UpiId { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? IfscCode { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}
