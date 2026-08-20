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

    /// <summary>
    /// Get home page data with promoted restaurants and popular cuisines.
    /// PRD page 6: GET /gateway/catalog/home
    /// </summary>
    [HttpGet("home")]
    public async Task<IActionResult> GetHome([FromQuery] string? city)
    {
        var data = await _catalogService.GetHomePageDataAsync(city);
        return Ok(ApiResponse<HomePageDto>.Ok(data, "Home data loaded successfully."));
    }

    /// <summary>
    /// Get nearby restaurants (location-aware).
    /// PRD page 6: GET /gateway/catalog/restaurants/nearby
    /// </summary>
    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearby([FromQuery] string? city)
    {
        var list = await _catalogService.GetNearbyRestaurantsAsync(city);
        return Ok(ApiResponse<List<RestaurantListDto>>.Ok(list, 
            $"Found {list.Count} nearby restaurants."));
    }

    /// <summary>Browse approved restaurants — public endpoint, no token needed</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? city,
        [FromQuery] string? cuisine,
        [FromQuery] string? search)
    {
        var list = await _catalogService.GetRestaurantsAsync(city, cuisine, search);
        
        // Auto-sync ratings for all restaurants
        await SyncAllRestaurantRatingsAsync(list);
        
        // Refresh the list to get updated ratings
        list = await _catalogService.GetRestaurantsAsync(city, cuisine, search);
        
        return Ok(ApiResponse<List<RestaurantListDto>>.Ok(list));
    }

    /// <summary>
    /// Enhanced search across restaurants and menu items
    /// GET /api/catalog/restaurants/search?query=pizza
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(ApiResponse<SearchResultsDto>.Fail("Search query is required."));

        var results = await _catalogService.SearchAsync(query);
        return Ok(ApiResponse<SearchResultsDto>.Ok(results, 
            $"Found {results.TotalResults} results ({results.Restaurants.Count} restaurants, {results.MenuItems.Count} menu items)."));
    }

    /// <summary>Get restaurant + full menu — used by Customer & DeliveryAgent (pickup address)</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        // Sync rating for this restaurant
        await SyncRestaurantRatingAsync(id);
        
        var detail = await _catalogService.GetRestaurantWithMenuAsync(id);
        if (detail is null)
            return NotFound(ApiResponse<RestaurantDetailDto>.Fail("Restaurant not found."));
        return Ok(ApiResponse<RestaurantDetailDto>.Ok(detail));
    }

    /// <summary>Get restaurant by partner user ID — used for authorization checks</summary>
    [HttpGet("by-partner/{partnerUserId:guid}")]
    public async Task<IActionResult> GetByPartnerUserId(Guid partnerUserId)
    {
        var restaurants = await _catalogService.GetRestaurantsByPartnerIdAsync(partnerUserId);
        var restaurant = restaurants.FirstOrDefault();
        
        if (restaurant is null)
            return NotFound(ApiResponse<RestaurantListDto>.Fail("Restaurant not found for this partner."));
        
        return Ok(ApiResponse<RestaurantListDto>.Ok(restaurant));
    }

    // ── PARTNER ────────────────────────────────────────────────────────

    /// <summary>Partner: Get own restaurants (including unapproved)</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Partner")]
    public async Task<IActionResult> GetMyRestaurants()
    {
        var partnerId = GetUserId();
        var list = await _catalogService.GetRestaurantsByPartnerIdAsync(partnerId);
        
        // Auto-sync rating for partner's restaurant
        if (list.Count > 0)
        {
            var restaurantId = list[0].Id;
            try
            {
                // Call OrderService to sync rating
                var httpClient = new HttpClient();
                // Use internal endpoint that doesn't require authentication
                var ordersResponse = await httpClient.GetAsync($"http://localhost:5003/api/orders/internal/restaurant/{restaurantId}/orders");
                
                if (ordersResponse.IsSuccessStatusCode)
                {
                    var ordersContent = await ordersResponse.Content.ReadAsStringAsync();
                    var ordersData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(ordersContent);
                    
                    double totalRating = 0;
                    int ratedOrdersCount = 0;
                    
                    if (ordersData.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var order in dataElement.EnumerateArray())
                        {
                            if (order.TryGetProperty("rating", out var ratingElement) && ratingElement.ValueKind != System.Text.Json.JsonValueKind.Null)
                            {
                                if (ratingElement.TryGetProperty("foodRating", out var foodRatingElement))
                                {
                                    var foodRating = foodRatingElement.GetDouble();
                                    var deliveryRating = foodRating;
                                    
                                    if (ratingElement.TryGetProperty("deliveryRating", out var deliveryRatingElement))
                                    {
                                        deliveryRating = deliveryRatingElement.GetDouble();
                                    }
                                    
                                    var avgOrderRating = (foodRating + deliveryRating) / 2.0;
                                    totalRating += avgOrderRating;
                                    ratedOrdersCount++;
                                }
                            }
                        }
                    }
                    
                    if (ratedOrdersCount > 0)
                    {
                        double newRating = Math.Round(totalRating / ratedOrdersCount, 1);
                        await _catalogService.UpdateRestaurantRatingAsync(restaurantId, newRating);
                        
                        // Refresh the list to get updated rating
                        list = await _catalogService.GetRestaurantsByPartnerIdAsync(partnerId);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the request
                Console.WriteLine($"Failed to sync rating: {ex.Message}");
            }
        }
        
        return Ok(ApiResponse<List<RestaurantListDto>>.Ok(list));
    }

    /// <summary>Partner: Register a new restaurant (pending Admin approval)</summary>
    [HttpPost]
    [Authorize(Roles = "Partner")]
    public async Task<IActionResult> Create([FromBody] CreateRestaurantDto dto)
    {
        try
        {
            var partnerId = GetUserId();
            var id = await _catalogService.CreateRestaurantAsync(dto, partnerId);
            return CreatedAtAction(nameof(GetById), new { id },
                ApiResponse<Guid>.Ok(id, "Restaurant submitted for approval."));
        }
        catch (InvalidOperationException ex)
        {
            // Return 400 Bad Request with the detailed error message
            return BadRequest(ApiResponse<Guid>.Fail(ex.Message));
        }
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

    /// <summary>Admin: Update restaurant status (Approved/Disabled/Pending)</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateRestaurantStatusRequest request)
    {
        try
        {
            await _catalogService.UpdateRestaurantStatusAsync(id, request.Status);
            var restaurant = await _catalogService.GetRestaurantWithMenuAsync(id);
            return Ok(ApiResponse<RestaurantDetailDto>.Ok(restaurant, $"Restaurant status updated to {request.Status}."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<RestaurantDetailDto>.Fail(ex.Message));
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

    /// <summary>Admin: Get restaurant details including unapproved/deleted</summary>
    [HttpGet("admin/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetByIdAdmin(Guid id)
    {
        var detail = await _catalogService.GetRestaurantWithMenuAdminAsync(id);
        if (detail is null)
            return NotFound(ApiResponse<RestaurantDetailDto>.Fail("Restaurant not found."));
        return Ok(ApiResponse<RestaurantDetailDto>.Ok(detail));
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

    /// <summary>Admin: Restore a soft-deleted restaurant</summary>
    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Restore(Guid id, [FromBody] RestoreRestaurantRequest request)
    {
        try
        {
            await _catalogService.RestoreRestaurantAsync(id, GetUserId(), request.Reason);
            return Ok(ApiResponse<string>.Ok("Restaurant restored successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            // Return 400 Bad Request with the detailed error message
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>Admin: Permanently delete a soft-deleted restaurant</summary>
    [HttpDelete("{id:guid}/permanent")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PermanentlyDelete(Guid id)
    {
        try
        {
            await _catalogService.PermanentlyDeleteRestaurantAsync(id, GetUserId(), GetRole());
            return Ok(ApiResponse<string>.Ok("Restaurant permanently deleted."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>Admin/Partner: Sync restaurant rating from order ratings</summary>
    [HttpPost("{id:guid}/sync-rating")]
    [Authorize(Roles = "Partner,Admin")]
    public async Task<IActionResult> SyncRating(Guid id)
    {
        try
        {
            // Call OrderService to get rating statistics
            var httpClient = new HttpClient();
            var ordersResponse = await httpClient.GetAsync($"http://localhost:5003/api/orders/restaurant/{id}");
            
            if (!ordersResponse.IsSuccessStatusCode)
            {
                return BadRequest(ApiResponse<string>.Fail("Failed to fetch orders from OrderService."));
            }

            var ordersContent = await ordersResponse.Content.ReadAsStringAsync();
            var ordersData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(ordersContent);
            
            // Calculate average rating from orders
            double totalRating = 0;
            int ratedOrdersCount = 0;
            
            if (ordersData.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var order in dataElement.EnumerateArray())
                {
                    if (order.TryGetProperty("rating", out var ratingElement) && ratingElement.ValueKind != System.Text.Json.JsonValueKind.Null)
                    {
                        if (ratingElement.TryGetProperty("foodRating", out var foodRatingElement))
                        {
                            var foodRating = foodRatingElement.GetDouble();
                            var deliveryRating = foodRating; // Default to food rating
                            
                            if (ratingElement.TryGetProperty("deliveryRating", out var deliveryRatingElement))
                            {
                                deliveryRating = deliveryRatingElement.GetDouble();
                            }
                            
                            var avgOrderRating = (foodRating + deliveryRating) / 2.0;
                            totalRating += avgOrderRating;
                            ratedOrdersCount++;
                        }
                    }
                }
            }
            
            // Update restaurant rating
            var restaurant = await _catalogService.GetRestaurantWithMenuAsync(id);
            if (restaurant == null)
            {
                return NotFound(ApiResponse<string>.Fail("Restaurant not found."));
            }
            
            double newRating = ratedOrdersCount > 0 ? Math.Round(totalRating / ratedOrdersCount, 1) : 0.0;
            
            // Update the rating in database
            await _catalogService.UpdateRestaurantRatingAsync(id, newRating);
            
            return Ok(ApiResponse<object>.Ok(new { 
                restaurantId = id,
                oldRating = restaurant.Rating,
                newRating = newRating,
                ratedOrdersCount = ratedOrdersCount
            }, $"Restaurant rating synced successfully. New rating: {newRating} (from {ratedOrdersCount} rated orders)"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.Fail($"Failed to sync rating: {ex.Message}"));
        }
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
    private string GetRole() => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    // Helper method to sync rating for a single restaurant
    private async Task SyncRestaurantRatingAsync(Guid restaurantId)
    {
        try
        {
            Console.WriteLine($"[RATING SYNC] Starting sync for restaurant {restaurantId}");
            var httpClient = new HttpClient();
            // Use internal endpoint that doesn't require authentication
            var ordersResponse = await httpClient.GetAsync($"http://localhost:5003/api/orders/internal/restaurant/{restaurantId}/orders");
            
            Console.WriteLine($"[RATING SYNC] Orders API response status: {ordersResponse.StatusCode}");
            
            if (ordersResponse.IsSuccessStatusCode)
            {
                var ordersContent = await ordersResponse.Content.ReadAsStringAsync();
                var ordersData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(ordersContent);
                
                double totalRating = 0;
                int ratedOrdersCount = 0;
                
                if (ordersData.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    Console.WriteLine($"[RATING SYNC] Found {dataElement.GetArrayLength()} orders");
                    foreach (var order in dataElement.EnumerateArray())
                    {
                        if (order.TryGetProperty("rating", out var ratingElement) && ratingElement.ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            if (ratingElement.TryGetProperty("foodRating", out var foodRatingElement))
                            {
                                var foodRating = foodRatingElement.GetDouble();
                                var deliveryRating = foodRating;
                                
                                if (ratingElement.TryGetProperty("deliveryRating", out var deliveryRatingElement))
                                {
                                    deliveryRating = deliveryRatingElement.GetDouble();
                                }
                                
                                var avgOrderRating = (foodRating + deliveryRating) / 2.0;
                                totalRating += avgOrderRating;
                                ratedOrdersCount++;
                                Console.WriteLine($"[RATING SYNC] Order rating: Food={foodRating}, Delivery={deliveryRating}, Avg={avgOrderRating}");
                            }
                        }
                    }
                }
                
                Console.WriteLine($"[RATING SYNC] Total rated orders: {ratedOrdersCount}, Total rating: {totalRating}");
                
                if (ratedOrdersCount > 0)
                {
                    double newRating = Math.Round(totalRating / ratedOrdersCount, 1);
                    Console.WriteLine($"[RATING SYNC] Updating restaurant {restaurantId} rating to {newRating}");
                    await _catalogService.UpdateRestaurantRatingAsync(restaurantId, newRating);
                    Console.WriteLine($"[RATING SYNC] ✅ Rating updated successfully");
                }
                else
                {
                    Console.WriteLine($"[RATING SYNC] No rated orders found for restaurant {restaurantId}");
                }
            }
            else
            {
                Console.WriteLine($"[RATING SYNC] ❌ Failed to fetch orders: {ordersResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RATING SYNC] ❌ Exception for restaurant {restaurantId}: {ex.Message}");
            Console.WriteLine($"[RATING SYNC] Stack trace: {ex.StackTrace}");
        }
    }

    // Helper method to sync ratings for multiple restaurants
    private async Task SyncAllRestaurantRatingsAsync(List<RestaurantListDto> restaurants)
    {
        var tasks = restaurants.Select(r => SyncRestaurantRatingAsync(r.Id));
        await Task.WhenAll(tasks);
    }
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


// Request DTOs
public class UpdateRestaurantStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class RestoreRestaurantRequest
{
    public string Reason { get; set; } = string.Empty;
}
