using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;
using AdminService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Infrastructure.Repositories;

public class DeliveryAgentSnapshotRepository : IDeliveryAgentSnapshotRepository
{
    private readonly AdminDbContext _db;
    public DeliveryAgentSnapshotRepository(AdminDbContext db) => _db = db;

    public async Task<List<DeliveryAgentSnapshot>> GetAllAsync(bool? isActive, bool? isOnline, int? page, int? pageSize)
    {
        var q = _db.DeliveryAgentSnapshots.AsQueryable();
        
        if (isActive.HasValue)
            q = q.Where(a => a.IsActive == isActive.Value);
        
        if (isOnline.HasValue)
            q = q.Where(a => a.IsOnline == isOnline.Value);

        q = q.OrderByDescending(a => a.RegisteredAt);

        if (page.HasValue && pageSize.HasValue)
        {
            q = q.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
        }

        return await q.ToListAsync();
    }

    public Task<DeliveryAgentSnapshot?> GetByIdAsync(Guid id) =>
        _db.DeliveryAgentSnapshots.FirstOrDefaultAsync(a => a.Id == id);

    public async Task UpsertAsync(DeliveryAgentSnapshot snapshot)
    {
        var existing = await _db.DeliveryAgentSnapshots.FindAsync(snapshot.Id);
        if (existing is null)
        {
            await _db.DeliveryAgentSnapshots.AddAsync(snapshot);
        }
        else
        {
            existing.FullName = snapshot.FullName;
            existing.Email = snapshot.Email;
            existing.Mobile = snapshot.Mobile;
            existing.IsActive = snapshot.IsActive;
            existing.IsOnline = snapshot.IsOnline;
            existing.IsAvailable = snapshot.IsAvailable;
            existing.VehicleType = snapshot.VehicleType;
            existing.TotalDeliveries = snapshot.TotalDeliveries;
            existing.AverageRating = snapshot.AverageRating;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.SyncedAt = DateTime.UtcNow;
            _db.DeliveryAgentSnapshots.Update(existing);
        }
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        var agent = await _db.DeliveryAgentSnapshots.FindAsync(id);
        if (agent is not null)
        {
            agent.IsActive = isActive;
            agent.UpdatedAt = DateTime.UtcNow;
            _db.DeliveryAgentSnapshots.Update(agent);
        }
    }

    public async Task<List<DeliveryAgentSnapshot>> GetPendingApprovalAsync()
    {
        return await _db.DeliveryAgentSnapshots
            .Where(a => !a.IsApproved && a.IsActive)
            .OrderBy(a => a.RegisteredAt)
            .ToListAsync();
    }

    public async Task UpdateAsync(DeliveryAgentSnapshot agent)
    {
        _db.DeliveryAgentSnapshots.Update(agent);
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
