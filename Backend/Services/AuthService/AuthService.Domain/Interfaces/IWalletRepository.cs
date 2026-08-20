using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;

public interface IWalletRepository
{
    Task<List<WalletTransaction>> GetTransactionsByUserIdAsync(Guid userId, int page, int pageSize);
    Task AddTransactionAsync(WalletTransaction transaction);
    Task SaveChangesAsync();
}
