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
           .Include(o => o.Rating)
           .FirstOrDefaultAsync(o => o.Id == id);

    public Task<List<Order>> GetByCustomerIdAsync(Guid customerId) =>
        _db.Orders
           .Include(o => o.Items)
           .Include(o => o.Payment)
           .Include(o => o.Rating)
           .Where(o => o.CustomerId == customerId)
           .OrderByDescending(o => o.CreatedAt)
           .ToListAsync();

    public Task<List<Order>> GetByRestaurantIdAsync(Guid restaurantId) =>
        _db.Orders
           .Include(o => o.Items)
           .Include(o => o.Payment)
           .Include(o => o.DeliveryAssignment)
           .Include(o => o.Rating)
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

    public async Task<(List<Order> orders, int totalCount)> SearchOrdersAsync(
        string? orderNumber, 
        string? status, 
        DateTime? fromDate, 
        DateTime? toDate, 
        Guid? restaurantId, 
        int page, 
        int pageSize)
    {
        var query = _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .Include(o => o.DeliveryAssignment)
            .Include(o => o.Rating)
            .AsQueryable();

        // Filter by order number (ID starts with)
        if (!string.IsNullOrWhiteSpace(orderNumber))
        {
            query = query.Where(o => o.Id.ToString().StartsWith(orderNumber));
        }

        // Filter by status
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, out var orderStatus))
        {
            query = query.Where(o => o.Status == orderStatus);
        }

        // Filter by date range
        if (fromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            var endDate = toDate.Value.AddDays(1); // Include entire day
            query = query.Where(o => o.CreatedAt < endDate);
        }

        // Filter by restaurant
        if (restaurantId.HasValue)
        {
            query = query.Where(o => o.RestaurantId == restaurantId.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (orders, totalCount);
    }

    public async Task AddAsync(Order order) => await _db.Orders.AddAsync(order);

    public Task UpdateAsync(Order order)
    {
        _db.Orders.Update(order);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public Task<List<Order>> GetOrdersReadyForPickupAsync() =>
        _db.Orders
           .Include(o => o.Items)
           .Include(o => o.DeliveryAssignment)
           .Where(o => o.Status == OrderStatus.ReadyForPickup && o.DeliveryAssignment == null)
           .OrderBy(o => o.CreatedAt)
           .ToListAsync();

    public async Task AddRefundRequestAsync(RefundRequest refundRequest) => 
        await _db.RefundRequests.AddAsync(refundRequest);
}
