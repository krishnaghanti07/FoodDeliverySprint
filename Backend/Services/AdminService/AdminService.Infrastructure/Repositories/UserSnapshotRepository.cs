using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;
using AdminService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Infrastructure.Repositories;

// ── User Snapshot Repository ──────────────────────────────────────────
public class UserSnapshotRepository : IUserSnapshotRepository
{
    private readonly AdminDbContext _db;
    public UserSnapshotRepository(AdminDbContext db) => _db = db;

    public async Task<List<UserSnapshot>> GetAllAsync(string? role, bool? isActive)
    {
        var q = _db.UserSnapshots.AsQueryable();
        if (!string.IsNullOrWhiteSpace(role))
            q = q.Where(u => u.Role == role);
        if (isActive.HasValue)
            q = q.Where(u => u.IsActive == isActive.Value);
        return await q.OrderBy(u => u.FullName).ToListAsync();
    }

    public Task<UserSnapshot?> GetByIdAsync(Guid id) =>
        _db.UserSnapshots.FirstOrDefaultAsync(u => u.Id == id);

    public Task<UserSnapshot?> GetByEmailAsync(string email) =>
        _db.UserSnapshots.FirstOrDefaultAsync(u => u.Email == email);

    public async Task UpsertAsync(UserSnapshot snap)
    {
        var existing = await _db.UserSnapshots.FindAsync(snap.Id);
        if (existing is null)
            await _db.UserSnapshots.AddAsync(snap);
        else
        {
            existing.FullName = snap.FullName;
            existing.Email = snap.Email;
            existing.Mobile = snap.Mobile;
            existing.Role = snap.Role;
            existing.IsActive = snap.IsActive;
            existing.RegisteredAt = snap.RegisteredAt;
            existing.SyncedAt = DateTime.UtcNow;
            _db.UserSnapshots.Update(existing);
        }
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        var u = await _db.UserSnapshots.FindAsync(id);
        if (u is not null) { u.IsActive = isActive; _db.UserSnapshots.Update(u); }
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}

// ── Order Snapshot Repository ─────────────────────────────────────────
public class OrderSnapshotRepository : IOrderSnapshotRepository
{
    private readonly AdminDbContext _db;
    public OrderSnapshotRepository(AdminDbContext db) => _db = db;

    public async Task<List<OrderSnapshot>> GetAllAsync(
        string? status, Guid? restaurantId, DateTime? from, DateTime? to)
    {
        var q = _db.OrderSnapshots.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(o => o.Status == status);
        if (restaurantId.HasValue)
            q = q.Where(o => o.RestaurantId == restaurantId.Value);
        if (from.HasValue)
            q = q.Where(o => o.PlacedAt >= from.Value);
        if (to.HasValue)
            q = q.Where(o => o.PlacedAt <= to.Value);
        return await q.OrderByDescending(o => o.PlacedAt).ToListAsync();
    }

    public Task<OrderSnapshot?> GetByIdAsync(Guid id) =>
        _db.OrderSnapshots.FirstOrDefaultAsync(o => o.Id == id);

    public async Task UpsertAsync(OrderSnapshot snap)
    {
        var existing = await _db.OrderSnapshots.FindAsync(snap.Id);
        if (existing is null)
            await _db.OrderSnapshots.AddAsync(snap);
        else
        {
            existing.Status = snap.Status;
            existing.TotalAmount = snap.TotalAmount;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.CancellationReason = snap.CancellationReason;
            _db.OrderSnapshots.Update(existing);
        }
    }

    public async Task UpdateStatusAsync(Guid id, string newStatus, string? reason)
    {
        var o = await _db.OrderSnapshots.FindAsync(id);
        if (o is not null)
        {
            o.Status = newStatus;
            o.UpdatedAt = DateTime.UtcNow;
            o.CancellationReason = reason ?? o.CancellationReason;
            _db.OrderSnapshots.Update(o);
        }
    }

    public Task<decimal> GetTotalRevenueAsync(DateTime? from, DateTime? to)
    {
        var q = _db.OrderSnapshots.Where(o => o.Status == "Delivered");
        if (from.HasValue) q = q.Where(o => o.PlacedAt >= from.Value);
        if (to.HasValue) q = q.Where(o => o.PlacedAt <= to.Value);
        return q.SumAsync(o => o.TotalAmount);
    }

    public Task<int> GetOrderCountAsync(string? status, DateTime? from, DateTime? to)
    {
        var q = _db.OrderSnapshots.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(o => o.Status == status);
        if (from.HasValue) q = q.Where(o => o.PlacedAt >= from.Value);
        if (to.HasValue) q = q.Where(o => o.PlacedAt <= to.Value);
        return q.CountAsync();
    }

    public async Task<List<(Guid RestaurantId, string Name, int Orders, decimal Revenue)>>
        GetTopRestaurantsAsync(int top, DateTime? from, DateTime? to)
    {
        var q = _db.OrderSnapshots.AsQueryable();
        if (from.HasValue) q = q.Where(o => o.PlacedAt >= from.Value);
        if (to.HasValue) q = q.Where(o => o.PlacedAt <= to.Value);

        var grouped = await q
            .GroupBy(o => new { o.RestaurantId, o.RestaurantName })
            .Select(g => new
            {
                g.Key.RestaurantId,
                g.Key.RestaurantName,
                Orders = g.Count(),
                Revenue = g.Where(o => o.Status == "Delivered").Sum(o => o.TotalAmount)
            })
            .OrderByDescending(g => g.Revenue)
            .Take(top)
            .ToListAsync();

        return grouped
            .Select(g => (g.RestaurantId, g.RestaurantName, g.Orders, g.Revenue))
            .ToList();
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}

// ── Audit Log Repository ──────────────────────────────────────────────
public class AdminAuditLogRepository : IAdminAuditLogRepository
{
    private readonly AdminDbContext _db;
    public AdminAuditLogRepository(AdminDbContext db) => _db = db;

    public async Task AddAsync(AdminAuditLog log) => await _db.AuditLogs.AddAsync(log);

    public Task<List<AdminAuditLog>> GetByEntityAsync(string entityType, Guid entityId) =>
        _db.AuditLogs
           .Where(a => a.EntityType == entityType && a.EntityId == entityId)
           .OrderByDescending(a => a.PerformedAt)
           .ToListAsync();

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}