using OrderService.Application.DTOs;

namespace OrderService.Application.Interfaces;

public interface IRatingService
{
    Task<OrderRatingDto> AddRatingAsync(Guid orderId, Guid customerId, CreateOrderRatingDto dto);
    Task<OrderRatingDto?> GetRatingByOrderIdAsync(Guid orderId);
    Task<List<OrderRatingDto>> GetMyRatingsAsync(Guid customerId);
    Task<OrderRatingDto> UpdateRatingAsync(Guid ratingId, Guid customerId, UpdateOrderRatingDto dto);
    Task DeleteRatingAsync(Guid ratingId, Guid customerId);
    
    // Cancellation
    Task<List<CancellationReasonDto>> GetCancellationReasonsAsync();
    Task<CanCancelOrderDto> CanCancelOrderAsync(Guid orderId, Guid customerId);
    Task CancelOrderAsync(Guid orderId, Guid customerId, CancelOrderDto dto);
}
