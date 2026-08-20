using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

public interface IRatingRepository
{
    Task<OrderRating?> GetByIdAsync(Guid id);
    Task<OrderRating?> GetByOrderIdAsync(Guid orderId);
    Task<List<OrderRating>> GetByCustomerIdAsync(Guid customerId);
    Task AddAsync(OrderRating rating);
    Task UpdateAsync(OrderRating rating);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}
