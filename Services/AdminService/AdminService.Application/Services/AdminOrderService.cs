using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;

namespace AdminService.Application.Services;

// ══════════════════════════════════════════════════════════════════════
// ORDER SERVICE
// PRD: GET /gateway/admin/orders  |  PUT /gateway/admin/orders/{id}/status
// Mandatory reason required for cancel/refund transitions.
// ══════════════════════════════════════════════════════════════════════
public class AdminOrderService : IAdminOrderService
{
    private readonly IOrderSnapshotRepository _orderRepo;
    private readonly IAdminAuditLogRepository _auditRepo;

    // PRD state machine — Admin can override any of these
    private static readonly HashSet<string> ValidStatuses = new()
    {
        "DraftCart","CheckoutStarted","PaymentPending","Paid","Accepted","Preparing",
        "ReadyForPickup","PickedUp","OutForDelivery","Delivered",
        "PaymentFailed","CancelRequested","Cancelled","RestaurantRejected",
        "RefundInitiated","Refunded"
    };

    // Statuses that REQUIRE a reason (PRD: audit logging mandatory)
    private static readonly HashSet<string> RequireReason = new()
    {
        "Cancelled","RefundInitiated","Refunded","CancelRequested"
    };

    public AdminOrderService(
        IOrderSnapshotRepository orderRepo,
        IAdminAuditLogRepository auditRepo)
    {
        _orderRepo = orderRepo;
        _auditRepo = auditRepo;
    }

    public async Task<List<AdminOrderDto>> GetAllOrdersAsync(
        string? status, Guid? restaurantId, DateTime? from, DateTime? to)
    {
        var orders = await _orderRepo.GetAllAsync(status, restaurantId, from, to);
        return orders.Select(MapDto).ToList();
    }

    public async Task<AdminOrderDto?> GetOrderByIdAsync(Guid id)
    {
        var o = await _orderRepo.GetByIdAsync(id);
        return o is null ? null : MapDto(o);
    }

    public async Task<AdminOrderDto> UpdateOrderStatusAsync(
        Guid orderId, AdminUpdateOrderStatusDto dto, Guid adminId)
    {
        var order = await _orderRepo.GetByIdAsync(orderId)
            ?? throw new KeyNotFoundException($"Order {orderId} not found.");

        var newStatus = dto.NewStatus.Trim();
        if (!ValidStatuses.Contains(newStatus))
            throw new ArgumentException(
                $"'{newStatus}' is not a valid status. Valid: {string.Join(", ", ValidStatuses)}");

        if (RequireReason.Contains(newStatus) && string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException(
                $"A reason is mandatory when setting status to '{newStatus}'.");

        var oldStatus = order.Status;

        await _orderRepo.UpdateStatusAsync(orderId, newStatus, dto.Reason);
        await _orderRepo.SaveChangesAsync();

        // Audit log — PRD: "reason capture and audit logging are mandatory"
        await _auditRepo.AddAsync(new AdminAuditLog
        {
            AdminUserId = adminId,
            Action = "UpdateOrderStatus",
            EntityType = "Order",
            EntityId = orderId,
            OldValue = oldStatus,
            NewValue = newStatus,
            Reason = dto.Reason
        });
        await _auditRepo.SaveChangesAsync();

        var updated = await _orderRepo.GetByIdAsync(orderId);
        return MapDto(updated!);
    }

    private static AdminOrderDto MapDto(OrderSnapshot o) => new()
    {
        Id = o.Id,
        CustomerId = o.CustomerId,
        CustomerEmail = o.CustomerEmail,
        RestaurantId = o.RestaurantId,
        RestaurantName = o.RestaurantName,
        TotalAmount = o.TotalAmount,
        Status = o.Status,
        PaymentMethod = o.PaymentMethod,
        PlacedAt = o.PlacedAt,
        UpdatedAt = o.UpdatedAt,
        CancellationReason = o.CancellationReason
    };
}

// ══════════════════════════════════════════════════════════════════════
// REPORT SERVICE
// PRD: GET /gateway/admin/reports/sales  |  GET /gateway/admin/reports/partners
// ══════════════════════════════════════════════════════════════════════
public class AdminReportService : IAdminReportService
{
    private readonly IOrderSnapshotRepository _orderRepo;

    public AdminReportService(IOrderSnapshotRepository orderRepo) =>
        _orderRepo = orderRepo;

    public async Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to)
    {
        if (from > to) throw new ArgumentException("'from' must be before 'to'.");

        var orders = await _orderRepo.GetAllAsync(null, null, from, to);

        var delivered = orders.Where(o => o.Status == "Delivered").ToList();
        var cancelled = orders.Where(o => o.Status is "Cancelled" or "Refunded").ToList();
        var totalRev = delivered.Sum(o => o.TotalAmount);
        var totalOrds = orders.Count;

        // Daily breakdown
        var daily = orders
            .GroupBy(o => o.PlacedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailySaleDto
            {
                Date = g.Key,
                Orders = g.Count(),
                Revenue = g.Where(o => o.Status == "Delivered").Sum(o => o.TotalAmount)
            }).ToList();

        // Payment method breakdown
        var byMethod = orders
            .GroupBy(o => string.IsNullOrEmpty(o.PaymentMethod) ? "Unknown" : o.PaymentMethod)
            .Select(g => new PaymentMethodBreakdownDto
            {
                Method = g.Key,
                Count = g.Count(),
                Revenue = g.Where(o => o.Status == "Delivered").Sum(o => o.TotalAmount)
            }).ToList();

        return new SalesReportDto
        {
            From = from,
            To = to,
            TotalOrders = totalOrds,
            TotalRevenue = totalRev,
            AverageOrderValue = totalOrds > 0 ? Math.Round(totalRev / totalOrds, 2) : 0,
            DeliveredOrders = delivered.Count,
            CancelledOrders = cancelled.Count,
            CancellationRate = totalOrds > 0
                ? Math.Round((decimal)cancelled.Count / totalOrds * 100, 2) : 0,
            DailyBreakdown = daily,
            PaymentMethods = byMethod
        };
    }

    public async Task<PartnerReportDto> GetPartnerReportAsync(DateTime from, DateTime to)
    {
        if (from > to) throw new ArgumentException("'from' must be before 'to'.");

        var orders = await _orderRepo.GetAllAsync(null, null, from, to);

        var byRestaurant = orders
            .GroupBy(o => new { o.RestaurantId, o.RestaurantName })
            .Select(g =>
            {
                var delivered = g.Count(o => o.Status == "Delivered");
                var cancelled = g.Count(o => o.Status is "Cancelled" or "Refunded");
                var total = g.Count();
                return new PartnerPerformanceDto
                {
                    RestaurantId = g.Key.RestaurantId,
                    Name = g.Key.RestaurantName,
                    TotalOrders = total,
                    TotalRevenue = g.Where(o => o.Status == "Delivered")
                                     .Sum(o => o.TotalAmount),
                    Delivered = delivered,
                    Cancelled = cancelled,
                    FulfillRate = total > 0
                        ? Math.Round((decimal)delivered / total * 100, 2) : 0
                };
            })
            .OrderByDescending(p => p.TotalRevenue)
            .ToList();

        return new PartnerReportDto
        {
            From = from,
            To = to,
            Partners = byRestaurant
        };
    }
}