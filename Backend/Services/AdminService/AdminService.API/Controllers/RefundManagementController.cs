using System.Security.Claims;
using System.Text.Json;
using AdminService.Application.DTOs;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.API.Controllers;

[ApiController]
[Route("api/admin/refunds")]
[Authorize(Roles = "Admin")]
public class RefundManagementController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RefundManagementController> _logger;

    public RefundManagementController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<RefundManagementController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Get all pending refund requests</summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingRefunds()
    {
        try
        {
            var orderServiceUrl = _configuration["Services:OrderService"] ?? "http://localhost:5003";
            var authServiceUrl = _configuration["Services:AuthService"] ?? "http://localhost:5001";
            var client = _httpClientFactory.CreateClient();

            // Get all refund requests from OrderService
            var response = await client.GetAsync($"{orderServiceUrl}/api/refunds/pending");
            
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, 
                    ApiResponse<object>.Fail("Failed to fetch refund requests from OrderService."));
            }

            var content = await response.Content.ReadAsStringAsync();
            var refundRequests = JsonSerializer.Deserialize<List<RefundRequestDto>>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<RefundRequestDto>();

            // Enrich with customer details from AuthService
            foreach (var refund in refundRequests)
            {
                try
                {
                    var userResponse = await client.GetAsync($"{authServiceUrl}/api/admin/users/{refund.CustomerId}");
                    if (userResponse.IsSuccessStatusCode)
                    {
                        var userContent = await userResponse.Content.ReadAsStringAsync();
                        var userDoc = JsonDocument.Parse(userContent);
                        
                        if (userDoc.RootElement.TryGetProperty("data", out var userData))
                        {
                            refund.CustomerName = userData.GetProperty("fullName").GetString() ?? "Unknown";
                            refund.CustomerEmail = userData.GetProperty("email").GetString() ?? "Unknown";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch customer details for refund {RefundId}", refund.Id);
                }
            }

            return Ok(ApiResponse<List<RefundRequestDto>>.Ok(refundRequests));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending refunds");
            return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
        }
    }

    /// <summary>Get all refund requests (with optional status filter)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllRefunds([FromQuery] string? status = null)
    {
        try
        {
            var orderServiceUrl = _configuration["Services:OrderService"] ?? "http://localhost:5003";
            var authServiceUrl = _configuration["Services:AuthService"] ?? "http://localhost:5001";
            var client = _httpClientFactory.CreateClient();

            var url = string.IsNullOrEmpty(status) 
                ? $"{orderServiceUrl}/api/refunds" 
                : $"{orderServiceUrl}/api/refunds?status={status}";

            var response = await client.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, 
                    ApiResponse<object>.Fail("Failed to fetch refund requests from OrderService."));
            }

            var content = await response.Content.ReadAsStringAsync();
            var refundRequests = JsonSerializer.Deserialize<List<RefundRequestDto>>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<RefundRequestDto>();

            // Enrich with customer details
            foreach (var refund in refundRequests)
            {
                try
                {
                    var userResponse = await client.GetAsync($"{authServiceUrl}/api/admin/users/{refund.CustomerId}");
                    if (userResponse.IsSuccessStatusCode)
                    {
                        var userContent = await userResponse.Content.ReadAsStringAsync();
                        var userDoc = JsonDocument.Parse(userContent);
                        
                        if (userDoc.RootElement.TryGetProperty("data", out var userData))
                        {
                            refund.CustomerName = userData.GetProperty("fullName").GetString() ?? "Unknown";
                            refund.CustomerEmail = userData.GetProperty("email").GetString() ?? "Unknown";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch customer details for refund {RefundId}", refund.Id);
                }
            }

            return Ok(ApiResponse<List<RefundRequestDto>>.Ok(refundRequests));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching refunds");
            return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
        }
    }

    /// <summary>Process refund request (approve or reject)</summary>
    [HttpPost("{id}/process")]
    public async Task<IActionResult> ProcessRefund(Guid id, [FromBody] ProcessRefundRequestDto dto)
    {
        try
        {
            var adminId = GetUserId();
            var orderServiceUrl = _configuration["Services:OrderService"] ?? "http://localhost:5003";
            var authServiceUrl = _configuration["Services:AuthService"] ?? "http://localhost:5001";
            var client = _httpClientFactory.CreateClient();

            // Validate action
            if (dto.Action.ToLower() != "approve" && dto.Action.ToLower() != "reject")
            {
                return BadRequest(ApiResponse<object>.Fail("Action must be 'Approve' or 'Reject'."));
            }

            // Get refund request details
            var refundResponse = await client.GetAsync($"{orderServiceUrl}/api/refunds/{id}");
            if (!refundResponse.IsSuccessStatusCode)
            {
                return NotFound(ApiResponse<object>.Fail("Refund request not found."));
            }

            var refundContent = await refundResponse.Content.ReadAsStringAsync();
            var refund = JsonSerializer.Deserialize<RefundRequestDto>(refundContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (refund == null)
            {
                return NotFound(ApiResponse<object>.Fail("Refund request not found."));
            }

            // Process refund in OrderService
            var processPayload = new
            {
                action = dto.Action,
                adminNotes = dto.AdminNotes,
                processedBy = adminId
            };

            var processResponse = await client.PostAsJsonAsync(
                $"{orderServiceUrl}/api/refunds/{id}/process", 
                processPayload);

            if (!processResponse.IsSuccessStatusCode)
            {
                var errorContent = await processResponse.Content.ReadAsStringAsync();
                return StatusCode((int)processResponse.StatusCode, 
                    ApiResponse<object>.Fail($"Failed to process refund: {errorContent}"));
            }

            // If approved, credit wallet in AuthService
            if (dto.Action.ToLower() == "approve")
            {
                var walletPayload = new
                {
                    userId = refund.CustomerId,
                    amount = refund.RefundAmount,  // Use RefundAmount instead of Amount
                    source = "Refund",
                    referenceId = refund.OrderId,
                    description = $"Refund for cancelled order {refund.OrderId}"
                };

                var walletResponse = await client.PostAsJsonAsync(
                    $"{authServiceUrl}/api/admin/wallet/credit", 
                    walletPayload);

                if (!walletResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to credit wallet for refund {RefundId}", id);
                    return StatusCode(500, ApiResponse<object>.Fail("Refund processed but wallet credit failed. Please credit manually."));
                }
            }

            var message = dto.Action.ToLower() == "approve" 
                ? "Refund approved and wallet credited successfully." 
                : "Refund rejected successfully.";

            return Ok(ApiResponse<object>.Ok(new { refundId = id, status = dto.Action }, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund {RefundId}", id);
            return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
        }
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
