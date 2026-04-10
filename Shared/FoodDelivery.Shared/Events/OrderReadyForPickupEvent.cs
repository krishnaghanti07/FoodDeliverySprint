using System;
using System.Collections.Generic;
using System.Text;
namespace FoodDelivery.Shared.Events;

public class OrderReadyForPickupEvent
{
    public Guid OrderId { get; set; }
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime ReadyAt { get; set; } = DateTime.UtcNow;
}
