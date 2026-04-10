using System;
using System.Collections.Generic;
using System.Text;
using FoodDelivery.Shared.Constants;
using FoodDelivery.Shared.Events;
using FoodDelivery.Shared.Messaging;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Application.Saga;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Services;

public class OrderAppService : IOrderService
{
    private readonly IOrderRepository _orderRepo;
    private readonly ICartRepository _cartRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IOrderSaga _saga;
    private readonly IRabbitMqPublisher _publisher;
    private const decimal GstRate = 0.05m;
    private const decimal DeliveryFeeFlat = 30.00m;

    public OrderAppService(
        IOrderRepository orderRepo,
        ICartRepository cartRepo,
        IPaymentRepository paymentRepo,
        IOrderSaga saga,
        IRabbitMqPublisher publisher)
    {
        _orderRepo = orderRepo;
        _cartRepo = cartRepo;
        _paymentRepo = paymentRepo;
        _saga = saga;
        _publisher = publisher;
    }

    public async Task<OrderDto> PlaceOrderAsync(Guid customerId, PlaceOrderDto dto)
    {
        var cart = await _cartRepo.GetByCustomerIdAsync(customerId)
            ?? throw new InvalidOperationException("Cart is empty. Add items before placing an order.");

        if (!cart.Items.Any())
            throw new InvalidOperationException("Cart is empty.");

        if (!cart.RestaurantId.HasValue)
            throw new InvalidOperationException("Cart has no associated restaurant.");

        var allowed = new[] { "COD", "Card", "Wallet" };
        if (!allowed.Contains(dto.PaymentMethod.ToUpperInvariant()))
            throw new ArgumentException("Invalid payment method. Allowed: COD, Card, Wallet.");

        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        var gst = Math.Round(subtotal * GstRate, 2);
        var total = subtotal + DeliveryFeeFlat + gst - cart.Discount;

        var sagaRequest = new OrderSagaRequest
        {
            CustomerId = customerId,
            RestaurantId = cart.RestaurantId.Value,
            RestaurantName = cart.RestaurantName ?? string.Empty,
            DeliveryAddress = dto.DeliveryAddress,
            DeliveryInstructions = dto.DeliveryInstructions,
            PaymentMethod = dto.PaymentMethod.ToUpperInvariant(),
            CouponCode = cart.CouponCode,
            Subtotal = subtotal,
            DeliveryFee = DeliveryFeeFlat,
            Discount = cart.Discount,
            GstAmount = gst,
            TotalAmount = total,
            Items = cart.Items.Select(i => new SagaOrderItem
            {
                MenuItemId = i.MenuItemId,
                Name = i.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                IsVeg = i.IsVeg
            }).ToList()
        };

        var result = await _saga.ExecuteAsync(sagaRequest);
        if (!result.Success)
            throw new InvalidOperationException(result.Message);

        // Clear cart after successful order
        await _cartRepo.DeleteAsync(customerId);
        await _cartRepo.SaveChangesAsync();

        var order = await _orderRepo.GetByIdWithDetailsAsync(result.OrderId!.Value)
            ?? throw new InvalidOperationException("Order created but could not be retrieved.");

        return MapToDto(order);
    }

    public async Task<OrderDto> GetByIdAsync(Guid orderId, Guid requesterId, string role)
    {
        var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found.");

        // Customers can only see their own orders
        if (role == "Customer" && order.CustomerId != requesterId)
            throw new UnauthorizedAccessException("Access denied.");

        // Partners can only see orders for their restaurant
        if (role == "Partner" && order.RestaurantId != requesterId)
            throw new UnauthorizedAccessException("Access denied.");

        return MapToDto(order);
    }

    public async Task<List<OrderDto>> GetMyOrdersAsync(Guid customerId)
    {
        var orders = await _orderRepo.GetByCustomerIdAsync(customerId);
        return orders.Select(MapToDto).ToList();
    }

    public async Task<List<OrderDto>> GetByRestaurantIdAsync(Guid restaurantId)
    {
        var orders = await _orderRepo.GetByRestaurantIdAsync(restaurantId);
        return orders.Select(MapToDto).ToList();
    }

    public async Task<List<OrderDto>> GetAllAsync()
    {
        var orders = await _orderRepo.GetAllAsync();
        return orders.Select(MapToDto).ToList();
    }

    public async Task<OrderDto> UpdateStatusAsync(Guid orderId, UpdateOrderStatusDto dto, string role)
    {
        var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found.");

        if (!Enum.TryParse<OrderStatus>(dto.NewStatus, ignoreCase: true, out var newStatus))
            throw new ArgumentException($"Invalid status '{dto.NewStatus}'.");

        if (!OrderStatusTransitions.IsValid(order.Status, newStatus, role))
            throw new InvalidOperationException(
                $"Role '{role}' cannot move order from '{order.Status}' to '{newStatus}'.");

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.Reason))
            order.CancellationReason = dto.Reason;

        // Publish ReadyForPickup event so delivery agents are notified
        if (newStatus == OrderStatus.ReadyForPickup)
        {
            _publisher.Publish(new OrderReadyForPickupEvent
            {
                OrderId = order.Id,
                RestaurantId = order.RestaurantId,
                RestaurantName = order.RestaurantName,
                DeliveryAddress = order.DeliveryAddress,
                TotalAmount = order.TotalAmount
            }, QueueNames.OrderReadyForPickup);
        }

        await _orderRepo.UpdateAsync(order);
        await _orderRepo.SaveChangesAsync();
        return MapToDto(order);
    }

    // ── Mapping ───────────────────────────────────────────────────────

    private static OrderDto MapToDto(Order o) => new()
    {
        Id = o.Id,
        CustomerId = o.CustomerId,
        RestaurantId = o.RestaurantId,
        RestaurantName = o.RestaurantName,
        DeliveryAddress = o.DeliveryAddress,
        DeliveryInstructions = o.DeliveryInstructions,
        CouponCode = o.CouponCode,
        Subtotal = o.Subtotal,
        DeliveryFee = o.DeliveryFee,
        Discount = o.Discount,
        GstAmount = o.GstAmount,
        TotalAmount = o.TotalAmount,
        PaymentMethod = o.PaymentMethod,
        Status = o.Status.ToString(),
        CancellationReason = o.CancellationReason,
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt,
        Items = o.Items.Select(i => new DTOs.OrderItemDto
        {
            Id = i.Id,
            MenuItemId = i.MenuItemId,
            Name = i.Name,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.UnitPrice * i.Quantity,
            IsVeg = i.IsVeg
        }).ToList(),
        Payment = o.Payment is null ? null : new PaymentSummaryDto
        {
            Id = o.Payment.Id,
            Amount = o.Payment.Amount,
            Method = o.Payment.Method,
            Status = o.Payment.Status.ToString(),
            TransactionId = o.Payment.TransactionId,
            PaidAt = o.Payment.PaidAt
        },
        DeliveryAssignment = o.DeliveryAssignment is null ? null : new DeliveryAssignmentDto
        {
            Id = o.DeliveryAssignment.Id,
            OrderId = o.DeliveryAssignment.OrderId,
            AgentId = o.DeliveryAssignment.AgentId,
            AgentName = o.DeliveryAssignment.AgentName,
            AgentMobile = o.DeliveryAssignment.AgentMobile,
            Status = o.DeliveryAssignment.Status.ToString(),
            AssignedAt = o.DeliveryAssignment.AssignedAt,
            PickedUpAt = o.DeliveryAssignment.PickedUpAt,
            OutForDeliveryAt = o.DeliveryAssignment.OutForDeliveryAt,
            DeliveredAt = o.DeliveryAssignment.DeliveredAt
        }
    };
}
