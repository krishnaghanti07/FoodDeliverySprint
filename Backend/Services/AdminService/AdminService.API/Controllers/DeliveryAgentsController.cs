using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace AdminService.API.Controllers;

// DTO for AuthService User response
public class UserProfileDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }
    
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
    
    [JsonPropertyName("isApproved")]
    public bool IsApproved { get; set; }
    
    [JsonPropertyName("registeredAt")]
    public DateTime RegisteredAt { get; set; }
}

[ApiController]
[Route("api/admin/delivery-agents")]
[Authorize(Roles = "Admin")]
public class DeliveryAgentsController : ControllerBase
{
    private readonly IAdminDeliveryAgentService _agentService;
    private readonly ILogger<DeliveryAgentsController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public DeliveryAgentsController(
        IAdminDeliveryAgentService agentService,
        ILogger<DeliveryAgentsController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _agentService = agentService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    private Guid GetAdminId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// GET /api/admin/delivery-agents - List all delivery agents with filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<DeliveryAgentListDto>>> GetAllAgents(
        [FromQuery] bool? isActive,
        [FromQuery] bool? isOnline,
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        try
        {
            var agents = await _agentService.GetAllAgentsAsync(isActive, isOnline, page, pageSize);
            _logger.LogInformation("Admin {AdminId} retrieved {Count} delivery agents", GetAdminId(), agents.Count);
            return Ok(agents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery agents");
            return StatusCode(500, new { error = "Failed to retrieve delivery agents" });
        }
    }

    /// <summary>
    /// GET /api/admin/delivery-agents/{id} - View delivery agent details
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DeliveryAgentDetailDto>> GetAgentById(Guid id)
    {
        try
        {
            var agent = await _agentService.GetAgentByIdAsync(id);
            if (agent is null)
            {
                _logger.LogWarning("Delivery agent {AgentId} not found", id);
                return NotFound(new { error = $"Delivery agent {id} not found" });
            }

            _logger.LogInformation("Admin {AdminId} viewed delivery agent {AgentId}", GetAdminId(), id);
            return Ok(agent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery agent {AgentId}", id);
            return StatusCode(500, new { error = "Failed to retrieve delivery agent" });
        }
    }

    /// <summary>
    /// PATCH /api/admin/delivery-agents/{id}/status - Activate/Deactivate delivery agent
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateAgentStatus(
        Guid id,
        [FromBody] UpdateAgentStatusDto dto)
    {
        try
        {
            var adminId = GetAdminId();
            await _agentService.UpdateAgentStatusAsync(id, dto, adminId);
            _logger.LogInformation("Admin {AdminId} updated delivery agent {AgentId} status to {IsActive}", 
                adminId, id, dto.IsActive);
            return Ok(new { message = $"Delivery agent status updated to {(dto.IsActive ? "Active" : "Inactive")}" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating delivery agent {AgentId} status", id);
            return StatusCode(500, new { error = "Failed to update delivery agent status" });
        }
    }

    /// <summary>
    /// POST /api/admin/delivery-agents/{id}/approve - Approve pending delivery agent
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveAgent(
        Guid id,
        [FromBody] ApproveAgentDto dto)
    {
        try
        {
            var adminId = GetAdminId();
            await _agentService.ApproveAgentAsync(id, dto, adminId);
            _logger.LogInformation("Admin {AdminId} approved delivery agent {AgentId}", adminId, id);
            return Ok(new { message = "Delivery agent approved successfully" });
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
            _logger.LogError(ex, "Error approving delivery agent {AgentId}", id);
            return StatusCode(500, new { error = "Failed to approve delivery agent" });
        }
    }

    /// <summary>
    /// POST /api/admin/delivery-agents/{id}/reject - Reject pending delivery agent
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectAgent(
        Guid id,
        [FromBody] RejectAgentDto dto)
    {
        try
        {
            var adminId = GetAdminId();
            await _agentService.RejectAgentAsync(id, dto, adminId);
            _logger.LogInformation("Admin {AdminId} rejected delivery agent {AgentId}", adminId, id);
            return Ok(new { message = "Delivery agent rejected successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting delivery agent {AgentId}", id);
            return StatusCode(500, new { error = "Failed to reject delivery agent" });
        }
    }

    /// <summary>
    /// GET /api/admin/delivery-agents/pending - Get pending approval agents
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<List<DeliveryAgentListDto>>> GetPendingAgents()
    {
        try
        {
            var agents = await _agentService.GetPendingAgentsAsync();
            _logger.LogInformation("Admin {AdminId} retrieved {Count} pending delivery agents", GetAdminId(), agents.Count);
            return Ok(agents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending delivery agents");
            return StatusCode(500, new { error = "Failed to retrieve pending delivery agents" });
        }
    }

    /// <summary>
    /// POST /api/admin/delivery-agents/sync - Sync delivery agents from AuthService
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult> SyncDeliveryAgents()
    {
        try
        {
            var adminId = GetAdminId();
            
            // Call AuthService to get all delivery agents
            var authServiceUrl = _configuration["Services:AuthService"] ?? "http://localhost:5001";
            var httpClient = _httpClientFactory.CreateClient();
            
            var response = await httpClient.GetAsync($"{authServiceUrl}/api/auth/admin/users?role=DeliveryAgent");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch delivery agents from AuthService: {StatusCode}", response.StatusCode);
                return StatusCode(500, new { error = "Failed to fetch delivery agents from AuthService" });
            }

            var content = await response.Content.ReadAsStringAsync();
            var deliveryAgents = System.Text.Json.JsonSerializer.Deserialize<List<UserProfileDto>>(content, 
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (deliveryAgents == null || deliveryAgents.Count == 0)
            {
                _logger.LogInformation("No delivery agents found in AuthService");
                return Ok(new { message = "No delivery agents found to sync", syncedCount = 0, totalAgents = 0 });
            }

            int syncedCount = 0;
            int updatedCount = 0;
            var agentRepo = HttpContext.RequestServices.GetRequiredService<IDeliveryAgentSnapshotRepository>();
            var userRepo = HttpContext.RequestServices.GetRequiredService<IUserSnapshotRepository>();
            
            foreach (var user in deliveryAgents)
            {
                // Sync to UserSnapshot first
                await userRepo.UpsertAsync(new UserSnapshot
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Mobile = user.Mobile ?? string.Empty,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    RegisteredAt = user.RegisteredAt
                });

                // Check if DeliveryAgentSnapshot already exists
                var existingAgent = await agentRepo.GetByIdAsync(user.Id);
                
                if (existingAgent == null)
                {
                    // Create new snapshot
                    await agentRepo.UpsertAsync(new DeliveryAgentSnapshot
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        Mobile = user.Mobile ?? string.Empty,
                        IsApproved = user.IsApproved,
                        IsActive = user.IsActive,
                        IsOnline = false,
                        IsAvailable = false,
                        VehicleType = "",
                        TotalDeliveries = 0,
                        AverageRating = 0,
                        RegisteredAt = user.RegisteredAt,
                        UpdatedAt = DateTime.UtcNow
                    });
                    syncedCount++;
                }
                else
                {
                    // Update existing snapshot with latest data from AuthService
                    existingAgent.FullName = user.FullName;
                    existingAgent.Email = user.Email;
                    existingAgent.Mobile = user.Mobile ?? string.Empty;
                    existingAgent.IsApproved = user.IsApproved;
                    existingAgent.IsActive = user.IsActive;
                    existingAgent.UpdatedAt = DateTime.UtcNow;
                    await agentRepo.UpdateAsync(existingAgent);
                    updatedCount++;
                }
            }
            
            await userRepo.SaveChangesAsync();
            await agentRepo.SaveChangesAsync();

            _logger.LogInformation("Admin {AdminId} synced {NewCount} new and updated {UpdatedCount} existing delivery agents from AuthService", 
                adminId, syncedCount, updatedCount);
            return Ok(new { 
                message = $"Synced {syncedCount} new and updated {updatedCount} existing delivery agents successfully", 
                syncedCount, 
                updatedCount,
                totalAgents = deliveryAgents.Count 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing delivery agents");
            return StatusCode(500, new { error = "Failed to sync delivery agents", details = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/admin/delivery-agents/{id} - Soft delete delivery agent
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> SoftDeleteAgent(
        Guid id,
        [FromBody] SoftDeleteDto dto)
    {
        try
        {
            var adminId = GetAdminId();
            
            // Call AuthService to soft delete the agent
            var authServiceUrl = _configuration["Services:AuthService"] ?? "http://localhost:5001";
            var httpClient = _httpClientFactory.CreateClient();
            
            var payload = new
            {
                deletedBy = adminId,
                reason = dto.Reason
            };

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{authServiceUrl}/api/auth/admin/users/{id}")
            {
                Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(payload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                )
            };

            var response = await httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to soft delete agent {AgentId}: {Error}", id, errorContent);
                return StatusCode((int)response.StatusCode, new { error = "Failed to delete agent", details = errorContent });
            }

            _logger.LogInformation("Admin {AdminId} soft deleted delivery agent {AgentId}", adminId, id);
            return Ok(new { message = "Delivery agent deleted successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting delivery agent {AgentId}", id);
            return StatusCode(500, new { error = "Failed to delete delivery agent" });
        }
    }

    /// <summary>
    /// POST /api/admin/delivery-agents/{id}/restore - Restore soft-deleted delivery agent
    /// </summary>
    [HttpPost("{id}/restore")]
    public async Task<ActionResult> RestoreAgent(
        Guid id,
        [FromBody] RestoreAgentDto dto)
    {
        try
        {
            // Log incoming request
            _logger.LogInformation("Restore request received for agent {AgentId}, Reason length: {Length}", 
                id, dto?.Reason?.Length ?? 0);
            
            // Validate DTO
            if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))
            {
                _logger.LogWarning("Restore request validation failed: Reason is required");
                return BadRequest(new { error = "Reason is required" });
            }
            
            if (dto.Reason.Length < 10)
            {
                _logger.LogWarning("Restore request validation failed: Reason must be at least 10 characters, got {Length}", 
                    dto.Reason.Length);
                return BadRequest(new { error = "Reason must be at least 10 characters" });
            }
            
            var adminId = GetAdminId();
            
            // Call AuthService to restore the agent
            var authServiceUrl = _configuration["Services:AuthService"] ?? "http://localhost:5001";
            var httpClient = _httpClientFactory.CreateClient();
            
            var payload = new
            {
                restoredBy = adminId,
                reason = dto.Reason
            };

            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
            _logger.LogInformation("Sending restore request to AuthService: {Payload}", jsonPayload);

            var content = new StringContent(
                jsonPayload,
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync($"{authServiceUrl}/api/auth/admin/users/{id}/restore", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to restore agent {AgentId}: Status={Status}, Error={Error}", 
                    id, response.StatusCode, errorContent);
                return StatusCode((int)response.StatusCode, new { error = "Failed to restore agent", details = errorContent });
            }

            _logger.LogInformation("Admin {AdminId} restored delivery agent {AgentId}", adminId, id);
            return Ok(new { message = "Delivery agent restored successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring delivery agent {AgentId}", id);
            return StatusCode(500, new { error = "Failed to restore delivery agent" });
        }
    }
}
