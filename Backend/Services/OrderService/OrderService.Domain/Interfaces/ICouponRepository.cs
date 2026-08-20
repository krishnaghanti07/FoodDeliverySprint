using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

public interface ICouponRepository
{
    Task<Coupon?> GetByIdAsync(Guid id);
    Task<Coupon?> GetByCodeAsync(string code);
    Task<List<Coupon>> GetAllAsync();
    Task<List<Coupon>> GetByRestaurantIdAsync(Guid? restaurantId);
    Task<List<Coupon>> GetActiveAsync();
    Task AddAsync(Coupon coupon);
    Task UpdateAsync(Coupon coupon);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}
