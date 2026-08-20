using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Application.Services;

public class OrderCancellationService
{
    private readonly OrderDbContext _context;
    private readonly IOrderRepository _orderRepo;

    public OrderCancellationService(OrderDbContext context, IOrderRepository orderRepo)
    {
        _context = context;
        _orderRepo = orderRepo;
    }

    public async Task<bool> CanCancelOrderAsync(Guid orderId, Guid userId)
    {
        var order = await _orderRepo.GetByIdAsync(orderId);
        
        if (order == null || order.CustomerId != userId)
            return false;

        // Can cancel if status is PaymentPending, Paid, or AwaitingAcceptance
        return order.Status == OrderStatus.PaymentPending 
            || order.Status == OrderStatus.Paid 
            || order.Status == OrderStatus.AwaitingAcceptance;
    }

    public async Task<(bool Success, string Message)> CancelOrderAsync(Guid orderId, Guid userId, string reason)
    {
        var order = await _orderRepo.GetByIdAsync(orderId);
        
        if (order == null)
            return (false, "Order not found");

        if (order.CustomerId != userId)
            return (false, "Unauthorized");

        // Check if order can be cancelled
        var cancellableStatuses = new[] { OrderStatus.PaymentPending, OrderStatus.Paid, OrderStatus.AwaitingAcceptance };
        if (!cancellableStatuses.Contains(order.Status))
            return (false, "Order cannot be cancelled at this stage");

        // Store the original status to check if refund is needed
        var wasAlreadyPaid = order.Status == OrderStatus.Paid;

        // Update order status
        order.Status = OrderStatus.Cancelled;
        order.CancellationReason = reason;
        order.CancelledAt = DateTime.UtcNow;
        order.CancelledBy = userId;
        order.UpdatedAt = DateTime.UtcNow;

        await _orderRepo.UpdateAsync(order);

        // If order was paid (not COD and not PaymentPending), create refund request
        if (order.PaymentMethod != "COD" && wasAlreadyPaid)
        {
            // Calculate refund using smart calculator
            var (refundAmount, platformFee, cancellationCharge) = 
                RefundCalculator.CalculateRefund(order.TotalAmount, order.PlatformFee);

            var refundRequest = new RefundRequest
            {
                OrderId = orderId,
                CustomerId = userId,
                OriginalAmount = order.TotalAmount,
                PlatformFee = platformFee,
                CancellationCharge = cancellationCharge,
                RefundAmount = refundAmount,
                Status = RefundStatus.PendingApproval,
                RequestedAt = DateTime.UtcNow
            };

            _context.RefundRequests.Add(refundRequest);
            await _context.SaveChangesAsync();
        }

        return (true, "Order cancelled successfully");
    }
}
