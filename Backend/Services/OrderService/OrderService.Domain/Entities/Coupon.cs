using System;

namespace OrderService.Domain.Entities;

public class Coupon
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CouponType Type { get; set; }  // Percentage or FixedAmount
    public decimal Value { get; set; }  // Percentage (10 = 10%) or Fixed Amount (50 = ₹50)
    public decimal MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }  // Cap for percentage discounts
    public int UsageLimit { get; set; }  // Total times coupon can be used
    public int UsedCount { get; set; } = 0;
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? RestaurantId { get; set; }  // Null = platform-wide, else restaurant-specific
    public Guid CreatedBy { get; set; }  // Admin or Partner user ID
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum CouponType
{
    Percentage = 0,
    FixedAmount = 1
}
