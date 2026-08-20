using System.Security.Claims;
using CatalogService.Application.DTOs;
using CatalogService.Application.Services;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.API.Controllers;

[ApiController]
[Route("api/catalog/categories")]
public class CategoriesController : ControllerBase
{
    private readonly CatalogAppService _catalogService;
    public CategoriesController(CatalogAppService catalogService) =>
        _catalogService = catalogService;

    /// <summary>Get all categories for a restaurant</summary>
    [HttpGet]
    public async Task<IActionResult> GetByRestaurant([FromQuery] Guid restaurantId)
    {
        if (restaurantId == Guid.Empty)
            return BadRequest(ApiResponse<List<CategoryDto>>.Fail("Restaurant ID is required."));

        var categories = await _catalogService.GetCategoriesByRestaurantAsync(restaurantId);
        return Ok(ApiResponse<List<CategoryDto>>.Ok(categories));
    }

    /// <summary>Partner/Admin: Create a new category</summary>
    [HttpPost]
    [Authorize(Roles = "Partner,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var id = await _catalogService.CreateCategoryAsync(dto);
        return Ok(ApiResponse<Guid>.Ok(id, "Category created successfully."));
    }

    /// <summary>Partner/Admin: Update a category</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Partner,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        try
        {
            await _catalogService.UpdateCategoryAsync(id, dto);
            return Ok(ApiResponse<string>.Ok("Category updated successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>Partner/Admin: Delete a category</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Partner,Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _catalogService.DeleteCategoryAsync(id);
            return Ok(ApiResponse<string>.Ok("Category deleted successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>Partner/Admin: Reorder categories</summary>
    [HttpPost("reorder")]
    [Authorize(Roles = "Partner,Admin")]
    public async Task<IActionResult> Reorder([FromQuery] Guid restaurantId, [FromBody] ReorderCategoriesDto dto)
    {
        await _catalogService.ReorderCategoriesAsync(restaurantId, dto);
        return Ok(ApiResponse<string>.Ok("Categories reordered successfully."));
    }
}
