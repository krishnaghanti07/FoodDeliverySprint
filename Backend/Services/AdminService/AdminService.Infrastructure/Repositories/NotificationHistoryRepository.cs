using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;
using AdminService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Infrastructure.Repositories;

public class NotificationHistoryRepository : INotificationHistoryRepository
{
    private readonly AdminDbContext _db;
    public NotificationHistoryRepository(AdminDbContext db) => _db = db;

    public async Task AddAsync(NotificationHistory notification)
    {
        await _db.NotificationHistory.AddAsync(notification);
    }

    public async Task<List<NotificationHistory>> GetHistoryAsync(DateTime? from, DateTime? to, string? type)
    {
        var q = _db.NotificationHistory.AsQueryable();
        
        if (from.HasValue)
            q = q.Where(n => n.SentAt >= from.Value);
        
        if (to.HasValue)
            q = q.Where(n => n.SentAt <= to.Value);
        
        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(n => n.Type == type);

        return await q.OrderByDescending(n => n.SentAt).ToListAsync();
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
