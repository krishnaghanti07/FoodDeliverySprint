using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;
using AdminService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Infrastructure.Repositories;

public class RestaurantSnapshotRepository : IRestaurantSnapshotRepository
{
    private readonly AdminDbContext _db;
    public RestaurantSnapshotRepository(AdminDbContext db) => _db = db;

    public async Task<List<RestaurantSnapshot>> GetAllAsync(string? status, int? page, int? pageSize)
    {
        var q = _db.RestaurantSnapshots.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(r => r.Status == status);

        q = q.OrderByDescending(r => r.CreatedAt);

        if (page.HasValue && pageSize.HasValue)
        {
            q = q.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
        }

        return await q.ToListAsync();
    }

    public Task<RestaurantSnapshot?> GetByIdAsync(Guid id) =>
        _db.RestaurantSnapshots.FirstOrDefaultAsync(r => r.Id == id);

    public async Task UpsertAsync(RestaurantSnapshot snapshot)
    {
        var existing = await _db.RestaurantSnapshots.FindAsync(snapshot.Id);
        if (existing is null)
        {
            await _db.RestaurantSnapshots.AddAsync(snapshot);
        }
        else
        {
            existing.Name = snapshot.Name;
            existing.Description = snapshot.Description;
            existing.Address = snapshot.Address;
            existing.Phone = snapshot.Phone;
            existing.PartnerId = snapshot.PartnerId;
            existing.PartnerName = snapshot.PartnerName;
            existing.Status = snapshot.Status;
            existing.IsOpen = snapshot.IsOpen;
            existing.AverageRating = snapshot.AverageRating;
            existing.TotalOrders = snapshot.TotalOrders;
            existing.TotalRevenue = snapshot.TotalRevenue;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.SyncedAt = DateTime.UtcNow;
            _db.RestaurantSnapshots.Update(existing);
        }
    }

    public async Task UpdateStatusAsync(Guid id, string status)
    {
        var restaurant = await _db.RestaurantSnapshots.FindAsync(id);
        if (restaurant is not null)
        {
            restaurant.Status = status;
            restaurant.UpdatedAt = DateTime.UtcNow;
            _db.RestaurantSnapshots.Update(restaurant);
        }
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
