namespace AuthService.Domain.Entities;

public class WalletTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionSource Source { get; set; }
    public Guid? ReferenceId { get; set; } // OrderId or RefundRequestId
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public User User { get; set; } = null!;
}

public enum TransactionType
{
    Credit,
    Debit
}

public enum TransactionSource
{
    Refund,
    OrderPayment,
    AdminCredit,
    AdminDebit
}
