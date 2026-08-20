using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;

namespace AdminService.Application.Services;

public class AdminRestaurantService : IAdminRestaurantService
{
    private readonly IRestaurantSnapshotRepository _restaurantRepo;
    private readonly IAdminAuditLogRepository _auditRepo;

    public AdminRestaurantService(
        IRestaurantSnapshotRepository restaurantRepo,
        IAdminAuditLogRepository auditRepo)
    {
        _restaurantRepo = restaurantRepo;
        _auditRepo = auditRepo;
    }

    public async Task<List<RestaurantListDto>> GetAllRestaurantsAsync(string? status, int? page, int? pageSize)
    {
        var restaurants = await _restaurantRepo.GetAllAsync(status, page, pageSize);
        return restaurants.Select(r => new RestaurantListDto
        {
            Id = r.Id,
            Name = r.Name,
            PartnerName = r.PartnerName,
            Status = r.Status,
            IsOpen = r.IsOpen,
            AverageRating = r.AverageRating,
            TotalOrders = r.TotalOrders,
            TotalRevenue = r.TotalRevenue,
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    public async Task<RestaurantDetailDto?> GetRestaurantByIdAsync(Guid id)
    {
        var r = await _restaurantRepo.GetByIdAsync(id);
        if (r is null) return null;

        return new RestaurantDetailDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            Address = r.Address,
            Phone = r.Phone,
            PartnerId = r.PartnerId,
            PartnerName = r.PartnerName,
            Status = r.Status,
            IsOpen = r.IsOpen,
            AverageRating = r.AverageRating,
            TotalOrders = r.TotalOrders,
            TotalRevenue = r.TotalRevenue,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }

    public async Task<RestaurantDetailDto> ApproveRestaurantAsync(Guid id, ApproveRestaurantDto dto, Guid adminId)
    {
        var restaurant = await _restaurantRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Restaurant {id} not found.");

        if (restaurant.Status != "Pending")
            throw new InvalidOperationException($"Restaurant is not pending approval. Current status: {restaurant.Status}");

        var oldStatus = restaurant.Status;
        await _restaurantRepo.UpdateStatusAsync(id, "Approved");
        await _restaurantRepo.SaveChangesAsync();

        await _auditRepo.AddAsync(new AdminAuditLog
        {
            AdminUserId = adminId,
            Action = "ApproveRestaurant",
            EntityType = "Restaurant",
            EntityId = id,
            OldValue = oldStatus,
            NewValue = "Approved",
            Reason = dto.Notes ?? "Restaurant approved"
        });
        await _auditRepo.SaveChangesAsync();

        // Fetch updated restaurant
        var updated = await _restaurantRepo.GetByIdAsync(id);
        return new RestaurantDetailDto
        {
            Id = updated!.Id,
            Name = updated.Name,
            Description = updated.Description,
            Address = updated.Address,
            Phone = updated.Phone,
            PartnerId = updated.PartnerId,
            PartnerName = updated.PartnerName,
            Status = updated.Status,
            IsOpen = updated.IsOpen,
            AverageRating = updated.AverageRating,
            TotalOrders = updated.TotalOrders,
            TotalRevenue = updated.TotalRevenue,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt
        };
    }

    public async Task<RestaurantDetailDto> UpdateRestaurantStatusAsync(Guid id, UpdateRestaurantStatusDto dto, Guid adminId)
    {
        var restaurant = await _restaurantRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Restaurant {id} not found.");

        var oldStatus = restaurant.Status;
        await _restaurantRepo.UpdateStatusAsync(id, dto.Status);
        await _restaurantRepo.SaveChangesAsync();

        await _auditRepo.AddAsync(new AdminAuditLog
        {
            AdminUserId = adminId,
            Action = "UpdateRestaurantStatus",
            EntityType = "Restaurant",
            EntityId = id,
            OldValue = oldStatus,
            NewValue = dto.Status,
            Reason = dto.Reason
        });
        await _auditRepo.SaveChangesAsync();

        // Fetch updated restaurant
        var updated = await _restaurantRepo.GetByIdAsync(id);
        return new RestaurantDetailDto
        {
            Id = updated!.Id,
            Name = updated.Name,
            Description = updated.Description,
            Address = updated.Address,
            Phone = updated.Phone,
            PartnerId = updated.PartnerId,
            PartnerName = updated.PartnerName,
            Status = updated.Status,
            IsOpen = updated.IsOpen,
            AverageRating = updated.AverageRating,
            TotalOrders = updated.TotalOrders,
            TotalRevenue = updated.TotalRevenue,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt
        };
    }

    public async Task RejectRestaurantAsync(Guid id, RejectRestaurantDto dto, Guid adminId)
    {
        var restaurant = await _restaurantRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Restaurant {id} not found.");

        if (restaurant.Status != "Pending")
            throw new InvalidOperationException($"Restaurant is not pending approval. Current status: {restaurant.Status}");

        var oldStatus = restaurant.Status;
        await _restaurantRepo.UpdateStatusAsync(id, "Rejected");
        await _restaurantRepo.SaveChangesAsync();

        await _auditRepo.AddAsync(new AdminAuditLog
        {
            AdminUserId = adminId,
            Action = "RejectRestaurant",
            EntityType = "Restaurant",
            EntityId = id,
            OldValue = oldStatus,
            NewValue = "Rejected",
            Reason = dto.Reason
        });
        await _auditRepo.SaveChangesAsync();
    }

    public async Task<RestaurantDetailDto> ToggleRestaurantActiveAsync(Guid id, ToggleActiveDto dto, Guid adminId)
    {
        var restaurant = await _restaurantRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Restaurant {id} not found.");

        var oldStatus = restaurant.Status;
        var newStatus = dto.IsActive ? "Approved" : "Disabled";
        
        await _restaurantRepo.UpdateStatusAsync(id, newStatus);
        await _restaurantRepo.SaveChangesAsync();

        await _auditRepo.AddAsync(new AdminAuditLog
        {
            AdminUserId = adminId,
            Action = "ToggleRestaurantActive",
            EntityType = "Restaurant",
            EntityId = id,
            OldValue = oldStatus,
            NewValue = newStatus,
            Reason = dto.Reason
        });
        await _auditRepo.SaveChangesAsync();

        // Fetch updated restaurant
        var updated = await _restaurantRepo.GetByIdAsync(id);
        return new RestaurantDetailDto
        {
            Id = updated!.Id,
            Name = updated.Name,
            Description = updated.Description,
            Address = updated.Address,
            Phone = updated.Phone,
            PartnerId = updated.PartnerId,
            PartnerName = updated.PartnerName,
            Status = updated.Status,
            IsOpen = updated.IsOpen,
            AverageRating = updated.AverageRating,
            TotalOrders = updated.TotalOrders,
            TotalRevenue = updated.TotalRevenue,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt
        };
    }
}
