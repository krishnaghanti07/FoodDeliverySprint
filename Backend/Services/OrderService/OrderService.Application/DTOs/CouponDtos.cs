using System;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Application.DTOs;

public class CouponDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public int RemainingUses { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public Guid? RestaurantId { get; set; }
    public string Scope { get; set; } = string.Empty;  // "Platform-wide" or "Restaurant-specific"
    public DateTime CreatedAt { get; set; }
}

public class CreateCouponDto
{
    [Required, StringLength(20, MinimumLength = 3)]
    [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Code must be uppercase letters and numbers only")]
    public string Code { get; set; } = string.Empty;
    
    [Required, StringLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string Type { get; set; } = string.Empty;  // "Percentage" or "FixedAmount"
    
    [Required, Range(0.01, 100)]
    public decimal Value { get; set; }
    
    [Required, Range(0, 10000)]
    public decimal MinOrderAmount { get; set; }
    
    [Range(0, 10000)]
    public decimal? MaxDiscountAmount { get; set; }
    
    [Required, Range(1, 100000)]
    public int UsageLimit { get; set; }
    
    [Required]
    public DateTime ValidFrom { get; set; }
    
    [Required]
    public DateTime ValidUntil { get; set; }
    
    public Guid? RestaurantId { get; set; }
}

public class UpdateCouponDto
{
    [Required, StringLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [Required, Range(0.01, 100)]
    public decimal Value { get; set; }
    
    [Required, Range(0, 10000)]
    public decimal MinOrderAmount { get; set; }
    
    [Range(0, 10000)]
    public decimal? MaxDiscountAmount { get; set; }
    
    [Required, Range(1, 100000)]
    public int UsageLimit { get; set; }
    
    [Required]
    public DateTime ValidUntil { get; set; }
    
    [Required]
    public bool IsActive { get; set; }
}

public class ValidateCouponDto
{
    [Required]
    public string CouponCode { get; set; } = string.Empty;
    
    [Required, Range(0.01, 100000)]
    public decimal OrderAmount { get; set; }
    
    public Guid? RestaurantId { get; set; }
}

public class CouponValidationResultDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal DiscountAmount { get; set; }
    public CouponDto? Coupon { get; set; }
}
