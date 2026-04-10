using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;

namespace AdminService.Application.Services;

// ══════════════════════════════════════════════════════════════════════
// DASHBOARD SERVICE
// ══════════════════════════════════════════════════════════════════════
public class AdminDashboardService : IAdminDashboardService
{
    private readonly IOrderSnapshotRepository _orderRepo;
    private readonly IUserSnapshotRepository _userRepo;

    public AdminDashboardService(
        IOrderSnapshotRepository orderRepo,
        IUserSnapshotRepository userRepo)
    {
        _orderRepo = orderRepo;
        _userRepo = userRepo;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var allOrders = await _orderRepo.GetAllAsync(null, null, null, null);
        var todayOrders = allOrders.Where(o => o.PlacedAt >= today && o.PlacedAt < tomorrow).ToList();
        var allUsers = await _userRepo.GetAllAsync(null, null);

        var inProgressStatuses = new HashSet<string>
        {
            "Accepted", "Preparing", "ReadyForPickup", "PickedUp", "OutForDelivery"
        };

        var topRaw = await _orderRepo.GetTopRestaurantsAsync(5, null, null);

        return new DashboardDto
        {
            TotalOrders = allOrders.Count,
            OrdersToday = todayOrders.Count,
            TotalRevenue = allOrders.Where(o => o.Status == "Delivered")
                                            .Sum(o => o.TotalAmount),
            RevenueToday = todayOrders.Where(o => o.Status == "Delivered")
                                              .Sum(o => o.TotalAmount),
            TotalUsers = allUsers.Count,
            TotalRestaurants = allOrders.Select(o => o.RestaurantId).Distinct().Count(),
            PendingApprovals = 0,  // populated from CatalogService via API call or event
            ActiveDeliveryAgents = allUsers.Count(u => u.Role == "DeliveryAgent" && u.IsActive),
            OrdersPaid = allOrders.Count(o => o.Status == "Paid"),
            OrdersDelivered = allOrders.Count(o => o.Status == "Delivered"),
            OrdersCancelled = allOrders.Count(o => o.Status is "Cancelled" or "Refunded"),
            OrdersInProgress = allOrders.Count(o => inProgressStatuses.Contains(o.Status)),
            TopRestaurants = topRaw.Select(r => new TopRestaurantDto
            {
                RestaurantId = r.RestaurantId,
                Name = r.Name,
                OrderCount = r.Orders,
                Revenue = r.Revenue
            }).ToList()
        };
    }
}

// ══════════════════════════════════════════════════════════════════════
// USER SERVICE
// ══════════════════════════════════════════════════════════════════════
public class AdminUserService : IAdminUserService
{
    private readonly IUserSnapshotRepository _userRepo;
    private readonly IAdminAuditLogRepository _auditRepo;

    public AdminUserService(
        IUserSnapshotRepository userRepo,
        IAdminAuditLogRepository auditRepo)
    {
        _userRepo = userRepo;
        _auditRepo = auditRepo;
    }

    public async Task<List<UserSummaryDto>> GetAllUsersAsync(string? role, bool? isActive)
    {
        var users = await _userRepo.GetAllAsync(role, isActive);
        return users.Select(MapDto).ToList();
    }

    public async Task<UserSummaryDto?> GetUserByIdAsync(Guid id)
    {
        var u = await _userRepo.GetByIdAsync(id);
        return u is null ? null : MapDto(u);
    }

    public async Task ToggleUserStatusAsync(
        Guid id, ToggleUserStatusDto dto, Guid adminId)
    {
        var user = await _userRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        var oldStatus = user.IsActive;
        await _userRepo.SetActiveAsync(id, dto.IsActive);
        await _userRepo.SaveChangesAsync();

        await _auditRepo.AddAsync(new AdminAuditLog
        {
            AdminUserId = adminId,
            Action = dto.IsActive ? "ActivateUser" : "DeactivateUser",
            EntityType = "User",
            EntityId = id,
            OldValue = oldStatus.ToString(),
            NewValue = dto.IsActive.ToString(),
            Reason = dto.Reason
        });
        await _auditRepo.SaveChangesAsync();
    }

    private static UserSummaryDto MapDto(UserSnapshot u) => new()
    {
        Id = u.Id,
        FullName = u.FullName,
        Email = u.Email,
        Mobile = u.Mobile,
        Role = u.Role,
        IsActive = u.IsActive,
        RegisteredAt = u.RegisteredAt
    };
}