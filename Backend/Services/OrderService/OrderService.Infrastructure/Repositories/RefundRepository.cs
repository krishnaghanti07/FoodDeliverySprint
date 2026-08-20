using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

public class RefundRepository : IRefundRepository
{
    private readonly OrderDbContext _db;

    public RefundRepository(OrderDbContext db) => _db = db;

    public Task<RefundRequest?> GetByIdAsync(Guid id) =>
        _db.RefundRequests
            .Include(r => r.Order)
            .FirstOrDefaultAsync(r => r.Id == id);

    public Task<RefundRequest?> GetByOrderIdAsync(Guid orderId) =>
        _db.RefundRequests
            .Include(r => r.Order)
            .FirstOrDefaultAsync(r => r.OrderId == orderId);

    public Task<List<RefundRequest>> GetPendingRefundsAsync() =>
        _db.RefundRequests
            .Include(r => r.Order)
            .Where(r => r.Status == RefundStatus.PendingApproval)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();

    public Task<List<RefundRequest>> GetByCustomerIdAsync(Guid customerId) =>
        _db.RefundRequests
            .Include(r => r.Order)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public Task<List<RefundRequest>> GetByStatusAsync(RefundStatus status) =>
        _db.RefundRequests
            .Include(r => r.Order)
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public async Task AddAsync(RefundRequest refund)
    {
        await _db.RefundRequests.AddAsync(refund);
    }

    public Task UpdateAsync(RefundRequest refund)
    {
        _db.RefundRequests.Update(refund);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
