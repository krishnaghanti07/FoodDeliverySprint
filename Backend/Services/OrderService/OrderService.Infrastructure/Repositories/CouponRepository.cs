using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

public class CouponRepository : ICouponRepository
{
    private readonly OrderDbContext _db;
    private readonly ILogger<CouponRepository> _logger;
    
    public CouponRepository(OrderDbContext db, ILogger<CouponRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Coupon?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("[COUPON_REPO] GetByIdAsync called for ID: {Id}", id);
        // Use tracking for updates - don't use AsNoTracking here
        var coupon = await _db.Set<Coupon>().FirstOrDefaultAsync(c => c.Id == id);
        _logger.LogInformation("[COUPON_REPO] Found coupon: {Found}, IsActive: {IsActive}", coupon != null, coupon?.IsActive);
        return coupon;
    }

    public Task<Coupon?> GetByCodeAsync(string code) =>
        _db.Set<Coupon>().FirstOrDefaultAsync(c => c.Code.ToUpper() == code.ToUpper());

    public Task<List<Coupon>> GetAllAsync() =>
        _db.Set<Coupon>().OrderByDescending(c => c.CreatedAt).ToListAsync();

    public Task<List<Coupon>> GetByRestaurantIdAsync(Guid? restaurantId) =>
        _db.Set<Coupon>()
           .Where(c => c.RestaurantId == restaurantId)
           .OrderByDescending(c => c.CreatedAt)
           .ToListAsync();

    public Task<List<Coupon>> GetActiveAsync() =>
        _db.Set<Coupon>()
           .Where(c => c.IsActive && c.ValidFrom <= DateTime.UtcNow && c.ValidUntil >= DateTime.UtcNow)
           .OrderByDescending(c => c.CreatedAt)
           .ToListAsync();

    public async Task AddAsync(Coupon coupon) =>
        await _db.Set<Coupon>().AddAsync(coupon);

    public Task UpdateAsync(Coupon coupon)
    {
        _logger.LogInformation("[COUPON_REPO] UpdateAsync called - ID: {Id}, Description: {Desc}, Value: {Value}, IsActive: {IsActive}", 
            coupon.Id, coupon.Description, coupon.Value, coupon.IsActive);
        
        var entry = _db.Entry(coupon);
        _logger.LogInformation("[COUPON_REPO] Entry state before: {State}", entry.State);
        
        if (entry.State == EntityState.Detached)
        {
            _logger.LogInformation("[COUPON_REPO] Entity is detached, attaching...");
            _db.Set<Coupon>().Attach(coupon);
        }
        
        entry.State = EntityState.Modified;
        _logger.LogInformation("[COUPON_REPO] Entry state after: {State}", entry.State);
        
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var coupon = await _db.Set<Coupon>().FindAsync(id);
        if (coupon is not null)
            _db.Set<Coupon>().Remove(coupon);
    }

    public Task SaveChangesAsync()
    {
        _logger.LogInformation("[COUPON_REPO] SaveChangesAsync called");
        var result = _db.SaveChangesAsync();
        _logger.LogInformation("[COUPON_REPO] SaveChangesAsync completed");
        return result;
    }
}
