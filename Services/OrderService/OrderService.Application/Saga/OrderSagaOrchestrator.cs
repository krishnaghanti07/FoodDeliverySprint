using System;
using System.Collections.Generic;
using System.Text;
using FoodDelivery.Shared.Constants;
using FoodDelivery.Shared.Events;
using FoodDelivery.Shared.Messaging;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Saga;

/// <summary>
/// Orchestration Saga for placing an order.
///
/// Steps:
///   1. Create Order record (status = PaymentPending)
///   2. For COD: mark Paid immediately, publish OrderPlacedEvent
///   3. For Card/Wallet: publish OrderPlacedEvent, await PaymentCompletedEvent
///      (consumer updates status to Paid when event arrives)
///   4. On any failure: compensate by setting order to Cancelled
///
/// This is a simplified synchronous saga. In production you would use
/// MassTransit or NServiceBus for distributed saga state persistence.
/// </summary>
public class OrderSagaOrchestrator : IOrderSaga
{
    private readonly IOrderRepository _orderRepo;
    private readonly IRabbitMqPublisher _publisher;

    public OrderSagaOrchestrator(IOrderRepository orderRepo, IRabbitMqPublisher publisher)
    {
        _orderRepo = orderRepo;
        _publisher = publisher;
    }

    public async Task<OrderSagaResult> ExecuteAsync(OrderSagaRequest request)
    {
        Order? order = null;

        try
        {
            // ── Step 1: Create Order ───────────────────────────────────
            order = new Order
            {
                CustomerId = request.CustomerId,
                RestaurantId = request.RestaurantId,
                RestaurantName = request.RestaurantName,
                DeliveryAddress = request.DeliveryAddress,
                DeliveryInstructions = request.DeliveryInstructions,
                PaymentMethod = request.PaymentMethod,
                CouponCode = request.CouponCode,
                Subtotal = request.Subtotal,
                DeliveryFee = request.DeliveryFee,
                Discount = request.Discount,
                GstAmount = request.GstAmount,
                TotalAmount = request.TotalAmount,
                Status = OrderStatus.PaymentPending,
                Items = request.Items.Select(i => new OrderItem
                {
                    MenuItemId = i.MenuItemId,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    IsVeg = i.IsVeg
                }).ToList()
            };

            await _orderRepo.AddAsync(order);
            await _orderRepo.SaveChangesAsync();

            // ── Step 2: COD — auto-confirm payment ────────────────────
            if (request.PaymentMethod.Equals("COD", StringComparison.OrdinalIgnoreCase))
            {
                order.Status = OrderStatus.Paid;
                order.UpdatedAt = DateTime.UtcNow;
                await _orderRepo.UpdateAsync(order);
                await _orderRepo.SaveChangesAsync();
            }

            // ── Step 3: Publish OrderPlacedEvent ──────────────────────
            // PaymentService consumes this and processes Card/Wallet payments.
            // On success it publishes PaymentCompletedEvent → consumer sets Paid.
            // On failure it publishes PaymentFailedEvent → consumer sets PaymentFailed.
            _publisher.Publish(new OrderPlacedEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                RestaurantId = order.RestaurantId,
                TotalAmount = order.TotalAmount,
                Items = request.Items.Select(i => new FoodDelivery.Shared.Events.OrderItemDto
                {
                    MenuItemId = i.MenuItemId,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            }, QueueNames.OrderPlaced);

            return new OrderSagaResult
            {
                Success = true,
                OrderId = order.Id,
                Message = request.PaymentMethod.Equals("COD", StringComparison.OrdinalIgnoreCase)
                    ? "Order placed successfully with Cash on Delivery."
                    : "Order created. Awaiting payment confirmation."
            };
        }
        catch (Exception ex)
        {
            // ── Compensation: cancel the order ───────────────────────
            if (order?.Id != null)
            {
                order.Status = OrderStatus.Cancelled;
                order.CancellationReason = $"Saga failure: {ex.Message}";
                order.UpdatedAt = DateTime.UtcNow;
                await _orderRepo.UpdateAsync(order);
                await _orderRepo.SaveChangesAsync();
            }

            return new OrderSagaResult
            {
                Success = false,
                OrderId = order?.Id,
                Message = "Order placement failed. Order has been cancelled.",
                FailureStep = ex.Message
            };
        }
    }
}
