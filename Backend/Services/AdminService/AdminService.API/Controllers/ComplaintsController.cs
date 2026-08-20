using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AdminService.API.Controllers;

[ApiController]
[Route("api/admin/complaints")]
[Authorize(Roles = "Admin")]
public class ComplaintsController : ControllerBase
{
    private readonly IAdminComplaintService _complaintService;
    private readonly ILogger<ComplaintsController> _logger;

    public ComplaintsController(
        IAdminComplaintService complaintService,
        ILogger<ComplaintsController> logger)
    {
        _complaintService = complaintService;
        _logger = logger;
    }

    private Guid GetAdminId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// GET /api/admin/complaints - List all complaints with filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ComplaintListDto>>> GetAllComplaints(
        [FromQuery] string? status,
        [FromQuery] string? type)
    {
        try
        {
            var complaints = await _complaintService.GetAllComplaintsAsync(status, type);
            _logger.LogInformation("Admin {AdminId} retrieved {Count} complaints", GetAdminId(), complaints.Count);
            return Ok(complaints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving complaints");
            return StatusCode(500, new { error = "Failed to retrieve complaints" });
        }
    }

    /// <summary>
    /// GET /api/admin/complaints/{id} - View complaint details
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ComplaintDetailDto>> GetComplaintById(Guid id)
    {
        try
        {
            var complaint = await _complaintService.GetComplaintByIdAsync(id);
            if (complaint is null)
            {
                _logger.LogWarning("Complaint {ComplaintId} not found", id);
                return NotFound(new { error = $"Complaint {id} not found" });
            }

            _logger.LogInformation("Admin {AdminId} viewed complaint {ComplaintId}", GetAdminId(), id);
            return Ok(complaint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving complaint {ComplaintId}", id);
            return StatusCode(500, new { error = "Failed to retrieve complaint" });
        }
    }

    /// <summary>
    /// POST /api/admin/complaints/{id}/resolve - Resolve complaint
    /// </summary>
    [HttpPost("{id}/resolve")]
    public async Task<ActionResult<ComplaintDetailDto>> ResolveComplaint(
        Guid id,
        [FromBody] ResolveComplaintDto dto)
    {
        try
        {
            var adminId = GetAdminId();
            var complaint = await _complaintService.ResolveComplaintAsync(id, dto, adminId);
            _logger.LogInformation("Admin {AdminId} resolved complaint {ComplaintId}", adminId, id);
            return Ok(complaint);
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
            _logger.LogError(ex, "Error resolving complaint {ComplaintId}", id);
            return StatusCode(500, new { error = "Failed to resolve complaint" });
        }
    }
}
