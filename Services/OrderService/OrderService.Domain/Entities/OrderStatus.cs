using System;
using System.Collections.Generic;
using System.Text;
namespace OrderService.Domain.Entities;

public enum OrderStatus
{
    DraftCart = 0,
    CheckoutStarted = 1,
    PaymentPending = 2,
    Paid = 3,
    Accepted = 4,
    Preparing = 5,
    ReadyForPickup = 6,
    PickedUp = 7,
    OutForDelivery = 8,
    Delivered = 9,
    PaymentFailed = 10,
    CancelRequested = 11,
    Cancelled = 12,
    RefundInitiated = 13,
    Refunded = 14,
    RestaurantRejected = 15
}

public static class OrderStatusTransitions
{
    private static readonly Dictionary<OrderStatus, (string[] roles, OrderStatus[] next)[]> Map = new()
    {
        [OrderStatus.DraftCart] = new[] { (new[] { "Customer" }, new[] { OrderStatus.CheckoutStarted }) },
        [OrderStatus.CheckoutStarted] = new[] { (new[] { "Customer" }, new[] { OrderStatus.PaymentPending }) },
        [OrderStatus.PaymentPending] = new[] { (new[] { "System" }, new[] { OrderStatus.Paid, OrderStatus.PaymentFailed }) },
        [OrderStatus.Paid] = new[]
        {
            (new[] { "Partner" }, new[] { OrderStatus.Accepted, OrderStatus.RestaurantRejected }),
            (new[] { "Customer" }, new[] { OrderStatus.CancelRequested }),
            (new[] { "Admin" },   new[] { OrderStatus.Cancelled })
        },
        [OrderStatus.Accepted] = new[]
        {
            (new[] { "Partner" }, new[] { OrderStatus.Preparing }),
            (new[] { "Admin" },   new[] { OrderStatus.Cancelled })
        },
        [OrderStatus.Preparing] = new[]
        {
            (new[] { "Partner" }, new[] { OrderStatus.ReadyForPickup }),
            (new[] { "Admin" },   new[] { OrderStatus.Cancelled })
        },
        [OrderStatus.ReadyForPickup] = new[]
        {
            (new[] { "DeliveryAgent" }, new[] { OrderStatus.PickedUp }),
            (new[] { "Admin" },         new[] { OrderStatus.Cancelled })
        },
        [OrderStatus.PickedUp] = new[] { (new[] { "DeliveryAgent" }, new[] { OrderStatus.OutForDelivery }) },
        [OrderStatus.OutForDelivery] = new[] { (new[] { "DeliveryAgent" }, new[] { OrderStatus.Delivered }) },
        [OrderStatus.CancelRequested] = new[]
        {
            (new[] { "Admin" }, new[] { OrderStatus.Cancelled, OrderStatus.RefundInitiated })
        },
        [OrderStatus.RefundInitiated] = new[] { (new[] { "Admin" }, new[] { OrderStatus.Refunded }) }
    };

    public static bool IsValid(OrderStatus current, OrderStatus next, string role)
    {
        if (!Map.TryGetValue(current, out var entries)) return false;
        return entries.Any(e => e.roles.Contains(role) && e.next.Contains(next));
    }
}