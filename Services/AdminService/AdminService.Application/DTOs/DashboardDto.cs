using System.ComponentModel.DataAnnotations;

namespace AdminService.Application.DTOs;

// ── Dashboard DTOs ────────────────────────────────────────────────────

/// <summary>PRD: GET /gateway/admin/dashboard — KPI cards + top restaurants</summary>
public class DashboardDto
{
    // KPI Cards
    public int TotalOrders { get; set; }
    public int OrdersToday { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RevenueToday { get; set; }
    public int TotalUsers { get; set; }
    public int TotalRestaurants { get; set; }
    public int PendingApprovals { get; set; }   // unapproved restaurants via Catalog
    public int ActiveDeliveryAgents { get; set; }

    // Order status breakdown
    public int OrdersPaid { get; set; }
    public int OrdersDelivered { get; set; }
    public int OrdersCancelled { get; set; }
    public int OrdersInProgress { get; set; }   // Accepted+Preparing+ReadyForPickup+PickedUp+OutForDelivery

    // Top restaurants
    public List<TopRestaurantDto> TopRestaurants { get; set; } = new();
}

public class TopRestaurantDto
{
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}

// ── User Management DTOs ──────────────────────────────────────────────

public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime RegisteredAt { get; set; }
}

public class ToggleUserStatusDto
{
    [Required] public bool IsActive { get; set; }
    [Required] public string Reason { get; set; } = string.Empty;
}

// ── Order Management DTOs ─────────────────────────────────────────────

public class AdminOrderDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime PlacedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CancellationReason { get; set; }
}

/// <summary>PRD: PUT /gateway/admin/orders/{id}/status — mandatory reason for cancel/refund</summary>
public class AdminUpdateOrderStatusDto
{
    [Required] public string NewStatus { get; set; } = string.Empty;
    [Required, MinLength(5)] public string Reason { get; set; } = string.Empty;
}

// ── Report DTOs ───────────────────────────────────────────────────────

/// <summary>PRD: GET /gateway/admin/reports/sales</summary>
public class SalesReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal CancellationRate { get; set; }
    public List<DailySaleDto> DailyBreakdown { get; set; } = new();
    public List<PaymentMethodBreakdownDto> PaymentMethods { get; set; } = new();
}

public class DailySaleDto
{
    public DateTime Date { get; set; }
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
}

public class PaymentMethodBreakdownDto
{
    public string Method { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>PRD: GET /gateway/admin/reports/partners</summary>
public class PartnerReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<PartnerPerformanceDto> Partners { get; set; } = new();
}

public class PartnerPerformanceDto
{
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int Delivered { get; set; }
    public int Cancelled { get; set; }
    public decimal FulfillRate { get; set; }
}

// ── Audit Log DTO ─────────────────────────────────────────────────────
public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid AdminUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public DateTime PerformedAt { get; set; }
}