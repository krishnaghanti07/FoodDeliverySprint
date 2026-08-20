using CatalogService.Domain.Entities;
using CatalogService.Domain.Interfaces;
using CatalogService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Repositories;

public class RestaurantRepository : IRestaurantRepository
{
    private readonly CatalogDbContext _db;
    public RestaurantRepository(CatalogDbContext db) => _db = db;

    public async Task<List<Restaurant>> GetAllApprovedAsync(
        string? city, string? cuisine, string? search)
    {
        var q = _db.Restaurants.Where(r => r.IsApproved && !r.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
            q = q.Where(r => r.City.ToLower().Contains(city.ToLower()));
        if (!string.IsNullOrWhiteSpace(cuisine))
            q = q.Where(r => r.Cuisine.ToLower().Contains(cuisine.ToLower()));
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(r => r.Name.ToLower().Contains(search.ToLower()));

        return await q.OrderByDescending(r => r.Rating).ToListAsync();
    }

    public Task<List<Restaurant>> GetAllAsync() =>
        _db.Restaurants.OrderByDescending(r => r.CreatedAt).ToListAsync();

    public Task<Restaurant?> GetByIdAsync(Guid id) =>
        _db.Restaurants.FirstOrDefaultAsync(r => r.Id == id);

    public Task<Restaurant?> GetByIdWithMenuAsync(Guid id) =>
        _db.Restaurants
           .Include(r => r.Categories)
           .ThenInclude(c => c.MenuItems)
           .FirstOrDefaultAsync(r => r.Id == id && r.IsApproved && !r.IsDeleted);

    public Task<Restaurant?> GetByIdWithMenuAdminAsync(Guid id) =>
        _db.Restaurants
           .Include(r => r.Categories)
           .ThenInclude(c => c.MenuItems)
           .FirstOrDefaultAsync(r => r.Id == id);  // No filters - admin can see all

    public Task<Restaurant?> GetByPartnerIdAsync(Guid partnerId) =>
        _db.Restaurants.FirstOrDefaultAsync(r => r.PartnerUserId == partnerId && !r.IsDeleted);

    public Task<Restaurant?> GetDeletedByPartnerIdAsync(Guid partnerId) =>
        _db.Restaurants.FirstOrDefaultAsync(r => r.PartnerUserId == partnerId && r.IsDeleted);

    public async Task AddAsync(Restaurant r) => await _db.Restaurants.AddAsync(r);

    public Task UpdateAsync(Restaurant r)
    {
        _db.Restaurants.Update(r);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var restaurant = await _db.Restaurants.FindAsync(id);
        if (restaurant is not null)
        {
            // Soft delete instead of hard delete
            restaurant.IsDeleted = true;
            restaurant.DeletedAt = DateTime.UtcNow;
            // Note: DeletedBy and DeletionReason should be set by the caller
            _db.Restaurants.Update(restaurant);
        }
    }

    public async Task PermanentlyDeleteAsync(Guid id)
    {
        var restaurant = await _db.Restaurants.FindAsync(id);
        if (restaurant is not null)
        {
            // Hard delete - permanently remove from database
            _db.Restaurants.Remove(restaurant);
        }
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}

public class MenuItemRepository : IMenuItemRepository
{
    private readonly CatalogDbContext _db;
    public MenuItemRepository(CatalogDbContext db) => _db = db;

    public Task<MenuItem?> GetByIdAsync(Guid id) =>
        _db.MenuItems.FindAsync(id).AsTask();

    public Task<List<MenuItem>> GetByRestaurantIdAsync(Guid restaurantId) =>
        _db.MenuItems.Where(m => m.RestaurantId == restaurantId).ToListAsync();

    public Task<List<MenuItem>> SearchByNameOrDescriptionAsync(string query) =>
        _db.MenuItems
           .Include(m => m.Category)
           .ThenInclude(c => c.Restaurant)
           .Where(m => m.IsAvailable && 
                      m.Category.Restaurant.IsApproved &&
                      !m.Category.Restaurant.IsDeleted &&
                      m.Category.Restaurant.IsOpen &&
                      (m.Name.ToLower().Contains(query.ToLower()) ||
                       m.Description.ToLower().Contains(query.ToLower())))
           .ToListAsync();

    public async Task AddAsync(MenuItem item) => await _db.MenuItems.AddAsync(item);

    public Task UpdateAsync(MenuItem item)
    {
        _db.MenuItems.Update(item);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item is not null) _db.MenuItems.Remove(item);
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}


public class CategoryRepository : ICategoryRepository
{
    private readonly CatalogDbContext _db;
    public CategoryRepository(CatalogDbContext db) => _db = db;

    public Task<Category?> GetByIdAsync(Guid id) =>
        _db.Categories.FindAsync(id).AsTask();

    public Task<List<Category>> GetByRestaurantIdAsync(Guid restaurantId) =>
        _db.Categories
           .Where(c => c.RestaurantId == restaurantId)
           .OrderBy(c => c.DisplayOrder)
           .ToListAsync();

    public async Task AddAsync(Category category) => await _db.Categories.AddAsync(category);

    public Task UpdateAsync(Category category)
    {
        _db.Categories.Update(category);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is not null) _db.Categories.Remove(category);
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}

public class OperatingHourRepository : IOperatingHourRepository
{
    private readonly CatalogDbContext _db;
    public OperatingHourRepository(CatalogDbContext db) => _db = db;

    public Task<List<OperatingHour>> GetByRestaurantIdAsync(Guid restaurantId) =>
        _db.OperatingHours
           .Where(oh => oh.RestaurantId == restaurantId)
           .OrderBy(oh => oh.DayOfWeek)
           .ToListAsync();

    public async Task AddRangeAsync(List<OperatingHour> hours) =>
        await _db.OperatingHours.AddRangeAsync(hours);

    public async Task DeleteByRestaurantIdAsync(Guid restaurantId)
    {
        var hours = await _db.OperatingHours
            .Where(oh => oh.RestaurantId == restaurantId)
            .ToListAsync();
        _db.OperatingHours.RemoveRange(hours);
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}

public class ReviewRepository : IReviewRepository
{
    private readonly CatalogDbContext _db;
    public ReviewRepository(CatalogDbContext db) => _db = db;

    public Task<Review?> GetByIdAsync(Guid id) =>
        _db.Reviews.FindAsync(id).AsTask();

    public Task<List<Review>> GetByRestaurantIdAsync(Guid restaurantId, int page = 1, int pageSize = 10) =>
        _db.Reviews
           .Where(r => r.RestaurantId == restaurantId)
           .OrderByDescending(r => r.CreatedAt)
           .Skip((page - 1) * pageSize)
           .Take(pageSize)
           .ToListAsync();

    public Task<Review?> GetByUserAndRestaurantAsync(Guid userId, Guid restaurantId) =>
        _db.Reviews
           .FirstOrDefaultAsync(r => r.UserId == userId && r.RestaurantId == restaurantId);

    public Task<int> GetTotalCountByRestaurantAsync(Guid restaurantId) =>
        _db.Reviews.CountAsync(r => r.RestaurantId == restaurantId);

    public async Task<Dictionary<int, int>> GetRatingDistributionAsync(Guid restaurantId)
    {
        var distribution = await _db.Reviews
            .Where(r => r.RestaurantId == restaurantId)
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync();

        var result = new Dictionary<int, int>();
        for (int i = 1; i <= 5; i++)
        {
            result[i] = distribution.FirstOrDefault(d => d.Rating == i)?.Count ?? 0;
        }
        return result;
    }

    public async Task AddAsync(Review review) => await _db.Reviews.AddAsync(review);

    public Task UpdateAsync(Review review)
    {
        _db.Reviews.Update(review);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var review = await _db.Reviews.FindAsync(id);
        if (review is not null) _db.Reviews.Remove(review);
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
