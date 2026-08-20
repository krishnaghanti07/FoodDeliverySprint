using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Application.DTOs;

public class CartDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public string? CouponCode { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
}

public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsVeg { get; set; }
}

public class AddCartItemDto
{
    [Required] public Guid MenuItemId { get; set; }
    [Required] public Guid RestaurantId { get; set; }
    [Required] public string ItemName { get; set; } = string.Empty;
    [Required, Range(0.01, 99999)] public decimal UnitPrice { get; set; }
    [Required, Range(1, 20)] public int Quantity { get; set; } = 1;
    public bool IsVeg { get; set; }
}

public class UpdateCartItemDto
{
    [Required, Range(0, 20)] public int Quantity { get; set; }
}

public class ApplyCouponDto
{
    [Required] public string CouponCode { get; set; } = string.Empty;
}
