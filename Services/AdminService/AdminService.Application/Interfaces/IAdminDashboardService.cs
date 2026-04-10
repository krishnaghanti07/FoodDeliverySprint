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