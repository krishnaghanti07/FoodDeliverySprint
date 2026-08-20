using System;

namespace FoodDelivery.Shared.Events;

public class OrderCancelledEvent
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid RestaurantId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool RefundRequired { get; set; }
    public Guid? RefundRequestId { get; set; }
    public DateTime CancelledAt { get; set; } = DateTime.UtcNow;
}
