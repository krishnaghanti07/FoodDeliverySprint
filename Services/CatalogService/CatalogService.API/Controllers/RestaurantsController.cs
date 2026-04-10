using System.Security.Claims;
using CatalogService.Application.DTOs;
using CatalogService.Application.Services;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.API.Controllers;

[ApiController]
[Route("api/catalog/restaurants")]
public class RestaurantsController : ControllerBase
{
    private readonly CatalogAppService _catalogService;
    public RestaurantsController(CatalogAppService catalogService) =>
        _catalogService = catalogService;

    // ── PUBLIC ─────────────────────────────────────────────────────────

    /// <summary>Browse approved restaurants — public endpoint, no token needed</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? city,
        [FromQuery] string? cuisine,
        [FromQuery] string? search)
    {
        var list = await _catalogService.GetRestaurantsAsync(city, cuisine, search);
        return Ok(ApiResponse<List<RestaurantListDto>>.Ok(list));
    }

    /// <summary>Get restaurant + full menu — used by Customer & DeliveryAgent (pickup address)</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var detail = await _catalogService.GetRestaurantWithMenuAsync(id);
        if (detail is null)
            return NotFound(ApiResponse<RestaurantDetailDto>.Fail("Restaurant not found."));
        return Ok(ApiResponse<RestaurantDetailDto>.Ok(detail));
    }

    // ── PARTNER ────────────────────────────────────────────────────────

    /// <summary>Partner: Register a new restaurant (pending Admin approval)</summary>
    [HttpPost]
    [Authorize(Roles = "Partner")]
    public async Task<IActionResult> Create([FromBody] CreateRestaurantDto dto)
    {
        var partnerId = GetUserId();
        var id = await _catalogService.CreateRestaurantAsync(dto, partnerId);
        return CreatedAtAction(nameof(GetById), new { id },
            ApiResponse<Guid>.Ok(id, "Restaurant submitted for approval."));
    }

    /// <summary>Partner: Update own restaurant profile</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Partner,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateRestaurantDto dto)
    {
        try
        {
            await _catalogService.UpdateRestaurantAsync(id, dto, GetUserId(), GetRole());
            return Ok(ApiResponse<string>.Ok("Restaurant updated."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>Partner: Toggle restaurant open/closed status</summary>
    [HttpPatch("{id:guid}/toggle-open")]
    [Authorize(Roles = "Partner")]
    public async Task<IActionResult> ToggleOpen(Guid id)
    {
        try
        {
            var isOpen = await _catalogService.ToggleOpenStatusAsync(id, GetUserId());
            return Ok(ApiResponse<bool>.Ok(isOpen,
                isOpen ? "Restaurant is now Open." : "Restaurant is now Closed."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.Fail(ex.Message));
        }
    }

    // ── ADMIN ──────────────────────────────────────────────────────────

    /// <summary>Admin: Approve a restaurant</summary>
    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            await _catalogService.ApproveRestaurantAsync(id);
            return Ok(ApiResponse<string>.Ok("Restaurant approved."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>Admin: Get all restaurants including unapproved</summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAdmin()
    {
        var list = await _catalogService.GetAllIncludingUnapprovedAsync();
        return Ok(ApiResponse<List<RestaurantListDto>>.Ok(list));
    }

    /// <summary>Partner/Admin: Delete a restaurant</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Partner,Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _catalogService.DeleteRestaurantAsync(id, GetUserId(), GetRole());
            return Ok(ApiResponse<string>.Ok("Restaurant deleted successfully."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
    private string GetRole() => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}

[ApiController]
[Route("api/catalog/menu-items")]
public class MenuItemsController : ControllerBase
{
    private readonly CatalogAppService _catalogService;
    public MenuItemsController(CatalogAppService catalogService) =>
        _catalogService = catalogService;

    /// <summary>Get all menu items for a restaurant (public)</summary>
    [HttpGet]
    public async Task<IActionResult> GetByRestaurant([FromQuery] Guid restaurantId)
    {
        if (restaurantId == Guid.Empty)
            return BadRequest(ApiResponse<List<MenuItemDto>>.Fail("Restaurant ID is required."));

        var items = await _catalogService.GetMenuItemsByRestaurantAsync(restaurantId);
        return Ok(ApiResponse<List<MenuItemDto>>.Ok(items));
    }

    /// <summary>Get a single menu item by ID (public)</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _catalogService.GetMenuItemByIdAsync(id);
        if (item is null)
            return NotFound(ApiResponse<MenuItemDto>.Fail("Menu item not found."));
        return Ok(ApiResponse<MenuItemDto>.Ok(item));
    }

    /// <summary>Partner/Admin: Add a menu item to a restaurant</summary>
    [HttpPost]
    [Authorize(Roles = "Partner,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateMenuItemDto dto)
    {
        if (dto.Price <= 0)
            return BadRequest(ApiResponse<Guid>.Fail("Price must be greater than zero."));

        var id = await _catalogService.AddMenuItemAsync(dto);
        return Ok(ApiResponse<Guid>.Ok(id, "Menu item added."));
    }

    /// <summary>Partner/Admin: Update a menu item</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Partner,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateMenuItemDto dto)
    {
        try
        {
            await _catalogService.UpdateMenuItemAsync(id, dto);
            return Ok(ApiResponse<string>.Ok("Menu item updated."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>Partner/Admin: Toggle item availability</summary>
    [HttpPatch("{id:guid}/toggle-availability")]
    [Authorize(Roles = "Partner,Admin")]
    public async Task<IActionResult> ToggleAvailability(Guid id)
    {
        try
        {
            var available = await _catalogService.ToggleMenuItemAvailabilityAsync(id);
            return Ok(ApiResponse<bool>.Ok(available,
                available ? "Item is now available." : "Item marked unavailable."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>Partner/Admin: Delete a menu item</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Partner,Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _catalogService.DeleteMenuItemAsync(id);
            return Ok(ApiResponse<string>.Ok("Menu item deleted."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
    }
}