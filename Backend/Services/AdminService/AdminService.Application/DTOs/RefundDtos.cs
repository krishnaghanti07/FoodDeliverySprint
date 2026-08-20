using System.ComponentModel.DataAnnotations;

namespace AdminService.Application.DTOs;

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
    
    [System.Text.Json.Serialization.JsonPropertyName("orderCancellationReason")]
    public string? OrderCancellationReason { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("adminNotes")]
    public string? AdminNotes { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("requestedAt")]
    public DateTime RequestedAt { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("processedAt")]
    public DateTime? ProcessedAt { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("processedBy")]
    public Guid? ProcessedBy { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("orderNumber")]
    public string? OrderNumber { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("restaurantName")]
    public string? RestaurantName { get; set; }
}

public class ProcessRefundRequestDto
{
    [Required]
    [System.Text.Json.Serialization.JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty; // "Approve" or "Reject"
    
    [System.Text.Json.Serialization.JsonPropertyName("adminNotes")]
    public string? AdminNotes { get; set; }
}
