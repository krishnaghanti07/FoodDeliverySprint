using PaymentService.Domain.Entities;

namespace PaymentService.Domain.Interfaces;

public interface IPaymentTransactionRepository
{
    Task<PaymentTransaction?> GetByIdAsync(Guid id);
    Task<PaymentTransaction?> GetByOrderIdAsync(Guid orderId);
    Task<List<PaymentTransaction>> GetByCustomerIdAsync(Guid customerId);
    Task<List<PaymentTransaction>> GetAllAsync(string? status, DateTime? from, DateTime? to);
    Task AddAsync(PaymentTransaction txn);
    Task UpdateAsync(PaymentTransaction txn);
    Task SaveChangesAsync();
}

public interface IRazorpayOrderRepository
{
    Task<RazorpayOrder?> GetByOrderIdAsync(Guid orderId);
    Task AddAsync(RazorpayOrder order);
    Task UpdateAsync(RazorpayOrder order);
    Task SaveChangesAsync();
}