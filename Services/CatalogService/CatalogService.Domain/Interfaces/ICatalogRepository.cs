using CatalogService.Domain.Entities;

namespace CatalogService.Domain.Interfaces;

public interface IRestaurantRepository
{
    Task<List<Restaurant>> GetAllApprovedAsync(string? city, string? cuisine, string? search);
    Task<List<Restaurant>> GetAllAsync();                          // Admin: all incl unapproved
    Task<Restaurant?> GetByIdAsync(Guid id);                      // lightweight — no includes
    Task<Restaurant?> GetByIdWithMenuAsync(Guid id);
    Task<Restaurant?> GetByPartnerIdAsync(Guid partnerId);
    Task AddAsync(Restaurant restaurant);
    Task UpdateAsync(Restaurant restaurant);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}

public interface IMenuItemRepository
{
    Task<MenuItem?> GetByIdAsync(Guid id);
    Task<List<MenuItem>> GetByRestaurantIdAsync(Guid restaurantId);
    Task AddAsync(MenuItem item);
    Task UpdateAsync(MenuItem item);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}