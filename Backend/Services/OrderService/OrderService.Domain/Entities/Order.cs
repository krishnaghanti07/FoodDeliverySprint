using System;
using System.Collections.Generic;
using System.Text;
namespace OrderService.Domain.Entities;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;   // Stored at order creation
    public string CustomerEmail { get; set; } = string.Empty;  // Stored at order creation
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string? DeliveryInstructions { get; set; }
    public string? CouponCode { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Discount { get; set; }
    public decimal GstAmount { get; set; }
    public decimal PlatformFee { get; set; } = 15.00m; // Fixed Rs. 15 platform fee
    public decimal RestaurantCommission { get; set; } // 15% of subtotal
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // COD | Card | Wallet
    public OrderStatus Status { get; set; } = OrderStatus.DraftCart;
    public string? CancellationReason { get; set; }
    public string? RejectionReason { get; set; }  // Reason when partner rejects order
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledBy { get; set; }  // UserId who cancelled
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeletedByCustomer { get; set; } = false;  // Soft delete for customer
    
    // New fields for improvements
    public int EstimatedPreparationMinutes { get; set; } = 30;
    public DateTime? EstimatedDeliveryTime { get; set; }
    public DateTime? ActualDeliveryTime { get; set; }
    public bool IsDelayed { get; set; } = false;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public Payment? Payment { get; set; }
    public DeliveryAssignment? DeliveryAssignment { get; set; }
    public OrderRating? Rating { get; set; }
}
