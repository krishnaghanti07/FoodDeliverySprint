using System;
using System.Collections.Generic;
using System.Text;
using OrderService.Application.DTOs;

namespace OrderService.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid customerId);
    Task<CartDto> AddItemAsync(Guid customerId, AddCartItemDto dto);
    Task<CartDto> UpdateItemAsync(Guid customerId, Guid cartItemId, UpdateCartItemDto dto);
    Task<CartDto> RemoveItemAsync(Guid customerId, Guid cartItemId);
    Task<CartDto> ApplyCouponAsync(Guid customerId, ApplyCouponDto dto);
    Task ClearCartAsync(Guid customerId);
    Task<CheckoutContextDto> GetCheckoutContextAsync(Guid customerId);
}