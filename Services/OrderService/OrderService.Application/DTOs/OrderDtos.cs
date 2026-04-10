using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Application.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string? DeliveryInstructions { get; set; }
    public string? CouponCode { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Discount { get; set; }
    public decimal GstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public PaymentSummaryDto? Payment { get; set; }
    public DeliveryAssignmentDto? DeliveryAssignment { get; set; }
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsVeg { get; set; }
}

public class PlaceOrderDto
{
    [Required] public string DeliveryAddress { get; set; } = string.Empty;
    [MaxLength(300)] public string? DeliveryInstructions { get; set; }
    [Required] public string PaymentMethod { get; set; } = string.Empty; // COD | Card | Wallet
}

public class UpdateOrderStatusDto
{
    [Required] public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class CheckoutContextDto
{
    public CartDto Cart { get; set; } = new();
    public decimal DeliveryFee { get; set; }
    public decimal GstRate { get; set; } = 5.0m;
    public decimal GstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<string> AvailablePaymentMethods { get; set; } = new() { "COD", "Card", "Wallet" };
}
