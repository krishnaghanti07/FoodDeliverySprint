using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AdminService.API.Controllers;

[ApiController]
[Route("api/admin/notifications")]
[Authorize(Roles = "Admin")]
public class NotificationsController : ControllerBase
{
    private readonly IAdminNotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        IAdminNotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    private Guid GetAdminId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// POST /api/admin/notifications/send - Send notification to users
    /// </summary>
    [HttpPost("send")]
    public async Task<ActionResult<NotificationHistoryDto>> SendNotification(
        [FromBody] SendNotificationDto dto)
    {
        try
        {
            var adminId = GetAdminId();
            var notification = await _notificationService.SendNotificationAsync(dto, adminId);
            _logger.LogInformation("Admin {AdminId} sent notification to {Recipients}: {Title}", 
                adminId, dto.Recipients, dto.Title);
            return Ok(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            return StatusCode(500, new { error = "Failed to send notification" });
        }
    }

    /// <summary>
    /// GET /api/admin/notifications/history - View notification history
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<List<NotificationHistoryDto>>> GetNotificationHistory(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? type)
    {
        try
        {
            var notifications = await _notificationService.GetNotificationHistoryAsync(from, to, type);
            _logger.LogInformation("Admin {AdminId} retrieved {Count} notification history records", 
                GetAdminId(), notifications.Count);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification history");
            return StatusCode(500, new { error = "Failed to retrieve notification history" });
        }
    }
}
