using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

public interface IRefundRepository
{
    Task<RefundRequest?> GetByIdAsync(Guid id);
    Task<RefundRequest?> GetByOrderIdAsync(Guid orderId);
    Task<List<RefundRequest>> GetPendingRefundsAsync();
    Task<List<RefundRequest>> GetByCustomerIdAsync(Guid customerId);
    Task<List<RefundRequest>> GetByStatusAsync(RefundStatus status);
    Task AddAsync(RefundRequest refund);
    Task UpdateAsync(RefundRequest refund);
    Task SaveChangesAsync();
}
