using System;
using System.Collections.Generic;
using System.Text;
using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByOrderIdAsync(Guid orderId);
    Task<Payment?> GetByIdAsync(Guid id);
    Task AddAsync(Payment payment);
    Task UpdateAsync(Payment payment);
    Task SaveChangesAsync();
}
