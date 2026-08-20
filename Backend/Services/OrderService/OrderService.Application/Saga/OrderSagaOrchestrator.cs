using System;
using System.Collections.Generic;
using System.Text;
using FoodDelivery.Shared.Constants;
using FoodDelivery.Shared.Events;
using FoodDelivery.Shared.Messaging;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Saga;

/// <summary>
/// Orchestration Saga for placing an order with proper compensation.
///
/// Saga Flow:
///   1. Create Order record (status = PaymentPending)
///   2. Publish OrderPlacedEvent to PaymentService
///   3. For COD: Immediately move to AwaitingAcceptance (payment on delivery)
///   4. For Card/Wallet: Remain in PaymentPending, await PaymentCompletedEvent
///   
/// Compensation (on payment failure):
///   - PaymentFailedEvent triggers PaymentFailedConsumer
///   - Order status → PaymentFailed
///   - CancellationReason set to payment failure details
///   - Order is effectively cancelled and cannot proceed
///
/// This implements the Saga pattern with automatic compensation for payment failures.
/// In production, consider using MassTransit or NServiceBus for distributed saga state.
/// </summary>
public class OrderSagaOrchestrator : IOrderSaga
{
    private readonly IOrderRepository _orderRepo;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<OrderSagaOrchestrator> _logger;

    public OrderSagaOrchestrator(
        IOrderRepository orderRepo, 
        IRabbitMqPublisher publisher,
        ILogger<OrderSagaOrchestrator> logger)
    {
        _orderRepo = orderRepo;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<OrderSagaResult> ExecuteAsync(OrderSagaRequest request)
    {
        Order? order = null;

        try
        {
            _logger.LogInformation(
                "[SAGA START] Creating order for Customer={CustomerId}, Restaurant={RestaurantId}, Amount={Amount}, Method={Method}",
                request.CustomerId, request.RestaurantId, request.TotalAmount, request.PaymentMethod);

            // ── Step 1: Create Order ───────────────────────────────────
            order = new Order
            {
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
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
                PlatformFee = request.PlatformFee,
                RestaurantCommission = request.RestaurantCommission,
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

            _logger.LogInformation(
                "[SAGA STEP 1] Order created with Id={OrderId}, Status={Status}",
                order.Id, order.Status);

            // ── Step 2: Handle payment based on method ────────────────
            if (request.PaymentAlreadyCompleted)
            {
                // Payment already completed (Card/Wallet) - move directly to Paid
                order.Status = OrderStatus.Paid;
                order.UpdatedAt = DateTime.UtcNow;
                await _orderRepo.UpdateAsync(order);
                await _orderRepo.SaveChangesAsync();
                
                _logger.LogInformation(
                    "[SAGA STEP 2] Payment already completed, order {OrderId} moved to Paid",
                    order.Id);
            }
            else if (request.PaymentMethod.Equals("COD", StringComparison.OrdinalIgnoreCase))
            {
                // COD orders go to AwaitingAcceptance - payment will be collected on delivery
                order.Status = OrderStatus.AwaitingAcceptance;
                order.UpdatedAt = DateTime.UtcNow;
                await _orderRepo.UpdateAsync(order);
                await _orderRepo.SaveChangesAsync();
                
                _logger.LogInformation(
                    "[SAGA STEP 2] COD order {OrderId} moved to AwaitingAcceptance",
                    order.Id);
            }
            else
            {
                // This shouldn't happen with new flow, but keep for safety
                _logger.LogWarning(
                    "[SAGA STEP 2] Order {OrderId} remains in PaymentPending (unexpected path)",
                    order.Id);
            }

            // ── Step 3: Publish OrderPlacedEvent ──────────────────────
            // Publish to per-service queues so both PaymentService and AdminService receive it.
            var orderPlacedEvent = new OrderPlacedEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                RestaurantId = order.RestaurantId,
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                Items = request.Items.Select(i => new FoodDelivery.Shared.Events.OrderItemDto
                {
                    MenuItemId = i.MenuItemId,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            _publisher.Publish(orderPlacedEvent, QueueNames.OrderPlacedPayment);  // → PaymentService
            _publisher.Publish(orderPlacedEvent, QueueNames.OrderPlacedAdmin);    // → AdminService

            _logger.LogInformation(
                "[SAGA STEP 3] OrderPlacedEvent published for Order={OrderId}",
                order.Id);

            // ── Step 4: For pre-paid orders, publish PaymentCompletedEvent ──
            // This ensures AdminService and OrderService know the payment is done.
            // For COD, payment happens on delivery so no event here.
            if (request.PaymentAlreadyCompleted)
            {
                var paymentCompletedEvent = new PaymentCompletedEvent
                {
                    OrderId = order.Id,
                    PaymentId = Guid.NewGuid(), // synthetic ID since payment was via Razorpay/Wallet
                    AmountPaid = order.TotalAmount,
                    PaymentMethod = order.PaymentMethod,
                    PaidAt = DateTime.UtcNow
                };

                _publisher.Publish(paymentCompletedEvent, QueueNames.PaymentCompletedOrder);  // → OrderService
                _publisher.Publish(paymentCompletedEvent, QueueNames.PaymentCompletedAdmin);  // → AdminService

                _logger.LogInformation(
                    "[SAGA STEP 4] PaymentCompletedEvent published for pre-paid Order={OrderId}",
                    order.Id);
            }

            _logger.LogInformation(
                "[SAGA SUCCESS] Order {OrderId} saga completed successfully",
                order.Id);

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
            _logger.LogError(ex, 
                "[SAGA COMPENSATION] Saga failed for Order={OrderId}, initiating compensation",
                order?.Id);

            if (order?.Id != null)
            {
                try
                {
                    order.Status = OrderStatus.Cancelled;
                    order.CancellationReason = $"Saga failure during order creation: {ex.Message}";
                    order.UpdatedAt = DateTime.UtcNow;
                    await _orderRepo.UpdateAsync(order);
                    await _orderRepo.SaveChangesAsync();
                    
                    _logger.LogWarning(
                        "[SAGA COMPENSATION] Order {OrderId} cancelled due to saga failure",
                        order.Id);
                }
                catch (Exception compensationEx)
                {
                    _logger.LogError(compensationEx,
                        "[SAGA COMPENSATION FAILED] Failed to compensate Order={OrderId}",
                        order.Id);
                }
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
