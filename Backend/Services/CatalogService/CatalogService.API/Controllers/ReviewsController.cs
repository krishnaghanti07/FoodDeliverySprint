using System.Security.Claims;
using CatalogService.Application.DTOs;
using CatalogService.Application.Services;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.API.Controllers;

[ApiController]
[Route("api/catalog/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly CatalogAppService _catalogService;
    public ReviewsController(CatalogAppService catalogService) =>
        _catalogService = catalogService;

    /// <summary>Get reviews for a restaurant (public)</summary>
    [HttpGet]
    public async Task<IActionResult> GetByRestaurant(
        [FromQuery] Guid restaurantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (restaurantId == Guid.Empty)
            return BadRequest(ApiResponse<List<ReviewDto>>.Fail("Restaurant ID is required."));

        var reviews = await _catalogService.GetReviewsAsync(restaurantId, page, pageSize);
        return Ok(ApiResponse<List<ReviewDto>>.Ok(reviews));
    }

    /// <summary>Get ratings summary for a restaurant (public)</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] Guid restaurantId)
    {
        if (restaurantId == Guid.Empty)
            return BadRequest(ApiResponse<RestaurantRatingsSummaryDto>.Fail("Restaurant ID is required."));

        var summary = await _catalogService.GetRatingsSummaryAsync(restaurantId);
        return Ok(ApiResponse<RestaurantRatingsSummaryDto>.Ok(summary));
    }

    /// <summary>Customer: Add a review for a restaurant</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddReview([FromQuery] Guid restaurantId, [FromBody] CreateReviewDto dto)
    {
        if (restaurantId == Guid.Empty)
            return BadRequest(ApiResponse<Guid>.Fail("Restaurant ID is required."));

        try
        {
            var userId = GetUserId();
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "Anonymous";
            
            var reviewId = await _catalogService.AddReviewAsync(restaurantId, userId, userName, dto);
            return Ok(ApiResponse<Guid>.Ok(reviewId, "Review added successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<Guid>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<Guid>.Fail(ex.Message));
        }
    }

    /// <summary>Customer: Update own review</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateReview(Guid id, [FromBody] UpdateReviewDto dto)
    {
        try
        {
            var userId = GetUserId();
            await _catalogService.UpdateReviewAsync(id, userId, dto);
            return Ok(ApiResponse<string>.Ok("Review updated successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>Customer/Admin: Delete a review</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var role = GetRole();
            await _catalogService.DeleteReviewAsync(id, userId, role);
            return Ok(ApiResponse<string>.Ok("Review deleted successfully."));
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

    /// <summary>Mark a review as helpful (public)</summary>
    [HttpPost("{id:guid}/helpful")]
    public async Task<IActionResult> MarkHelpful(Guid id)
    {
        try
        {
            await _catalogService.MarkReviewHelpfulAsync(id);
            return Ok(ApiResponse<string>.Ok("Review marked as helpful."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
    private string GetRole() => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
