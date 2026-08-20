using System.Text.Json;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Services;

public class RatingAppService : IRatingService
{
    private readonly IRatingRepository _ratingRepo;
    private readonly IOrderRepository _orderRepo;

    public RatingAppService(IRatingRepository ratingRepo, IOrderRepository orderRepo)
    {
        _ratingRepo = ratingRepo;
        _orderRepo = orderRepo;
    }

    public async Task<OrderRatingDto> AddRatingAsync(Guid orderId, Guid customerId, CreateOrderRatingDto dto)
    {
        // Verify order exists and belongs to customer
        var order = await _orderRepo.GetByIdAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found.");

        if (order.CustomerId != customerId)
            throw new UnauthorizedAccessException("You can only rate your own orders.");

        if (order.Status != OrderStatus.Delivered)
            throw new InvalidOperationException("You can only rate delivered orders.");

        // Check if already rated
        var existing = await _ratingRepo.GetByOrderIdAsync(orderId);
        if (existing != null)
            throw new InvalidOperationException("You have already rated this order. Use update instead.");

        var rating = new OrderRating
        {
            OrderId = orderId,
            CustomerId = customerId,
            FoodRating = dto.FoodRating,
            DeliveryRating = dto.DeliveryRating,
            Comment = dto.Comment,
            Tags = dto.Tags.Any() ? JsonSerializer.Serialize(dto.Tags) : null,
            Photos = dto.Photos.Any() ? JsonSerializer.Serialize(dto.Photos) : null
        };

        await _ratingRepo.AddAsync(rating);
        await _ratingRepo.SaveChangesAsync();

        return MapToDto(rating);
    }

    public async Task<OrderRatingDto?> GetRatingByOrderIdAsync(Guid orderId)
    {
        var rating = await _ratingRepo.GetByOrderIdAsync(orderId);
        return rating == null ? null : MapToDto(rating);
    }

    public async Task<List<OrderRatingDto>> GetMyRatingsAsync(Guid customerId)
    {
        var ratings = await _ratingRepo.GetByCustomerIdAsync(customerId);
        return ratings.Select(MapToDto).ToList();
    }

    public async Task<OrderRatingDto> UpdateRatingAsync(Guid ratingId, Guid customerId, UpdateOrderRatingDto dto)
    {
        var rating = await _ratingRepo.GetByIdAsync(ratingId)
            ?? throw new KeyNotFoundException("Rating not found.");

        if (rating.CustomerId != customerId)
            throw new UnauthorizedAccessException("You can only update your own ratings.");

        rating.FoodRating = dto.FoodRating;
        rating.DeliveryRating = dto.DeliveryRating;
        rating.Comment = dto.Comment;
        rating.Tags = dto.Tags.Any() ? JsonSerializer.Serialize(dto.Tags) : null;
        rating.Photos = dto.Photos.Any() ? JsonSerializer.Serialize(dto.Photos) : null;
        rating.UpdatedAt = DateTime.UtcNow;

        await _ratingRepo.UpdateAsync(rating);
        await _ratingRepo.SaveChangesAsync();

        return MapToDto(rating);
    }

    public async Task DeleteRatingAsync(Guid ratingId, Guid customerId)
    {
        var rating = await _ratingRepo.GetByIdAsync(ratingId)
            ?? throw new KeyNotFoundException("Rating not found.");

        if (rating.CustomerId != customerId)
            throw new UnauthorizedAccessException("You can only delete your own ratings.");

        await _ratingRepo.DeleteAsync(ratingId);
        await _ratingRepo.SaveChangesAsync();
    }

    // ── Cancellation ───────────────────────────────────────────────────

    public Task<List<CancellationReasonDto>> GetCancellationReasonsAsync()
    {
        var reasons = new List<CancellationReasonDto>
        {
            new() { Code = "CHANGED_MIND", DisplayText = "Changed my mind", Category = "Customer" },
            new() { Code = "WRONG_ORDER", DisplayText = "Ordered wrong items", Category = "Customer" },
            new() { Code = "TAKING_TOO_LONG", DisplayText = "Taking too long", Category = "Time" },
            new() { Code = "FOUND_BETTER_DEAL", DisplayText = "Found a better deal", Category = "Price" },
            new() { Code = "PAYMENT_ISSUE", DisplayText = "Payment issue", Category = "Payment" },
            new() { Code = "RESTAURANT_CLOSED", DisplayText = "Restaurant is closed", Category = "Restaurant" },
            new() { Code = "DELIVERY_ISSUE", DisplayText = "Delivery not available", Category = "Delivery" },
            new() { Code = "OTHER", DisplayText = "Other reason", Category = "Other" }
        };

        return Task.FromResult(reasons);
    }

    public async Task<CanCancelOrderDto> CanCancelOrderAsync(Guid orderId, Guid customerId)
    {
        var order = await _orderRepo.GetByIdAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found.");

        if (order.CustomerId != customerId)
            throw new UnauthorizedAccessException("Access denied.");

        // Can cancel if order is Paid, Accepted, or Preparing
        var cancellableStatuses = new[] { OrderStatus.Paid, OrderStatus.Accepted, OrderStatus.Preparing };
        bool canCancel = cancellableStatuses.Contains(order.Status);

        decimal cancellationFee = 0m;
        decimal refundAmount = order.TotalAmount;
        string? reason = null;

        if (!canCancel)
        {
            reason = order.Status switch
            {
                OrderStatus.DraftCart or OrderStatus.CheckoutStarted or OrderStatus.PaymentPending =>
                    "Order has not been paid yet.",
                OrderStatus.ReadyForPickup or OrderStatus.PickedUp or OrderStatus.OutForDelivery =>
                    "Order is already being delivered and cannot be cancelled.",
                OrderStatus.Delivered => "Order has already been delivered.",
                OrderStatus.Cancelled or OrderStatus.CancelRequested => "Order is already cancelled.",
                _ => "Order cannot be cancelled at this stage."
            };
        }
        else
        {
            // Calculate cancellation fee based on order status
            if (order.Status == OrderStatus.Preparing)
            {
                cancellationFee = Math.Round(order.TotalAmount * 0.10m, 2); // 10% fee if preparing
                refundAmount = order.TotalAmount - cancellationFee;
            }
        }

        return new CanCancelOrderDto
        {
            CanCancel = canCancel,
            Reason = reason,
            CancellationFee = cancellationFee > 0 ? cancellationFee : null,
            RefundAmount = canCancel ? refundAmount : null
        };
    }

    public async Task CancelOrderAsync(Guid orderId, Guid customerId, CancelOrderDto dto)
    {
        var canCancel = await CanCancelOrderAsync(orderId, customerId);
        if (!canCancel.CanCancel)
            throw new InvalidOperationException(canCancel.Reason ?? "Order cannot be cancelled.");

        var order = await _orderRepo.GetByIdAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found.");

        order.Status = OrderStatus.CancelRequested;
        order.CancellationReason = dto.Reason;
        order.UpdatedAt = DateTime.UtcNow;

        await _orderRepo.UpdateAsync(order);
        await _orderRepo.SaveChangesAsync();

        // In production, this would trigger:
        // 1. Notification to restaurant
        // 2. Refund initiation
        // 3. Email/SMS to customer
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static OrderRatingDto MapToDto(OrderRating rating)
    {
        var tags = string.IsNullOrEmpty(rating.Tags)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(rating.Tags) ?? new List<string>();

        var photos = string.IsNullOrEmpty(rating.Photos)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(rating.Photos) ?? new List<string>();

        return new OrderRatingDto
        {
            Id = rating.Id,
            OrderId = rating.OrderId,
            CustomerId = rating.CustomerId,
            FoodRating = rating.FoodRating,
            DeliveryRating = rating.DeliveryRating,
            AverageRating = Math.Round((rating.FoodRating + rating.DeliveryRating) / 2.0, 1),
            Comment = rating.Comment,
            Tags = tags,
            Photos = photos,
            CreatedAt = rating.CreatedAt,
            UpdatedAt = rating.UpdatedAt
        };
    }
}
