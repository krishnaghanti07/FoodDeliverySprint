using System.Security.Claims;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;
    public RatingsController(IRatingService ratingService) => _ratingService = ratingService;

    /// <summary>Customer: Add rating for a delivered order</summary>
    [HttpPost("{orderId:guid}/rating")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> AddRating(Guid orderId, [FromBody] CreateOrderRatingDto dto)
    {
        try
        {
            var rating = await _ratingService.AddRatingAsync(orderId, GetUserId(), dto);
            return Ok(ApiResponse<OrderRatingDto>.Ok(rating, "Rating submitted successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<OrderRatingDto>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<OrderRatingDto>.Fail(ex.Message));
        }
    }

    /// <summary>Get rating for an order (public)</summary>
    [HttpGet("{orderId:guid}/rating")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRating(Guid orderId)
    {
        var rating = await _ratingService.GetRatingByOrderIdAsync(orderId);
        if (rating == null)
            return NotFound(ApiResponse<OrderRatingDto>.Fail("No rating found for this order."));
        return Ok(ApiResponse<OrderRatingDto>.Ok(rating));
    }

    /// <summary>Customer: Update own rating</summary>
    [HttpPut("ratings/{ratingId:guid}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> UpdateRating(Guid ratingId, [FromBody] UpdateOrderRatingDto dto)
    {
        try
        {
            var rating = await _ratingService.UpdateRatingAsync(ratingId, GetUserId(), dto);
            return Ok(ApiResponse<OrderRatingDto>.Ok(rating, "Rating updated successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<OrderRatingDto>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>Customer: Delete own rating</summary>
    [HttpDelete("ratings/{ratingId:guid}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> DeleteRating(Guid ratingId)
    {
        try
        {
            await _ratingService.DeleteRatingAsync(ratingId, GetUserId());
            return Ok(ApiResponse<string>.Ok("Rating deleted successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>Customer: Get my rating history</summary>
    [HttpGet("ratings/my")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyRatings()
    {
        var ratings = await _ratingService.GetMyRatingsAsync(GetUserId());
        return Ok(ApiResponse<List<OrderRatingDto>>.Ok(ratings));
    }

    /// <summary>Get available cancellation reasons</summary>
    [HttpGet("cancellation-reasons")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetCancellationReasons()
    {
        var reasons = await _ratingService.GetCancellationReasonsAsync();
        return Ok(ApiResponse<List<CancellationReasonDto>>.Ok(reasons));
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
