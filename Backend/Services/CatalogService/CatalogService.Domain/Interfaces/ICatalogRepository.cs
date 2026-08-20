using CatalogService.Domain.Entities;

namespace CatalogService.Domain.Interfaces;

public interface IRestaurantRepository
{
    Task<List<Restaurant>> GetAllApprovedAsync(string? city, string? cuisine, string? search);
    Task<List<Restaurant>> GetAllAsync();                          // Admin: all incl unapproved
    Task<Restaurant?> GetByIdAsync(Guid id);                      // lightweight — no includes
    Task<Restaurant?> GetByIdWithMenuAsync(Guid id);              // Public: approved & not deleted only
    Task<Restaurant?> GetByIdWithMenuAdminAsync(Guid id);         // Admin: all restaurants including unapproved/deleted
    Task<Restaurant?> GetByPartnerIdAsync(Guid partnerId);        // Get active restaurant for partner
    Task<Restaurant?> GetDeletedByPartnerIdAsync(Guid partnerId); // Get deleted restaurant for partner
    Task AddAsync(Restaurant restaurant);
    Task UpdateAsync(Restaurant restaurant);
    Task DeleteAsync(Guid id);                                    // Soft delete
    Task PermanentlyDeleteAsync(Guid id);                         // Hard delete (permanent)
    Task SaveChangesAsync();
}

public interface IMenuItemRepository
{
    Task<MenuItem?> GetByIdAsync(Guid id);
    Task<List<MenuItem>> GetByRestaurantIdAsync(Guid restaurantId);
    Task<List<MenuItem>> SearchByNameOrDescriptionAsync(string query);
    Task AddAsync(MenuItem item);
    Task UpdateAsync(MenuItem item);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id);
    Task<List<Category>> GetByRestaurantIdAsync(Guid restaurantId);
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}

public interface IOperatingHourRepository
{
    Task<List<OperatingHour>> GetByRestaurantIdAsync(Guid restaurantId);
    Task AddRangeAsync(List<OperatingHour> hours);
    Task DeleteByRestaurantIdAsync(Guid restaurantId);
    Task SaveChangesAsync();
}

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(Guid id);
    Task<List<Review>> GetByRestaurantIdAsync(Guid restaurantId, int page = 1, int pageSize = 10);
    Task<Review?> GetByUserAndRestaurantAsync(Guid userId, Guid restaurantId);
    Task<int> GetTotalCountByRestaurantAsync(Guid restaurantId);
    Task<Dictionary<int, int>> GetRatingDistributionAsync(Guid restaurantId);
    Task AddAsync(Review review);
    Task UpdateAsync(Review review);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}