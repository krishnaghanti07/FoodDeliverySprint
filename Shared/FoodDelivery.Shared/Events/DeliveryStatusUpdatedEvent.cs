using System;
using System.Collections.Generic;
using System.Text;
namespace FoodDelivery.Shared.Events;

public class DeliveryStatusUpdatedEvent
{
    public Guid DeliveryAssignmentId { get; set; }
    public Guid OrderId { get; set; }
    public Guid AgentId { get; set; }
    public string NewStatus { get; set; } = string.Empty;  // PickedUp | OutForDelivery | Delivered
    public string? Note { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
