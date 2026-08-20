namespace AdminService.Domain.Entities;

// ══════════════════════════════════════════════════════════════════════
// AdminService owns its own read-model copies of Users and Restaurants
// synced via RabbitMQ events (UserRegisteredEvent, OrderPlacedEvent).
// It does NOT call other services directly — it queries its own DB.
// ══════════════════════════════════════════════════════════════════════

/// <summary>
/// Lightweight user snapshot synced from AuthService via UserRegisteredEvent.
/// AdminService uses this for user management without calling AuthService.
/// </summary>
public class UserSnapshot
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Admin-level order snapshot synced via OrderPlacedEvent.
/// Stores enough data for dashboards and reports without calling OrderService.
/// </summary>
public class OrderSnapshot
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;  // Added for admin dashboard
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "PaymentPending";
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CancellationReason { get; set; }
}

/// <summary>
/// Audit log for every Admin action (status override, refund, approval).
/// Required by PRD: "reason capture and audit logging are mandatory".
/// </summary>
public class AdminAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AdminUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Restaurant snapshot synced from CatalogService.
/// Used for restaurant approval and management.
/// </summary>
public class RestaurantSnapshot
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Guid PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Disabled
    public bool IsOpen { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Delivery agent snapshot for agent management.
/// Synced from AuthService (users with DeliveryAgent role).
/// </summary>
public class DeliveryAgentSnapshot
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsOnline { get; set; }
    public bool IsAvailable { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public int TotalDeliveries { get; set; }
    public decimal AverageRating { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;

    // ── Approval fields ────────────────────────────
    public bool IsApproved { get; set; } = false;
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }
}

/// <summary>
/// Complaint entity for quality management.
/// </summary>
public class Complaint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Order, Restaurant, Agent
    public Guid? OrderId { get; set; }
    public Guid? RestaurantId { get; set; }
    public Guid? AgentId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Resolved
    public string? Resolution { get; set; }
    public Guid? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Notification history for admin announcements.
/// </summary>
public class NotificationHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SentBy { get; set; }
    public string Recipients { get; set; } = string.Empty; // "all", "customers", "partners", "agents", or JSON array of IDs
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info"; // info, warning, announcement
    public int TotalRecipients { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
