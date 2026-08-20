using AdminService.Application.DTOs;

namespace AdminService.Application.Interfaces;

public interface IAdminDashboardService
{
    Task<DashboardDto> GetDashboardAsync();
}

public interface IAdminUserService
{
    Task<List<UserSummaryDto>> GetAllUsersAsync(string? role, bool? isActive);
    Task<UserSummaryDto?> GetUserByIdAsync(Guid id);
    Task ToggleUserStatusAsync(Guid id, ToggleUserStatusDto dto, Guid adminId);
}

public interface IAdminOrderService
{
    Task<List<AdminOrderDto>> GetAllOrdersAsync(
        string? status, Guid? restaurantId, DateTime? from, DateTime? to);
    Task<AdminOrderDto?> GetOrderByIdAsync(Guid id);
    Task<AdminOrderDto> UpdateOrderStatusAsync(
        Guid orderId, AdminUpdateOrderStatusDto dto, Guid adminId);
}

public interface IAdminReportService
{
    Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to);
    Task<PartnerReportDto> GetPartnerReportAsync(DateTime from, DateTime to);
}


public interface IAdminRestaurantService
{
    Task<List<RestaurantListDto>> GetAllRestaurantsAsync(string? status, int? page, int? pageSize);
    Task<RestaurantDetailDto?> GetRestaurantByIdAsync(Guid id);
    Task<RestaurantDetailDto> ApproveRestaurantAsync(Guid id, ApproveRestaurantDto dto, Guid adminId);
    Task<RestaurantDetailDto> UpdateRestaurantStatusAsync(Guid id, UpdateRestaurantStatusDto dto, Guid adminId);
    Task RejectRestaurantAsync(Guid id, RejectRestaurantDto dto, Guid adminId);
    Task<RestaurantDetailDto> ToggleRestaurantActiveAsync(Guid id, ToggleActiveDto dto, Guid adminId);
}

public interface IAdminDeliveryAgentService
{
    Task<List<DeliveryAgentListDto>> GetAllAgentsAsync(bool? isActive, bool? isOnline, int? page, int? pageSize);
    Task<DeliveryAgentDetailDto?> GetAgentByIdAsync(Guid id);
    Task UpdateAgentStatusAsync(Guid id, UpdateAgentStatusDto dto, Guid adminId);
    Task ApproveAgentAsync(Guid id, ApproveAgentDto dto, Guid adminId);
    Task RejectAgentAsync(Guid id, RejectAgentDto dto, Guid adminId);
    Task<List<DeliveryAgentListDto>> GetPendingAgentsAsync();
}

public interface IAdminComplaintService
{
    Task<List<ComplaintListDto>> GetAllComplaintsAsync(string? status, string? type);
    Task<ComplaintDetailDto?> GetComplaintByIdAsync(Guid id);
    Task<ComplaintDetailDto> ResolveComplaintAsync(Guid id, ResolveComplaintDto dto, Guid adminId);
}

public interface IAdminNotificationService
{
    Task<NotificationHistoryDto> SendNotificationAsync(SendNotificationDto dto, Guid adminId);
    Task<List<NotificationHistoryDto>> GetNotificationHistoryAsync(DateTime? from, DateTime? to, string? type);
}
