using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

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
        CustomerName = o.CustomerName,
        RestaurantId = o.RestaurantId,
        RestaurantName = o.RestaurantName,
        TotalAmount = o.TotalAmount,
        Status = o.Status,
        PaymentMethod = o.PaymentMethod,
        PlacedAt = o.PlacedAt,
        CreatedAt = o.PlacedAt,  // Map PlacedAt to CreatedAt for frontend compatibility
        UpdatedAt = o.UpdatedAt,
        CancellationReason = o.CancellationReason
    };
}

// ══════════════════════════════════════════════════════════════════════
// REPORT SERVICE
// PRD: GET /gateway/admin/reports/sales  |  GET /gateway/admin/reports/partners
// Uses OrderService data directly for accurate real-time reports.
// ══════════════════════════════════════════════════════════════════════
public class AdminReportService : IAdminReportService
{
    private readonly IOrderSnapshotRepository _orderRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public AdminReportService(
        IOrderSnapshotRepository orderRepo,
        IHttpClientFactory httpClientFactory,
        IConfiguration config)
    {
        _orderRepo = orderRepo;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public async Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to)
    {
        if (from > to) throw new ArgumentException("'from' must be before 'to'.");

        // Use OrderService data for accurate real-time reports
        var orders = await GetOrdersFromOrderServiceAsync(from, to);

        if (!orders.Any())
        {
            // Fallback to snapshots if OrderService unavailable
            var snapshots = await _orderRepo.GetAllAsync(null, null, from, to);
            orders = snapshots.Select(s => new OrderData
            {
                Id = s.Id,
                Status = s.Status,
                TotalAmount = s.TotalAmount,
                PaymentMethod = s.PaymentMethod,
                CreatedAt = s.PlacedAt,
                RestaurantId = s.RestaurantId,
                RestaurantName = s.RestaurantName
            }).ToList();
        }

        var paidStatuses = new[] { "Paid", "Accepted", "Preparing", "ReadyForPickup", "PickedUp", "OutForDelivery", "Delivered" };
        var delivered = orders.Where(o => o.Status == "Delivered").ToList();
        var cancelled = orders.Where(o => o.Status is "Cancelled" or "Refunded" or "RefundRejected").ToList();
        var paidOrders = orders.Where(o => paidStatuses.Contains(o.Status)).ToList();
        var totalRev = paidOrders.Sum(o => o.TotalAmount);
        var totalOrds = orders.Count;

        // Orders by status
        var ordersByStatus = orders
            .GroupBy(o => o.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        // Daily breakdown
        var daily = orders
            .GroupBy(o => o.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var dayPaid = g.Where(o => paidStatuses.Contains(o.Status)).ToList();
                var dayRev = dayPaid.Sum(o => o.TotalAmount);
                var dayCount = g.Count();
                return new DailySaleDto
                {
                    Date = g.Key,
                    Orders = dayCount,
                    OrderCount = dayCount,
                    Revenue = dayRev,
                    AverageOrderValue = dayCount > 0 ? Math.Round(dayRev / dayCount, 2) : 0
                };
            }).ToList();

        // Payment method breakdown
        var byMethod = orders
            .GroupBy(o => string.IsNullOrEmpty(o.PaymentMethod) ? "Unknown" : o.PaymentMethod.ToUpper())
            .Select(g =>
            {
                var methodPaid = g.Where(o => paidStatuses.Contains(o.Status)).ToList();
                var methodRev = methodPaid.Sum(o => o.TotalAmount);
                return new PaymentMethodBreakdownDto
                {
                    Method = g.Key,
                    Count = g.Count(),
                    Revenue = methodRev,
                    Amount = methodRev
                };
            }).ToList();

        var paymentMethodBreakdown = byMethod.ToDictionary(
            p => p.Method,
            p => p
        );

        var avgOrderValue = totalOrds > 0 ? Math.Round(totalRev / totalOrds, 2) : 0;

        return new SalesReportDto
        {
            From = from,
            To = to,
            TotalOrders = totalOrds,
            TotalRevenue = totalRev,
            TotalGMV = totalRev,
            AverageOrderValue = avgOrderValue,
            DeliveredOrders = delivered.Count,
            CancelledOrders = cancelled.Count,
            CancellationRate = totalOrds > 0
                ? Math.Round((decimal)cancelled.Count / totalOrds * 100, 2) : 0,
            OrdersByStatus = ordersByStatus,
            PaymentMethodBreakdown = paymentMethodBreakdown,
            DailyBreakdown = daily,
            PaymentMethods = byMethod
        };
    }

    public async Task<PartnerReportDto> GetPartnerReportAsync(DateTime from, DateTime to)
    {
        if (from > to) throw new ArgumentException("'from' must be before 'to'.");

        var orders = await GetOrdersFromOrderServiceAsync(from, to);

        if (!orders.Any())
        {
            var snapshots = await _orderRepo.GetAllAsync(null, null, from, to);
            orders = snapshots.Select(s => new OrderData
            {
                Id = s.Id,
                Status = s.Status,
                TotalAmount = s.TotalAmount,
                PaymentMethod = s.PaymentMethod,
                CreatedAt = s.PlacedAt,
                RestaurantId = s.RestaurantId,
                RestaurantName = s.RestaurantName
            }).ToList();
        }

        var paidStatuses = new[] { "Paid", "Accepted", "Preparing", "ReadyForPickup", "PickedUp", "OutForDelivery", "Delivered" };

        var byRestaurant = orders
            .GroupBy(o => new { o.RestaurantId, o.RestaurantName })
            .Select(g =>
            {
                var delivered = g.Count(o => o.Status == "Delivered");
                var cancelled = g.Count(o => o.Status is "Cancelled" or "Refunded");
                var total = g.Count();
                var revenue = g.Where(o => paidStatuses.Contains(o.Status)).Sum(o => o.TotalAmount);
                var avgOrderValue = total > 0 ? Math.Round(revenue / total, 2) : 0;
                var fulfillRate = total > 0 ? Math.Round((decimal)delivered / total * 100, 2) : 0;

                return new PartnerPerformanceDto
                {
                    RestaurantId = g.Key.RestaurantId,
                    Name = g.Key.RestaurantName,
                    RestaurantName = g.Key.RestaurantName,
                    TotalOrders = total,
                    TotalRevenue = revenue,
                    AverageOrderValue = avgOrderValue,
                    Delivered = delivered,
                    Cancelled = cancelled,
                    FulfillRate = fulfillRate,
                    FulfillmentRate = fulfillRate
                };
            })
            .OrderByDescending(p => p.TotalRevenue)
            .ToList();

        var totalRevenue = byRestaurant.Sum(p => p.TotalRevenue);
        var totalOrders = byRestaurant.Sum(p => p.TotalOrders);
        var activePartners = byRestaurant.Count(p => p.TotalOrders > 0);

        return new PartnerReportDto
        {
            From = from,
            To = to,
            TotalPartners = byRestaurant.Count,
            ActivePartners = activePartners,
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            Partners = byRestaurant,
            PartnerPerformance = byRestaurant
        };
    }

    private async Task<List<OrderData>> GetOrdersFromOrderServiceAsync(DateTime from, DateTime to)
    {
        try
        {
            var orderServiceUrl = _config["Services:OrderService"] ?? "http://localhost:5003";
            var httpClient = _httpClientFactory.CreateClient();
            var res = await httpClient.GetAsync($"{orderServiceUrl}/api/orders/admin/all");

            if (!res.IsSuccessStatusCode) return new List<OrderData>();

            var json = System.Text.Json.JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            if (!json.RootElement.TryGetProperty("data", out var dataEl) ||
                dataEl.ValueKind != System.Text.Json.JsonValueKind.Array)
                return new List<OrderData>();

            var orders = new List<OrderData>();
            foreach (var o in dataEl.EnumerateArray())
            {
                var createdAt = o.TryGetProperty("createdAt", out var caEl) ? caEl.GetDateTime() : DateTime.MinValue;

                // Filter by date range
                if (createdAt < from || createdAt > to) continue;

                orders.Add(new OrderData
                {
                    Id = o.TryGetProperty("id", out var idEl) ? idEl.GetGuid() : Guid.Empty,
                    Status = o.TryGetProperty("status", out var stEl) ? stEl.GetString() ?? "" : "",
                    TotalAmount = o.TryGetProperty("totalAmount", out var taEl) ? taEl.GetDecimal() : 0m,
                    PaymentMethod = o.TryGetProperty("paymentMethod", out var pmEl) ? pmEl.GetString() ?? "" : "",
                    CreatedAt = createdAt,
                    RestaurantId = o.TryGetProperty("restaurantId", out var ridEl) ? ridEl.GetGuid() : Guid.Empty,
                    RestaurantName = o.TryGetProperty("restaurantName", out var rnEl) ? rnEl.GetString() ?? "" : ""
                });
            }
            return orders;
        }
        catch
        {
            return new List<OrderData>();
        }
    }

    private class OrderData
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public Guid RestaurantId { get; set; }
        public string RestaurantName { get; set; } = "";
    }
}