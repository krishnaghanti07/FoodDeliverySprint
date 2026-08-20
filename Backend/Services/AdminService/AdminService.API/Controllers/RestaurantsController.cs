using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AdminService.API.Controllers;

[ApiController]
[Route("api/admin/restaurants")]
[Authorize(Roles = "Admin")]
public class RestaurantsController : ControllerBase
{
    private readonly IAdminRestaurantService _restaurantService;
    private readonly ILogger<RestaurantsController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public RestaurantsController(
        IAdminRestaurantService restaurantService,
        ILogger<RestaurantsController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _restaurantService = restaurantService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    private Guid GetAdminId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private HttpClient CreateAuthenticatedClient()
    {
        var httpClient = _httpClientFactory.CreateClient();
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", authHeader.ToString());
        }
        return httpClient;
    }

    /// <summary>
    /// GET /api/admin/restaurants - List all restaurants with filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<RestaurantListDto>>> GetAllRestaurants(
        [FromQuery] string? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        try
        {
            var catalogServiceUrl = _configuration["Services:CatalogService"] ?? "http://localhost:5002";
            var httpClient = CreateAuthenticatedClient();
            
            var response = await httpClient.GetAsync($"{catalogServiceUrl}/api/catalog/restaurants/admin/all");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch restaurants from CatalogService: {StatusCode}", response.StatusCode);
                return StatusCode(500, new { error = "Failed to retrieve restaurants" });
            }

            var content = await response.Content.ReadAsStringAsync();
            var restaurants = System.Text.Json.JsonSerializer.Deserialize<List<RestaurantListDto>>(content,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _logger.LogInformation("Admin {AdminId} retrieved {Count} restaurants", GetAdminId(), restaurants?.Count ?? 0);
            return Ok(restaurants ?? new List<RestaurantListDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving restaurants");
            return StatusCode(500, new { error = "Failed to retrieve restaurants" });
        }
    }

    /// <summary>
    /// GET /api/admin/restaurants/{id} - View restaurant details
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<RestaurantDetailDto>> GetRestaurantById(Guid id)
    {
        try
        {
            var catalogServiceUrl = _configuration["Services:CatalogService"] ?? "http://localhost:5002";
            var httpClient = CreateAuthenticatedClient();
            
            // Use admin-specific endpoint that doesn't filter by approval status
            var response = await httpClient.GetAsync($"{catalogServiceUrl}/api/catalog/restaurants/admin/{id}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Restaurant {RestaurantId} not found", id);
                return NotFound(new { error = $"Restaurant {id} not found" });
            }
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch restaurant from CatalogService: {StatusCode}", response.StatusCode);
                return StatusCode(500, new { error = "Failed to retrieve restaurant" });
            }

            var content = await response.Content.ReadAsStringAsync();
            
            // Parse the ApiResponse wrapper
            var apiResponse = System.Text.Json.JsonSerializer.Deserialize<ApiResponseWrapper>(content,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _logger.LogInformation("Admin {AdminId} viewed restaurant {RestaurantId}", GetAdminId(), id);
            return Ok(apiResponse?.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving restaurant {RestaurantId}", id);
            return StatusCode(500, new { error = "Failed to retrieve restaurant" });
        }
    }

    // Helper class to deserialize ApiResponse<RestaurantDetailDto>
    private class ApiResponseWrapper
    {
        public RestaurantDetailDto? Data { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// PATCH /api/admin/restaurants/{id}/approve - Approve pending restaurant
    /// </summary>
    [HttpPatch("{id}/approve")]
    public async Task<ActionResult<RestaurantDetailDto>> ApproveRestaurant(
        Guid id,
        [FromBody] ApproveRestaurantDto dto)
    {
        try
        {
            var catalogServiceUrl = _configuration["Services:CatalogService"] ?? "http://localhost:5002";
            var httpClient = CreateAuthenticatedClient();
            
            var response = await httpClient.PatchAsync($"{catalogServiceUrl}/api/catalog/restaurants/{id}/approve", null);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { error = $"Restaurant {id} not found" });
            }
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to approve restaurant: {Error}", errorContent);
                return StatusCode((int)response.StatusCode, new { error = "Failed to approve restaurant" });
            }

            _logger.LogInformation("Admin {AdminId} approved restaurant {RestaurantId}", GetAdminId(), id);
            return Ok(new { message = "Restaurant approved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving restaurant {RestaurantId}", id);
            return StatusCode(500, new { error = "Failed to approve restaurant" });
        }
    }

    /// <summary>
    /// PATCH /api/admin/restaurants/{id}/status - Update restaurant status (Approved/Disabled)
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<RestaurantDetailDto>> UpdateRestaurantStatus(
        Guid id,
        [FromBody] UpdateRestaurantStatusDto dto)
    {
        try
        {
            var catalogServiceUrl = _configuration["Services:CatalogService"] ?? "http://localhost:5002";
            var httpClient = CreateAuthenticatedClient();
            
            var payload = new
            {
                status = dto.Status,
                reason = dto.Reason
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PatchAsync($"{catalogServiceUrl}/api/catalog/restaurants/{id}/status", content);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Restaurant {RestaurantId} not found", id);
                return NotFound(new { error = $"Restaurant {id} not found" });
            }
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update restaurant status: {Error}", errorContent);
                return StatusCode((int)response.StatusCode, new { error = "Failed to update restaurant status", details = errorContent });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Admin {AdminId} updated restaurant {RestaurantId} status to {Status}", 
                GetAdminId(), id, dto.Status);
            return Ok(new { message = $"Restaurant status updated to {dto.Status}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating restaurant {RestaurantId} status", id);
            return StatusCode(500, new { error = "Failed to update restaurant status" });
        }
    }

    /// <summary>
    /// POST /api/admin/restaurants/{id}/reject - Reject pending restaurant
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<ActionResult> RejectRestaurant(
        Guid id,
        [FromBody] RejectRestaurantDto dto)
    {
        try
        {
            var adminId = GetAdminId();
            await _restaurantService.RejectRestaurantAsync(id, dto, adminId);
            _logger.LogInformation("Admin {AdminId} rejected restaurant {RestaurantId}", adminId, id);
            return Ok(new { message = "Restaurant rejected successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting restaurant {RestaurantId}", id);
            return StatusCode(500, new { error = "Failed to reject restaurant" });
        }
    }

    /// <summary>
    /// POST /api/admin/restaurants/{id}/toggle-active - Toggle restaurant active status
    /// </summary>
    [HttpPost("{id}/toggle-active")]
    public async Task<ActionResult<RestaurantDetailDto>> ToggleRestaurantActive(
        Guid id,
        [FromBody] ToggleActiveDto dto)
    {
        try
        {
            var adminId = GetAdminId();
            var restaurant = await _restaurantService.ToggleRestaurantActiveAsync(id, dto, adminId);
            _logger.LogInformation("Admin {AdminId} toggled restaurant {RestaurantId} active status", adminId, id);
            return Ok(restaurant);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling restaurant {RestaurantId} active status", id);
            return StatusCode(500, new { error = "Failed to toggle restaurant status" });
        }
    }

    /// <summary>
    /// DELETE /api/admin/restaurants/{id} - Soft delete restaurant
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> SoftDeleteRestaurant(
        Guid id,
        [FromBody] SoftDeleteDto dto)
    {
        try
        {
            var catalogServiceUrl = _configuration["Services:CatalogService"] ?? "http://localhost:5002";
            var httpClient = CreateAuthenticatedClient();
            
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{catalogServiceUrl}/api/catalog/restaurants/{id}");
            
            var response = await httpClient.SendAsync(request);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { error = $"Restaurant {id} not found" });
            }
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete restaurant {RestaurantId}: {Error}", id, errorContent);
                return StatusCode((int)response.StatusCode, new { error = "Failed to delete restaurant", details = errorContent });
            }

            _logger.LogInformation("Admin {AdminId} deleted restaurant {RestaurantId}", GetAdminId(), id);
            return Ok(new { message = "Restaurant deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting restaurant {RestaurantId}", id);
            return StatusCode(500, new { error = "Failed to delete restaurant" });
        }
    }

    /// <summary>
    /// POST /api/admin/restaurants/{id}/restore - Restore soft-deleted restaurant
    /// </summary>
    [HttpPost("{id}/restore")]
    public async Task<ActionResult> RestoreRestaurant(
        Guid id,
        [FromBody] RestoreRestaurantDto dto)
    {
        try
        {
            var catalogServiceUrl = _configuration["Services:CatalogService"] ?? "http://localhost:5002";
            var httpClient = CreateAuthenticatedClient();
            
            var payload = new
            {
                reason = dto.Reason
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync($"{catalogServiceUrl}/api/catalog/restaurants/{id}/restore", content);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { error = $"Restaurant {id} not found" });
            }
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to restore restaurant {RestaurantId}: {Error}", id, errorContent);
                
                // Try to extract the actual error message from the response
                try
                {
                    var errorResponse = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(errorContent,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    var errorMessage = errorResponse?.Message ?? errorResponse?.Error ?? "Failed to restore restaurant";
                    return StatusCode((int)response.StatusCode, new { error = errorMessage });
                }
                catch
                {
                    return StatusCode((int)response.StatusCode, new { error = "Failed to restore restaurant", details = errorContent });
                }
            }

            _logger.LogInformation("Admin {AdminId} restored restaurant {RestaurantId}", GetAdminId(), id);
            return Ok(new { message = "Restaurant restored successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring restaurant {RestaurantId}", id);
            return StatusCode(500, new { error = "Failed to restore restaurant" });
        }
    }

    /// <summary>
    /// DELETE /api/admin/restaurants/{id}/permanent - Permanently delete a soft-deleted restaurant
    /// </summary>
    [HttpDelete("{id}/permanent")]
    public async Task<ActionResult> PermanentlyDeleteRestaurant(Guid id)
    {
        try
        {
            var catalogServiceUrl = _configuration["Services:CatalogService"] ?? "http://localhost:5002";
            var httpClient = CreateAuthenticatedClient();
            
            var response = await httpClient.DeleteAsync($"{catalogServiceUrl}/api/catalog/restaurants/{id}/permanent");
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { error = $"Restaurant {id} not found" });
            }
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to permanently delete restaurant {RestaurantId}: {Error}", id, errorContent);
                
                // Try to extract the actual error message
                try
                {
                    var errorResponse = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(errorContent,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    var errorMessage = errorResponse?.Message ?? errorResponse?.Error ?? "Failed to permanently delete restaurant";
                    return StatusCode((int)response.StatusCode, new { error = errorMessage });
                }
                catch
                {
                    return StatusCode((int)response.StatusCode, new { error = "Failed to permanently delete restaurant", details = errorContent });
                }
            }

            _logger.LogInformation("Admin {AdminId} permanently deleted restaurant {RestaurantId}", GetAdminId(), id);
            return Ok(new { message = "Restaurant permanently deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error permanently deleting restaurant {RestaurantId}", id);
            return StatusCode(500, new { error = "Failed to permanently delete restaurant" });
        }
    }

    // Helper class to deserialize error responses
    private class ErrorResponse
    {
        public string? Error { get; set; }
        public string? Message { get; set; }
    }
}
