using AdminService.Domain.Entities;

namespace AdminService.Domain.Interfaces;

public interface IUserSnapshotRepository
{
    Task<List<UserSnapshot>> GetAllAsync(string? role, bool? isActive);
    Task<UserSnapshot?> GetByIdAsync(Guid id);
    Task<UserSnapshot?> GetByEmailAsync(string email);
    Task UpsertAsync(UserSnapshot snapshot);   // insert or update
    Task SetActiveAsync(Guid id, bool isActive);
    Task SaveChangesAsync();
}

public interface IOrderSnapshotRepository
{
    Task<List<OrderSnapshot>> GetAllAsync(
        string? status, Guid? restaurantId, DateTime? from, DateTime? to);
    Task<OrderSnapshot?> GetByIdAsync(Guid id);
    Task UpsertAsync(OrderSnapshot snapshot);
    Task UpdateStatusAsync(Guid id, string newStatus, string? reason);
    Task<decimal> GetTotalRevenueAsync(DateTime? from, DateTime? to);
    Task<int> GetOrderCountAsync(string? status, DateTime? from, DateTime? to);
    Task<List<(Guid RestaurantId, string Name, int Orders, decimal Revenue)>>
                              GetTopRestaurantsAsync(int top, DateTime? from, DateTime? to);
    Task SaveChangesAsync();
}

public interface IAdminAuditLogRepository
{
    Task AddAsync(AdminAuditLog log);
    Task<List<AdminAuditLog>> GetByEntityAsync(string entityType, Guid entityId);
    Task SaveChangesAsync();
}


public interface IRestaurantSnapshotRepository
{
    Task<List<RestaurantSnapshot>> GetAllAsync(string? status, int? page, int? pageSize);
    Task<RestaurantSnapshot?> GetByIdAsync(Guid id);
    Task UpsertAsync(RestaurantSnapshot snapshot);
    Task UpdateStatusAsync(Guid id, string status);
    Task SaveChangesAsync();
}

public interface IDeliveryAgentSnapshotRepository
{
    Task<List<DeliveryAgentSnapshot>> GetAllAsync(bool? isActive, bool? isOnline, int? page, int? pageSize);
    Task<DeliveryAgentSnapshot?> GetByIdAsync(Guid id);
    Task UpsertAsync(DeliveryAgentSnapshot snapshot);
    Task SetActiveAsync(Guid id, bool isActive);
    Task<List<DeliveryAgentSnapshot>> GetPendingApprovalAsync();
    Task UpdateAsync(DeliveryAgentSnapshot agent);
    Task SaveChangesAsync();
}

public interface IComplaintRepository
{
    Task<List<Complaint>> GetAllAsync(string? status, string? type);
    Task<Complaint?> GetByIdAsync(Guid id);
    Task AddAsync(Complaint complaint);
    Task UpdateAsync(Complaint complaint);
    Task SaveChangesAsync();
}

public interface INotificationHistoryRepository
{
    Task AddAsync(NotificationHistory notification);
    Task<List<NotificationHistory>> GetHistoryAsync(DateTime? from, DateTime? to, string? type);
    Task SaveChangesAsync();
}
