using System;

namespace FoodDelivery.Shared.Events;

public class OrderRejectedEvent
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid RestaurantId { get; set; }
    public decimal TotalAmount { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public Guid? PaymentId { get; set; }
    public DateTime RejectedAt { get; set; } = DateTime.UtcNow;
}
