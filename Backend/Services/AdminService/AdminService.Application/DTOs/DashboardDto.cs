using System.ComponentModel.DataAnnotations;

namespace AdminService.Application.DTOs;

// ── Soft Delete DTO ────────────────────────────────────────────────────

public class SoftDeleteDto
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}

// ── Dashboard DTOs ────────────────────────────────────────────────────

/// <summary>PRD: GET /gateway/admin/dashboard — KPI cards + top restaurants</summary>
public class DashboardDto
{
    // KPI Cards
    public int TotalOrders { get; set; }
    public int OrdersToday { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RevenueToday { get; set; }
    public decimal AdminRevenue { get; set; } // Platform fees + Restaurant commissions
    public decimal AdminRevenueToday { get; set; }
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
    public string CustomerName { get; set; } = string.Empty;  // Added for frontend
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime PlacedAt { get; set; }
    public DateTime CreatedAt { get; set; }  // Added for frontend compatibility
    public DateTime UpdatedAt { get; set; }
    public string? CancellationReason { get; set; }
}

/// <summary>PRD: PUT /gateway/admin/orders/{id}/status — mandatory reason for cancel/refund</summary>
public class AdminUpdateOrderStatusDto
{
    [Required] public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }  // Required only for Cancelled/RefundInitiated/Refunded/CancelRequested — enforced in service layer
}

// ── Report DTOs ───────────────────────────────────────────────────────

/// <summary>PRD: GET /gateway/admin/reports/sales</summary>
public class SalesReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalGMV { get; set; }          // Alias for frontend
    public decimal AverageOrderValue { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal CancellationRate { get; set; }
    public Dictionary<string, int> OrdersByStatus { get; set; } = new();
    public Dictionary<string, PaymentMethodBreakdownDto> PaymentMethodBreakdown { get; set; } = new();
    public List<DailySaleDto> DailyBreakdown { get; set; } = new();
    public List<PaymentMethodBreakdownDto> PaymentMethods { get; set; } = new();
}

public class DailySaleDto
{
    public DateTime Date { get; set; }
    public int Orders { get; set; }
    public int OrderCount { get; set; }   // Alias for frontend
    public decimal Revenue { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class PaymentMethodBreakdownDto
{
    public string Method { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Revenue { get; set; }
    public decimal Amount { get; set; }  // Alias for frontend
}

/// <summary>PRD: GET /gateway/admin/reports/partners</summary>
public class PartnerReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int TotalPartners { get; set; }
    public int ActivePartners { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public List<PartnerPerformanceDto> Partners { get; set; } = new();
    public List<PartnerPerformanceDto> PartnerPerformance { get; set; } = new();  // Alias for frontend
}

public class PartnerPerformanceDto
{
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RestaurantName { get; set; } = string.Empty;  // Alias for frontend
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int Delivered { get; set; }
    public int Cancelled { get; set; }
    public decimal FulfillRate { get; set; }
    public decimal FulfillmentRate { get; set; }  // Alias for frontend
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


// ── Restaurant Management DTOs ────────────────────────────────────────

public class RestaurantListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RestaurantDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Guid PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ApproveRestaurantDto
{
    public string? Notes { get; set; }
}

public class RejectRestaurantDto
{
    [Required, MinLength(10)]
    public string Reason { get; set; } = string.Empty;
}

public class ToggleActiveDto
{
    [Required]
    public bool IsActive { get; set; }
    [Required, MinLength(5)]
    public string Reason { get; set; } = string.Empty;
}

public class UpdateRestaurantStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty; // Approved, Disabled
    [Required, MinLength(5)]
    public string Reason { get; set; } = string.Empty;
}

public class RestoreRestaurantDto
{
    [Required, MinLength(10)]
    public string Reason { get; set; } = string.Empty;
}

// ── Delivery Agent Management DTOs ────────────────────────────────────

public class DeliveryAgentListDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsOnline { get; set; }
    public bool IsAvailable { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public int TotalDeliveries { get; set; }
    public decimal AverageRating { get; set; }
    public DateTime RegisteredAt { get; set; }
}

public class DeliveryAgentDetailDto : DeliveryAgentListDto
{
    public string Email { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class UpdateAgentStatusDto
{
    [Required]
    public bool IsActive { get; set; }
    [Required, MinLength(5)]
    public string Reason { get; set; } = string.Empty;
}

public class ApproveAgentDto
{
    public string? Notes { get; set; }
}

public class RejectAgentDto
{
    [Required, MinLength(10)]
    public string Reason { get; set; } = string.Empty;
}

public class RestoreAgentDto
{
    [Required, MinLength(10)]
    public string Reason { get; set; } = string.Empty;
}

// ── Complaint Management DTOs ─────────────────────────────────────────

public class ComplaintListDto
{
    public Guid Id { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ComplaintDetailDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public Guid? RestaurantId { get; set; }
    public Guid? AgentId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class ResolveComplaintDto
{
    [Required, MinLength(10)]
    public string Action { get; set; } = string.Empty;
    [Required, MinLength(10)]
    public string Notes { get; set; } = string.Empty;
}

// ── Notification DTOs ─────────────────────────────────────────────────

public class SendNotificationDto
{
    [Required]
    public string Recipients { get; set; } = string.Empty; // "all", "customers", "partners", "agents"
    [Required, MinLength(3)]
    public string Title { get; set; } = string.Empty;
    [Required, MinLength(10)]
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info"; // info, warning, announcement
}

public class NotificationHistoryDto
{
    public Guid Id { get; set; }
    public string Recipients { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int TotalRecipients { get; set; }
    public DateTime SentAt { get; set; }
}
