using System;
using System.Collections.Generic;
using System.Text;
namespace OrderService.Application.Saga;

/// <summary>
/// Saga orchestrator contract for the Order placement flow.
/// Coordinates: Cart validation → Order creation → Payment → Confirmation.
/// On failure: compensates by cancelling the order and notifying via events.
/// </summary>
public interface IOrderSaga
{
    Task<OrderSagaResult> ExecuteAsync(OrderSagaRequest request);
}

public class OrderSagaRequest
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;   // Added for admin display
    public string CustomerEmail { get; set; } = string.Empty;  // Added for admin display
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string? DeliveryInstructions { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public List<SagaOrderItem> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Discount { get; set; }
    public decimal GstAmount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal RestaurantCommission { get; set; }
    public decimal TotalAmount { get; set; }
    public string? CouponCode { get; set; }
    public bool PaymentAlreadyCompleted { get; set; } = false;
}

public class SagaOrderItem
{
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsVeg { get; set; }
}

public class OrderSagaResult
{
    public bool Success { get; set; }
    public Guid? OrderId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? FailureStep { get; set; }
}