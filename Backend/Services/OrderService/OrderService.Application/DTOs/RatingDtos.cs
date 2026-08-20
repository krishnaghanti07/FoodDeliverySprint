using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Application.DTOs;

public class OrderRatingDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public int FoodRating { get; set; }
    public int DeliveryRating { get; set; }
    public double AverageRating { get; set; }
    public string? Comment { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> Photos { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateOrderRatingDto
{
    [Required, Range(1, 5)]
    public int FoodRating { get; set; }
    
    [Required, Range(1, 5)]
    public int DeliveryRating { get; set; }
    
    [MaxLength(1000)]
    public string? Comment { get; set; }
    
    public List<string> Tags { get; set; } = new();
    
    public List<string> Photos { get; set; } = new();
}

public class UpdateOrderRatingDto
{
    [Required, Range(1, 5)]
    public int FoodRating { get; set; }
    
    [Required, Range(1, 5)]
    public int DeliveryRating { get; set; }
    
    [MaxLength(1000)]
    public string? Comment { get; set; }
    
    public List<string> Tags { get; set; } = new();
    
    public List<string> Photos { get; set; } = new();
}

public class CancellationReasonDto
{
    public string Code { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class CancelOrderDto
{
    [Required]
    [System.Text.Json.Serialization.JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class CanCancelOrderDto
{
    public bool CanCancel { get; set; }
    public string? Reason { get; set; }
    public decimal? CancellationFee { get; set; }
    public decimal? RefundAmount { get; set; }
}

public class OrderSearchDto
{
    public string? OrderNumber { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? RestaurantId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedOrdersDto
{
    public List<OrderDto> Orders { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class RefundRequestDto
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("orderId")]
    public Guid OrderId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("customerId")]
    public Guid CustomerId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("customerEmail")]
    public string CustomerEmail { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("originalAmount")]
    public decimal OriginalAmount { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("platformFee")]
    public decimal PlatformFee { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("cancellationCharge")]
    public decimal CancellationCharge { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("refundAmount")]
    public decimal RefundAmount { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("adminNotes")]
    public string? AdminNotes { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("processedBy")]
    public Guid? ProcessedBy { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("requestedAt")]
    public DateTime RequestedAt { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("processedAt")]
    public DateTime? ProcessedAt { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("refundedAt")]
    public DateTime? RefundedAt { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("orderNumber")]
    public string? OrderNumber { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("restaurantName")]
    public string? RestaurantName { get; set; }
}

public class ProcessRefundDto
{
    [Required]
    [System.Text.Json.Serialization.JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty; // "Approve" or "Reject"
    
    [System.Text.Json.Serialization.JsonPropertyName("adminNotes")]
    public string? AdminNotes { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("processedBy")]
    public Guid? ProcessedBy { get; set; }
}

public class ApproveRefundDto
{
    [Required]
    [System.Text.Json.Serialization.JsonPropertyName("orderId")]
    public Guid OrderId { get; set; }
    
    [Required]
    [System.Text.Json.Serialization.JsonPropertyName("customerId")]
    public Guid CustomerId { get; set; }
    
    [Required]
    [System.Text.Json.Serialization.JsonPropertyName("originalAmount")]
    public decimal OriginalAmount { get; set; }
    
    [Required]
    [System.Text.Json.Serialization.JsonPropertyName("platformFee")]
    public decimal PlatformFee { get; set; }
    
    [Required]
    [System.Text.Json.Serialization.JsonPropertyName("cancellationCharge")]
    public decimal CancellationCharge { get; set; }
    
    [Required]
    [System.Text.Json.Serialization.JsonPropertyName("refundAmount")]
    public decimal RefundAmount { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("adminNotes")]
    public string? AdminNotes { get; set; }
}

public class RejectRefundDto
{
    [Required]
    [System.Text.Json.Serialization.JsonPropertyName("orderId")]
    public Guid OrderId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("adminNotes")]
    public string? AdminNotes { get; set; }
}
