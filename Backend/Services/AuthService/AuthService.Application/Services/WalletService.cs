using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Services;

public class WalletService : IWalletService
{
    private readonly IUserRepository _userRepo;
    private readonly IWalletRepository _walletRepo;

    public WalletService(IUserRepository userRepo, IWalletRepository walletRepo)
    {
        _userRepo = userRepo;
        _walletRepo = walletRepo;
    }

    public async Task<decimal> GetBalanceAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        return user?.WalletBalance ?? 0;
    }

    public async Task<List<WalletTransaction>> GetTransactionsAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        return await _walletRepo.GetTransactionsByUserIdAsync(userId, page, pageSize);
    }

    public async Task<bool> AddCreditAsync(Guid userId, decimal amount, TransactionSource source, Guid? referenceId, string description)
    {
        if (amount <= 0) return false;

        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return false;

        // Update user balance
        user.WalletBalance += amount;
        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();

        // Create transaction record
        var transaction = new WalletTransaction
        {
            UserId = userId,
            Amount = amount,
            Type = TransactionType.Credit,
            Source = source,
            ReferenceId = referenceId,
            Description = description
        };

        await _walletRepo.AddTransactionAsync(transaction);
        await _walletRepo.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DebitWalletAsync(Guid userId, decimal amount, TransactionSource source, Guid? referenceId, string description)
    {
        if (amount <= 0) return false;

        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return false;

        // Check sufficient balance
        if (user.WalletBalance < amount) return false;

        // Update user balance
        user.WalletBalance -= amount;
        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();

        // Create transaction record
        var transaction = new WalletTransaction
        {
            UserId = userId,
            Amount = amount,
            Type = TransactionType.Debit,
            Source = source,
            ReferenceId = referenceId,
            Description = description
        };

        await _walletRepo.AddTransactionAsync(transaction);
        await _walletRepo.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CreateSyncTransactionAsync(Guid userId, decimal amount, TransactionSource source, Guid? referenceId, string description)
    {
        if (amount <= 0) return false;

        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return false;

        // Create transaction record WITHOUT modifying the balance (for sync purposes)
        var transaction = new WalletTransaction
        {
            UserId = userId,
            Amount = amount,
            Type = TransactionType.Credit,
            Source = source,
            ReferenceId = referenceId,
            Description = description
        };

        await _walletRepo.AddTransactionAsync(transaction);
        await _walletRepo.SaveChangesAsync();

        return true;
    }
}
