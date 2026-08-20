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
    public const string OrderReadyForPickup = "order.ready_for_pickup";
    public const string DeliveryStatusUpdated = "delivery.status_updated";
    public const string OrderRejected = "order.rejected";
    public const string OrderCancelled = "order.cancelled";

    // ── Per-service copies (fan-out pattern) ──────────────────────────
    // Each service gets its own queue so all consumers receive every event.
    // Publisher still publishes to the base queue name; each service
    // declares its own suffixed queue and binds to the same exchange.
    // For simplicity we use separate named queues published to directly.
    public const string OrderPlacedAdmin = "order.placed.admin";
    public const string OrderPlacedPayment = "order.placed.payment";
    public const string PaymentCompletedOrder = "payment.completed.order";
    public const string PaymentCompletedAdmin = "payment.completed.admin";
    public const string PaymentFailedOrder = "payment.failed.order";
}
