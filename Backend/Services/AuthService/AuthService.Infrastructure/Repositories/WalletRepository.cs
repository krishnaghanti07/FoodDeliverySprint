using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly AuthDbContext _context;

    public WalletRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<List<WalletTransaction>> GetTransactionsByUserIdAsync(Guid userId, int page, int pageSize)
    {
        return await _context.WalletTransactions
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task AddTransactionAsync(WalletTransaction transaction)
    {
        await _context.WalletTransactions.AddAsync(transaction);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
