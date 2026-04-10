using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _db;
    public OrderRepository(OrderDbContext db) => _db = db;

    public Task<Order?> GetByIdAsync(Guid id) =>
        _db.Orders.FirstOrDefaultAsync(o => o.Id == id);

    public Task<Order?> GetByIdWithDetailsAsync(Guid id) =>
        _db.Orders
           .Include(o => o.Items)
           .Include(o => o.Payment)
           .Include(o => o.DeliveryAssignment)
               .ThenInclude(d => d!.StatusHistory)
           .FirstOrDefaultAsync(o => o.Id == id);

    public Task<List<Order>> GetByCustomerIdAsync(Guid customerId) =>
        _db.Orders
           .Include(o => o.Items)
           .Include(o => o.Payment)
           .Where(o => o.CustomerId == customerId)
           .OrderByDescending(o => o.CreatedAt)
           .ToListAsync();

    public Task<List<Order>> GetByRestaurantIdAsync(Guid restaurantId) =>
        _db.Orders
           .Include(o => o.Items)
           .Where(o => o.RestaurantId == restaurantId)
           .OrderByDescending(o => o.CreatedAt)
           .ToListAsync();

    public Task<List<Order>> GetAllAsync() =>
        _db.Orders
           .Include(o => o.Items)
           .Include(o => o.Payment)
           .Include(o => o.DeliveryAssignment)
           .OrderByDescending(o => o.CreatedAt)
           .ToListAsync();

    public async Task AddAsync(Order order) => await _db.Orders.AddAsync(order);

    public Task UpdateAsync(Order order)
    {
        _db.Orders.Update(order);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
