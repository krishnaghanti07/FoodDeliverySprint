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
    Task<List<OrderDto>> GetByRestaurantIdAsync(Guid restaurantId);
    Task<List<OrderDto>> GetAllAsync();
    Task<OrderDto> UpdateStatusAsync(Guid orderId, UpdateOrderStatusDto dto, string role);
}
