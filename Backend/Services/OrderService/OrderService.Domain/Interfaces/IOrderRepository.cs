using System;
using System.Collections.Generic;
using System.Text;
using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetByIdWithDetailsAsync(Guid id);
    Task<List<Order>> GetByCustomerIdAsync(Guid customerId);
    Task<List<Order>> GetByRestaurantIdAsync(Guid restaurantId);
    Task<List<Order>> GetAllAsync();
    Task<(List<Order> orders, int totalCount)> SearchOrdersAsync(
        string? orderNumber, 
        string? status, 
        DateTime? fromDate, 
        DateTime? toDate, 
        Guid? restaurantId, 
        int page, 
        int pageSize);
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task SaveChangesAsync();
    
    // New method for available orders
    Task<List<Order>> GetOrdersReadyForPickupAsync();
    
    // Refund request methods
    Task AddRefundRequestAsync(RefundRequest refundRequest);
}
