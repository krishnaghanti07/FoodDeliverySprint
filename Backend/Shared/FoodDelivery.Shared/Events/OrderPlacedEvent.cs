using System;
using System.Collections.Generic;
using System.Text;
namespace FoodDelivery.Shared.Events;

public class OrderPlacedEvent
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid RestaurantId { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
}

public class OrderItemDto
{
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
