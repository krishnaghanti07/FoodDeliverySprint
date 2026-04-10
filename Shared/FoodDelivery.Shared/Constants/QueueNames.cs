using System;
using System.Collections.Generic;
using System.Text;
namespace FoodDelivery.Shared.Constants;

public static class QueueNames
{
    public const string OrderPlaced = "order.placed";
    public const string PaymentCompleted = "payment.completed";
    public const string PaymentFailed = "payment.failed";
    public const string UserRegistered = "user.registered";
    public const string OrderReadyForPickup = "order.ready_for_pickup";    // ← NEW
    public const string DeliveryStatusUpdated = "delivery.status_updated";   // ← NEW
}
