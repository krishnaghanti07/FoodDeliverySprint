using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces;

public interface IWalletService
{
    Task<decimal> GetBalanceAsync(Guid userId);
    Task<List<WalletTransaction>> GetTransactionsAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<bool> AddCreditAsync(Guid userId, decimal amount, TransactionSource source, Guid? referenceId, string description);
    Task<bool> DebitWalletAsync(Guid userId, decimal amount, TransactionSource source, Guid? referenceId, string description);
    Task<bool> CreateSyncTransactionAsync(Guid userId, decimal amount, TransactionSource source, Guid? referenceId, string description);
}
