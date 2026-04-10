using System;
using System.Collections.Generic;
using System.Text;
namespace FoodDelivery.Shared.Events;

public class PaymentFailedEvent
{
    public Guid OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;
}
