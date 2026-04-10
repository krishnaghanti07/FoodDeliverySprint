using System;
using System.Collections.Generic;
using System.Text;
namespace FoodDelivery.Shared.Events;

public class PaymentCompletedEvent
{
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}
