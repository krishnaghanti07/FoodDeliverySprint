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

        // Calculate admin revenue from OrderSnapshots
        // Sources of admin revenue:
        // 1. Delivered orders: Platform Fee (₹15) + Commission (15% of subtotal)
        // 2. Rejected refunds: Platform Fee (₹15) + Cancellation Charge (5% of total)
        decimal adminRevenue = 0;
        decimal adminRevenueToday = 0;
        
        var deliveredOrders = allOrders.Where(o => o.Status == "Delivered").ToList();
        var deliveredOrdersToday = todayOrders.Where(o => o.Status == "Delivered").ToList();
        var refundRejectedOrders = allOrders.Where(o => o.Status == "RefundRejected").ToList();
        var refundRejectedOrdersToday = todayOrders.Where(o => o.Status == "RefundRejected").ToList();
        
        // Revenue from delivered orders: Platform Fee + 15% Commission
        foreach (var order in deliveredOrders)
        {
            decimal platformFee = 15.00m; // Fixed platform fee
            decimal commission = order.TotalAmount * 0.15m; // 15% commission
            adminRevenue += platformFee + commission;
        }
        
        foreach (var order in deliveredOrdersToday)
        {
            decimal platformFee = 15.00m;
            decimal commission = order.TotalAmount * 0.15m;
            adminRevenueToday += platformFee + commission;
        }
        
        // Revenue from rejected refunds: Platform Fee + 5% Cancellation Charge
        foreach (var order in refundRejectedOrders)
        {
            decimal platformFee = 15.00m;
            decimal cancellationCharge = order.TotalAmount * 0.05m; // 5% cancellation charge
            adminRevenue += platformFee + cancellationCharge;
        }
        
        foreach (var order in refundRejectedOrdersToday)
        {
            decimal platformFee = 15.00m;
            decimal cancellationCharge = order.TotalAmount * 0.05m;
            adminRevenueToday += platformFee + cancellationCharge;
        }

        return new DashboardDto
        {
            TotalOrders = allOrders.Count,
            OrdersToday = todayOrders.Count,
            TotalRevenue = allOrders.Where(o => o.Status == "Delivered")
                                            .Sum(o => o.TotalAmount),
            RevenueToday = todayOrders.Where(o => o.Status == "Delivered")
                                              .Sum(o => o.TotalAmount),
            AdminRevenue = adminRevenue,
            AdminRevenueToday = adminRevenueToday,
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
        
        // Update snapshot
        await _userRepo.SetActiveAsync(id, dto.IsActive);
        await _userRepo.SaveChangesAsync();

        // Call AuthService to update actual user status
        try
        {
            using var httpClient = new HttpClient();
            var authServiceUrl = "http://localhost:5001/api/auth/admin/toggle-user-status";
            var payload = new { userId = id, isActive = dto.IsActive, reason = dto.Reason };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            await httpClient.PostAsync(authServiceUrl, content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to update AuthService: {ex.Message}");
        }

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