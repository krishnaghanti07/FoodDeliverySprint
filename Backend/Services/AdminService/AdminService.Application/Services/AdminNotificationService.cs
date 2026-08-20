using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;

namespace AdminService.Application.Services;

public class AdminNotificationService : IAdminNotificationService
{
    private readonly INotificationHistoryRepository _notificationRepo;
    private readonly IUserSnapshotRepository _userRepo;

    public AdminNotificationService(
        INotificationHistoryRepository notificationRepo,
        IUserSnapshotRepository userRepo)
    {
        _notificationRepo = notificationRepo;
        _userRepo = userRepo;
    }

    public async Task<NotificationHistoryDto> SendNotificationAsync(SendNotificationDto dto, Guid adminId)
    {
        // Calculate total recipients based on the recipients filter
        int totalRecipients = 0;
        
        if (dto.Recipients.ToLower() == "all")
        {
            var allUsers = await _userRepo.GetAllAsync(null, true);
            totalRecipients = allUsers.Count;
        }
        else if (dto.Recipients.ToLower() == "customers")
        {
            var customers = await _userRepo.GetAllAsync("Customer", true);
            totalRecipients = customers.Count;
        }
        else if (dto.Recipients.ToLower() == "partners")
        {
            var partners = await _userRepo.GetAllAsync("Partner", true);
            totalRecipients = partners.Count;
        }
        else if (dto.Recipients.ToLower() == "agents")
        {
            var agents = await _userRepo.GetAllAsync("DeliveryAgent", true);
            totalRecipients = agents.Count;
        }
        else
        {
            // Assume it's a JSON array of user IDs or comma-separated list
            totalRecipients = dto.Recipients.Split(',').Length;
        }

        var notification = new NotificationHistory
        {
            SentBy = adminId,
            Recipients = dto.Recipients,
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            TotalRecipients = totalRecipients,
            SentAt = DateTime.UtcNow
        };

        await _notificationRepo.AddAsync(notification);
        await _notificationRepo.SaveChangesAsync();

        // TODO: Integrate with actual notification service (email/SMS/push)
        // For now, we just log the notification to history

        return new NotificationHistoryDto
        {
            Id = notification.Id,
            Recipients = notification.Recipients,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            TotalRecipients = notification.TotalRecipients,
            SentAt = notification.SentAt
        };
    }

    public async Task<List<NotificationHistoryDto>> GetNotificationHistoryAsync(DateTime? from, DateTime? to, string? type)
    {
        var notifications = await _notificationRepo.GetHistoryAsync(from, to, type);
        return notifications.Select(n => new NotificationHistoryDto
        {
            Id = n.Id,
            Recipients = n.Recipients,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            TotalRecipients = n.TotalRecipients,
            SentAt = n.SentAt
        }).ToList();
    }
}
