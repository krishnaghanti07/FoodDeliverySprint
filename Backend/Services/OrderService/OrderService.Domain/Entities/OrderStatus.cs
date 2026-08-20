using System;
using System.Collections.Generic;
using System.Text;
namespace OrderService.Domain.Entities;

public enum OrderStatus
{
    DraftCart = 0,
    CheckoutStarted = 1,
    PaymentPending = 2,          // For online payments awaiting confirmation
    Paid = 3,                     // Online payment successful
    AwaitingAcceptance = 4,       // COD orders - payment on delivery, awaiting restaurant acceptance
    Accepted = 5,
    Preparing = 6,
    ReadyForPickup = 7,
    PickedUp = 8,
    OutForDelivery = 9,
    Delivered = 10,
    PaymentFailed = 11,
    CancelRequested = 12,
    Cancelled = 13,
    RefundInitiated = 14,
    Refunded = 15,
    RestaurantRejected = 16,
    RefundRejected = 17           // Admin rejected refund - platform keeps fees
}

public static class OrderStatusTransitions
{
    private static readonly Dictionary<OrderStatus, (string[] roles, OrderStatus[] next)[]> Map = new()
    {
        [OrderStatus.DraftCart] = new[] { (new[] { "Customer" }, new[] { OrderStatus.CheckoutStarted }) },
        [OrderStatus.CheckoutStarted] = new[] { (new[] { "Customer" }, new[] { OrderStatus.PaymentPending, OrderStatus.AwaitingAcceptance }) },
        [OrderStatus.PaymentPending] = new[] { (new[] { "System" }, new[] { OrderStatus.Paid, OrderStatus.PaymentFailed }) },
        [OrderStatus.Paid] = new[]
        {
            (new[] { "Partner" }, new[] { OrderStatus.Accepted, OrderStatus.RestaurantRejected }),
            (new[] { "Customer" }, new[] { OrderStatus.CancelRequested }),
            (new[] { "Admin" },   new[] { OrderStatus.Cancelled })
        },
        [OrderStatus.AwaitingAcceptance] = new[]
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