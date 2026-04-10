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
        var q = _db.Restaurants.Where(r => r.IsApproved).AsQueryable();

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
           .FirstOrDefaultAsync(r => r.Id == id && r.IsApproved);

    public Task<Restaurant?> GetByPartnerIdAsync(Guid partnerId) =>
        _db.Restaurants.FirstOrDefaultAsync(r => r.PartnerUserId == partnerId);

    public async Task AddAsync(Restaurant r) => await _db.Restaurants.AddAsync(r);

    public Task UpdateAsync(Restaurant r)
    {
        _db.Restaurants.Update(r);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var restaurant = await _db.Restaurants.FindAsync(id);
        if (restaurant is not null) _db.Restaurants.Remove(restaurant);
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