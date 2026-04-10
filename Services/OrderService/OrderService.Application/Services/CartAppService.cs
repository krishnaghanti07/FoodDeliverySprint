using System;
using System.Collections.Generic;
using System.Text;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Services;

public class CartAppService : ICartService
{
    private readonly ICartRepository _cartRepo;
    private const decimal GstRate = 0.05m;  // 5% GST
    private const decimal DeliveryFee = 30.00m; // flat ₹30

    public CartAppService(ICartRepository cartRepo) => _cartRepo = cartRepo;

    public async Task<CartDto> GetCartAsync(Guid customerId)
    {
        var cart = await _cartRepo.GetByCustomerIdAsync(customerId)
                   ?? new Cart { CustomerId = customerId };
        return MapToDto(cart);
    }

    public async Task<CartDto> AddItemAsync(Guid customerId, AddCartItemDto dto)
    {
        var cart = await _cartRepo.GetByCustomerIdAsync(customerId);

        if (cart is null)
        {
            cart = new Cart
            {
                CustomerId = customerId,
                RestaurantId = dto.RestaurantId,
                RestaurantName = string.Empty   // enriched later via Catalog
            };
            await _cartRepo.AddAsync(cart);
        }
        else
        {
            // PRD rule: mixed-cart (different restaurant) is blocked
            if (cart.RestaurantId.HasValue && cart.RestaurantId != dto.RestaurantId)
                throw new InvalidOperationException(
                    "Cart already contains items from another restaurant. Clear cart first.");
            cart.RestaurantId = dto.RestaurantId;
        }

        var existing = cart.Items.FirstOrDefault(i => i.MenuItemId == dto.MenuItemId);
        if (existing is not null)
            existing.Quantity += dto.Quantity;
        else
            cart.Items.Add(new CartItem
            {
                MenuItemId = dto.MenuItemId,
                Name = dto.ItemName,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice,
                IsVeg = dto.IsVeg,
                RestaurantId = dto.RestaurantId
            });

        cart.UpdatedAt = DateTime.UtcNow;
        await _cartRepo.UpdateAsync(cart);
        await _cartRepo.SaveChangesAsync();
        return MapToDto(cart);
    }

    public async Task<CartDto> UpdateItemAsync(Guid customerId, Guid cartItemId, UpdateCartItemDto dto)
    {
        var cart = await _cartRepo.GetByCustomerIdAsync(customerId)
            ?? throw new KeyNotFoundException("Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId)
            ?? throw new KeyNotFoundException("Cart item not found.");

        if (dto.Quantity == 0)
            cart.Items.Remove(item);
        else
            item.Quantity = dto.Quantity;

        if (!cart.Items.Any()) cart.RestaurantId = null;

        cart.UpdatedAt = DateTime.UtcNow;
        await _cartRepo.UpdateAsync(cart);
        await _cartRepo.SaveChangesAsync();
        return MapToDto(cart);
    }

    public async Task<CartDto> RemoveItemAsync(Guid customerId, Guid cartItemId)
    {
        var cart = await _cartRepo.GetByCustomerIdAsync(customerId)
            ?? throw new KeyNotFoundException("Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId)
            ?? throw new KeyNotFoundException("Cart item not found.");

        cart.Items.Remove(item);
        if (!cart.Items.Any()) cart.RestaurantId = null;

        cart.UpdatedAt = DateTime.UtcNow;
        await _cartRepo.UpdateAsync(cart);
        await _cartRepo.SaveChangesAsync();
        return MapToDto(cart);
    }

    public async Task<CartDto> ApplyCouponAsync(Guid customerId, ApplyCouponDto dto)
    {
        var cart = await _cartRepo.GetByCustomerIdAsync(customerId)
            ?? throw new KeyNotFoundException("Cart not found.");

        if (!cart.Items.Any())
            throw new InvalidOperationException("Cart is empty. Add items before applying a coupon.");

        // Simulated coupon validation (real: query CouponService or DB)
        var (valid, discount) = ValidateCoupon(dto.CouponCode, GetSubtotal(cart));
        if (!valid)
            throw new InvalidOperationException(
                $"Coupon '{dto.CouponCode}' is invalid or does not meet the minimum order requirement.");

        cart.CouponCode = dto.CouponCode.ToUpperInvariant();
        cart.Discount = discount;
        cart.UpdatedAt = DateTime.UtcNow;

        await _cartRepo.UpdateAsync(cart);
        await _cartRepo.SaveChangesAsync();
        return MapToDto(cart);
    }

    public async Task ClearCartAsync(Guid customerId)
    {
        await _cartRepo.DeleteAsync(customerId);
        await _cartRepo.SaveChangesAsync();
    }

    public async Task<CheckoutContextDto> GetCheckoutContextAsync(Guid customerId)
    {
        var cart = await _cartRepo.GetByCustomerIdAsync(customerId)
            ?? throw new KeyNotFoundException("Cart not found or empty.");

        if (!cart.Items.Any())
            throw new InvalidOperationException("Cart is empty.");

        var subtotal = GetSubtotal(cart);
        var gst = Math.Round(subtotal * GstRate, 2);
        var total = subtotal + DeliveryFee + gst - cart.Discount;

        return new CheckoutContextDto
        {
            Cart = MapToDto(cart),
            DeliveryFee = DeliveryFee,
            GstRate = GstRate * 100,
            GstAmount = gst,
            TotalAmount = total
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static decimal GetSubtotal(Cart cart) =>
        cart.Items.Sum(i => i.UnitPrice * i.Quantity);

    private static (bool valid, decimal discount) ValidateCoupon(string code, decimal subtotal)
    {
        // Simulated coupon table — replace with DB lookup in production
        return code.ToUpperInvariant() switch
        {
            "WELCOME10" when subtotal >= 100 => (true, Math.Round(subtotal * 0.10m, 2)),
            "FLAT50" when subtotal >= 200 => (true, 50m),
            "SAVE20" when subtotal >= 150 => (true, Math.Round(subtotal * 0.20m, 2)),
            _ => (false, 0m)
        };
    }

    private static CartDto MapToDto(Cart cart)
    {
        var subtotal = GetSubtotal(cart);
        var total = subtotal - cart.Discount;
        return new CartDto
        {
            Id = cart.Id,
            CustomerId = cart.CustomerId,
            RestaurantId = cart.RestaurantId,
            RestaurantName = cart.RestaurantName,
            CouponCode = cart.CouponCode,
            Discount = cart.Discount,
            Subtotal = subtotal,
            Total = total,
            ItemCount = cart.Items.Sum(i => i.Quantity),
            Items = cart.Items.Select(i => new CartItemDto
            {
                Id = i.Id,
                MenuItemId = i.MenuItemId,
                Name = i.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.UnitPrice * i.Quantity,
                IsVeg = i.IsVeg
            }).ToList()
        };
    }
}