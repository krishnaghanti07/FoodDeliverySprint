using CatalogService.Application.DTOs;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Interfaces;

namespace CatalogService.Application.Services;

public class CatalogAppService
{
    private readonly IRestaurantRepository _restaurantRepo;
    private readonly IMenuItemRepository _menuItemRepo;

    public CatalogAppService(
        IRestaurantRepository restaurantRepo,
        IMenuItemRepository menuItemRepo)
    {
        _restaurantRepo = restaurantRepo;
        _menuItemRepo = menuItemRepo;
    }

    // ── Restaurants ────────────────────────────────────────────────────

    public async Task<List<RestaurantListDto>> GetRestaurantsAsync(
        string? city, string? cuisine, string? search)
    {
        var list = await _restaurantRepo.GetAllApprovedAsync(city, cuisine, search);
        return list.Select(MapToListDto).ToList();
    }

    public async Task<List<RestaurantListDto>> GetAllIncludingUnapprovedAsync()
    {
        var list = await _restaurantRepo.GetAllAsync();
        return list.Select(MapToListDto).ToList();
    }

    public async Task<RestaurantDetailDto?> GetRestaurantWithMenuAsync(Guid id)
    {
        var r = await _restaurantRepo.GetByIdWithMenuAsync(id);
        return r is null ? null : MapToDetailDto(r);
    }

    public async Task<Guid> CreateRestaurantAsync(CreateRestaurantDto dto, Guid partnerId)
    {
        var restaurant = new Restaurant
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            Cuisine = dto.Cuisine,
            Address = dto.Address,
            City = dto.City,
            Phone = dto.Phone,
            LogoUrl = dto.LogoUrl,
            PrepTimeMinutes = dto.PrepTimeMinutes,
            MinOrderAmount = dto.MinOrderAmount,
            DeliveryFee = dto.DeliveryFee,
            PartnerUserId = partnerId,
            IsApproved = false
        };
        await _restaurantRepo.AddAsync(restaurant);
        await _restaurantRepo.SaveChangesAsync();
        return restaurant.Id;
    }

    public async Task UpdateRestaurantAsync(
        Guid id, CreateRestaurantDto dto, Guid requesterId, string role)
    {
        var restaurant = await _restaurantRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Restaurant not found.");

        if (role == "Partner" && restaurant.PartnerUserId != requesterId)
            throw new UnauthorizedAccessException(
                "You can only update your own restaurant.");

        restaurant.Name = dto.Name.Trim();
        restaurant.Description = dto.Description;
        restaurant.Cuisine = dto.Cuisine;
        restaurant.Address = dto.Address;
        restaurant.City = dto.City;
        restaurant.Phone = dto.Phone;
        restaurant.LogoUrl = dto.LogoUrl;
        restaurant.PrepTimeMinutes = dto.PrepTimeMinutes;
        restaurant.MinOrderAmount = dto.MinOrderAmount;
        restaurant.DeliveryFee = dto.DeliveryFee;

        await _restaurantRepo.UpdateAsync(restaurant);
        await _restaurantRepo.SaveChangesAsync();
    }

    public async Task<bool> ToggleOpenStatusAsync(Guid id, Guid partnerId)
    {
        var restaurant = await _restaurantRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Restaurant not found.");

        if (restaurant.PartnerUserId != partnerId)
            throw new UnauthorizedAccessException("Access denied.");

        restaurant.IsOpen = !restaurant.IsOpen;
        await _restaurantRepo.UpdateAsync(restaurant);
        await _restaurantRepo.SaveChangesAsync();
        return restaurant.IsOpen;
    }

    public async Task ApproveRestaurantAsync(Guid id)
    {
        var restaurant = await _restaurantRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Restaurant not found.");
        restaurant.IsApproved = true;
        await _restaurantRepo.UpdateAsync(restaurant);
        await _restaurantRepo.SaveChangesAsync();
    }

    public async Task DeleteRestaurantAsync(Guid id, Guid requesterId, string role)
    {
        var restaurant = await _restaurantRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Restaurant not found.");

        if (role == "Partner" && restaurant.PartnerUserId != requesterId)
            throw new UnauthorizedAccessException(
                "You can only delete your own restaurant.");

        await _restaurantRepo.DeleteAsync(id);
        await _restaurantRepo.SaveChangesAsync();
    }

    // ── Menu Items ─────────────────────────────────────────────────────

    public async Task<List<MenuItemDto>> GetMenuItemsByRestaurantAsync(Guid restaurantId)
    {
        var items = await _menuItemRepo.GetByRestaurantIdAsync(restaurantId);
        return items.Select(m => new MenuItemDto
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            Price = m.Price,
            IsVeg = m.IsVeg,
            IsAvailable = m.IsAvailable,
            ImageUrl = m.ImageUrl
        }).ToList();
    }

    public async Task<MenuItemDto?> GetMenuItemByIdAsync(Guid id)
    {
        var item = await _menuItemRepo.GetByIdAsync(id);
        if (item is null) return null;
        
        return new MenuItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            IsVeg = item.IsVeg,
            IsAvailable = item.IsAvailable,
            ImageUrl = item.ImageUrl
        };
    }

    public async Task<Guid> AddMenuItemAsync(CreateMenuItemDto dto)
    {
        var item = new MenuItem
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            Price = dto.Price,
            IsVeg = dto.IsVeg,
            ImageUrl = dto.ImageUrl,
            CategoryId = dto.CategoryId,
            RestaurantId = dto.RestaurantId
        };
        await _menuItemRepo.AddAsync(item);
        await _menuItemRepo.SaveChangesAsync();
        return item.Id;
    }

    public async Task UpdateMenuItemAsync(Guid id, CreateMenuItemDto dto)
    {
        var item = await _menuItemRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Menu item not found.");

        item.Name = dto.Name.Trim();
        item.Description = dto.Description;
        item.Price = dto.Price;
        item.IsVeg = dto.IsVeg;
        item.ImageUrl = dto.ImageUrl;

        await _menuItemRepo.UpdateAsync(item);
        await _menuItemRepo.SaveChangesAsync();
    }

    public async Task<bool> ToggleMenuItemAvailabilityAsync(Guid id)
    {
        var item = await _menuItemRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Menu item not found.");
        item.IsAvailable = !item.IsAvailable;
        await _menuItemRepo.UpdateAsync(item);
        await _menuItemRepo.SaveChangesAsync();
        return item.IsAvailable;
    }

    public async Task DeleteMenuItemAsync(Guid id)
    {
        var item = await _menuItemRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Menu item not found.");
        await _menuItemRepo.DeleteAsync(id);
        await _menuItemRepo.SaveChangesAsync();
    }

    // ── Mappings ───────────────────────────────────────────────────────

    private static RestaurantListDto MapToListDto(Restaurant r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Cuisine = r.Cuisine,
        City = r.City,
        LogoUrl = r.LogoUrl,
        Rating = r.Rating,
        PrepTimeMinutes = r.PrepTimeMinutes,
        DeliveryFee = r.DeliveryFee,
        IsOpen = r.IsOpen,
        IsApproved = r.IsApproved
    };

    private static RestaurantDetailDto MapToDetailDto(Restaurant r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        Cuisine = r.Cuisine,
        City = r.City,
        Address = r.Address,
        Phone = r.Phone,
        LogoUrl = r.LogoUrl,
        Rating = r.Rating,
        PrepTimeMinutes = r.PrepTimeMinutes,
        DeliveryFee = r.DeliveryFee,
        MinOrderAmount = r.MinOrderAmount,
        IsOpen = r.IsOpen,
        IsApproved = r.IsApproved,
        Categories = r.Categories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                DisplayOrder = c.DisplayOrder,
                MenuItems = c.MenuItems
                    .Where(m => m.IsAvailable)
                    .Select(m => new MenuItemDto
                    {
                        Id = m.Id,
                        Name = m.Name,
                        Description = m.Description,
                        Price = m.Price,
                        IsVeg = m.IsVeg,
                        IsAvailable = m.IsAvailable,
                        ImageUrl = m.ImageUrl
                    }).ToList()
            }).ToList()
    };
}