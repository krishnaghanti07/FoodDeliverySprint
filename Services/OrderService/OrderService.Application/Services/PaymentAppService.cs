using System;
using System.Collections.Generic;
using System.Text;
using FoodDelivery.Shared.Constants;
using FoodDelivery.Shared.Events;
using FoodDelivery.Shared.Messaging;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Services;

public class PaymentAppService : IPaymentService
{
    private readonly IOrderRepository _orderRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IRabbitMqPublisher _publisher;

    public PaymentAppService(
        IOrderRepository orderRepo,
        IPaymentRepository paymentRepo,
        IRabbitMqPublisher publisher)
    {
        _orderRepo = orderRepo;
        _paymentRepo = paymentRepo;
        _publisher = publisher;
    }

    public async Task<PaymentResultDto> SimulatePaymentAsync(SimulatePaymentDto dto)
    {
        var order = await _orderRepo.GetByIdWithDetailsAsync(dto.OrderId)
            ?? throw new KeyNotFoundException("Order not found.");

        if (order.Status != OrderStatus.PaymentPending && order.Status != OrderStatus.PaymentFailed)
            throw new InvalidOperationException(
                $"Payment cannot be processed. Order is in status: {order.Status}.");

        var payment = new Payment
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Method = dto.Method.ToUpperInvariant(),
            Status = dto.ShouldSucceed ? PaymentStatus.Success : PaymentStatus.Failed,
            TransactionId = dto.ShouldSucceed
                ? $"TXN-{Guid.NewGuid():N}".ToUpperInvariant()[..18]
                : null,
            FailureReason = dto.ShouldSucceed ? null : "Simulated payment failure.",
            PaidAt = dto.ShouldSucceed ? DateTime.UtcNow : null
        };

        await _paymentRepo.AddAsync(payment);

        if (dto.ShouldSucceed)
        {
            order.Status = OrderStatus.Paid;
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepo.UpdateAsync(order);

            _publisher.Publish(new PaymentCompletedEvent
            {
                OrderId = order.Id,
                PaymentId = payment.Id,
                AmountPaid = payment.Amount,
                PaymentMethod = payment.Method
            }, QueueNames.PaymentCompleted);
        }
        else
        {
            order.Status = OrderStatus.PaymentFailed;
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepo.UpdateAsync(order);

            _publisher.Publish(new PaymentFailedEvent
            {
                OrderId = order.Id,
                Reason = payment.FailureReason!
            }, QueueNames.PaymentFailed);
        }

        await _paymentRepo.SaveChangesAsync();
        await _orderRepo.SaveChangesAsync();

        return new PaymentResultDto
        {
            PaymentId = payment.Id,
            OrderId = order.Id,
            Amount = payment.Amount,
            Method = payment.Method,
            Status = payment.Status.ToString(),
            TransactionId = payment.TransactionId,
            FailureReason = payment.FailureReason,
            ProcessedAt = DateTime.UtcNow
        };
    }

    public async Task<PaymentSummaryDto?> GetPaymentByOrderIdAsync(Guid orderId)
    {
        var payment = await _paymentRepo.GetByOrderIdAsync(orderId);
        if (payment is null) return null;
        return new PaymentSummaryDto
        {
            Id = payment.Id,
            Amount = payment.Amount,
            Method = payment.Method,
            Status = payment.Status.ToString(),
            TransactionId = payment.TransactionId,
            PaidAt = payment.PaidAt
        };
    }
}