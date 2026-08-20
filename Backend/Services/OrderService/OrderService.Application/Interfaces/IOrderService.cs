using System;
using System.Collections.Generic;
using System.Text;
using OrderService.Application.DTOs;

namespace OrderService.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> PlaceOrderAsync(Guid customerId, PlaceOrderDto dto);
    Task<OrderDto> GetByIdAsync(Guid orderId, Guid requesterId, string role);
    Task<List<OrderDto>> GetMyOrdersAsync(Guid customerId);
    Task<PagedOrdersDto> SearchOrdersAsync(Guid customerId, OrderSearchDto search);
    Task<List<OrderDto>> GetByRestaurantIdAsync(Guid restaurantId);
    Task<List<OrderDto>> GetAllAsync();
    Task<OrderDto> UpdateStatusAsync(Guid orderId, UpdateOrderStatusDto dto, string role);
    
    // New methods for order management
    Task<OrderDto> RejectOrderAsync(Guid orderId, string rejectionReason, Guid partnerUserId);
    Task<bool> SoftDeleteOrderAsync(Guid orderId, Guid customerId);
    Task<ReorderResponseDto> ReorderAsync(Guid orderId, Guid customerId);
    Task<List<OrderDto>> GetMyOrdersFilteredAsync(Guid customerId, string? statusFilter);
    Task<List<OrderDto>> GetRestaurantOrdersFilteredAsync(Guid restaurantId, string? statusFilter);
    Task<OrderDto> CancelOrderAsync(Guid orderId, Guid customerId, string reason);
    Task<int> BackfillOrderNamesAsync();
}
