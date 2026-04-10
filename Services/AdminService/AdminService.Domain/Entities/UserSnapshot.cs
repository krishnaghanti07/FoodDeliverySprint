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