using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Interfaces;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Infrastructure.Repositories;

public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly PaymentDbContext _db;
    public PaymentTransactionRepository(PaymentDbContext db) => _db = db;

    public Task<PaymentTransaction?> GetByIdAsync(Guid id) =>
        _db.Transactions.FirstOrDefaultAsync(t => t.Id == id);

    public Task<PaymentTransaction?> GetByOrderIdAsync(Guid orderId) =>
        _db.Transactions.FirstOrDefaultAsync(t => t.OrderId == orderId);

    public Task<List<PaymentTransaction>> GetByCustomerIdAsync(Guid customerId) =>
        _db.Transactions
           .Where(t => t.CustomerId == customerId)
           .OrderByDescending(t => t.CreatedAt)
           .ToListAsync();

    public async Task<List<PaymentTransaction>> GetAllAsync(
        string? status, DateTime? from, DateTime? to)
    {
        var q = _db.Transactions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<PaymentStatus>(status, ignoreCase: true, out var ps))
                q = q.Where(t => t.Status == ps);
        }
        if (from.HasValue) q = q.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(t => t.CreatedAt <= to.Value);
        return await q.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task AddAsync(PaymentTransaction txn) =>
        await _db.Transactions.AddAsync(txn);

    public Task UpdateAsync(PaymentTransaction txn)
    {
        _db.Transactions.Update(txn);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}

public class RazorpayOrderRepository : IRazorpayOrderRepository
{
    private readonly PaymentDbContext _db;
    public RazorpayOrderRepository(PaymentDbContext db) => _db = db;

    public Task<RazorpayOrder?> GetByOrderIdAsync(Guid orderId) =>
        _db.RazorpayOrders.FirstOrDefaultAsync(r => r.OrderId == orderId);

    public async Task AddAsync(RazorpayOrder order) =>
        await _db.RazorpayOrders.AddAsync(order);

    public Task UpdateAsync(RazorpayOrder order)
    {
        _db.RazorpayOrders.Update(order);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}