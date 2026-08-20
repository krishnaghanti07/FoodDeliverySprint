using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly OrderDbContext _db;
    public PaymentRepository(OrderDbContext db) => _db = db;

    public Task<Payment?> GetByOrderIdAsync(Guid orderId) =>
        _db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);

    public Task<Payment?> GetByIdAsync(Guid id) =>
        _db.Payments.FindAsync(id).AsTask();

    public async Task AddAsync(Payment payment) => await _db.Payments.AddAsync(payment);

    public Task UpdateAsync(Payment payment)
    {
        _db.Payments.Update(payment);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}