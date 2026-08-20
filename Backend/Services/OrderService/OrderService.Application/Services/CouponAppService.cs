using Microsoft.Extensions.Logging;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Services;

public class CouponAppService : ICouponService
{
    private readonly ICouponRepository _couponRepo;
    private readonly ILogger<CouponAppService> _logger;

    public CouponAppService(ICouponRepository couponRepo, ILogger<CouponAppService> logger)
    {
        _couponRepo = couponRepo;
        _logger = logger;
    }

    public async Task<CouponDto> CreateCouponAsync(CreateCouponDto dto, Guid createdBy, string role)
    {
        // Validate dates
        if (dto.ValidFrom >= dto.ValidUntil)
            throw new ArgumentException("ValidFrom must be before ValidUntil.");

        // Check if code already exists
        var existing = await _couponRepo.GetByCodeAsync(dto.Code);
        if (existing != null)
            throw new InvalidOperationException($"Coupon code '{dto.Code}' already exists.");

        // Partners can only create restaurant-specific coupons
        if (role == "Partner" && !dto.RestaurantId.HasValue)
            throw new UnauthorizedAccessException("Partners must specify a restaurant ID.");

        // Admins can create platform-wide or restaurant-specific
        if (!Enum.TryParse<CouponType>(dto.Type, ignoreCase: true, out var couponType))
            throw new ArgumentException("Invalid coupon type. Use 'Percentage' or 'FixedAmount'.");

        var coupon = new Coupon
        {
            Code = dto.Code.ToUpperInvariant(),
            Description = dto.Description,
            Type = couponType,
            Value = dto.Value,
            MinOrderAmount = dto.MinOrderAmount,
            MaxDiscountAmount = dto.MaxDiscountAmount,
            UsageLimit = dto.UsageLimit,
            ValidFrom = dto.ValidFrom,
            ValidUntil = dto.ValidUntil,
            RestaurantId = dto.RestaurantId,
            CreatedBy = createdBy
        };

        await _couponRepo.AddAsync(coupon);
        await _couponRepo.SaveChangesAsync();

        return MapToDto(coupon);
    }

    public async Task<CouponDto> GetByIdAsync(Guid id)
    {
        var coupon = await _couponRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Coupon not found.");
        return MapToDto(coupon);
    }

    public async Task<List<CouponDto>> GetAllCouponsAsync()
    {
        var coupons = await _couponRepo.GetAllAsync();
        return coupons.Select(MapToDto).ToList();
    }

    public async Task<List<CouponDto>> GetMyCouponsAsync(Guid restaurantId)
    {
        var coupons = await _couponRepo.GetByRestaurantIdAsync(restaurantId);
        return coupons.Select(MapToDto).ToList();
    }

    public async Task<List<CouponDto>> GetActiveCouponsAsync()
    {
        var coupons = await _couponRepo.GetActiveAsync();
        return coupons.Select(MapToDto).ToList();
    }

    public async Task<CouponDto> UpdateCouponAsync(Guid id, UpdateCouponDto dto, Guid userId, string role)
    {
        _logger.LogInformation("[COUPON] UpdateCouponAsync called - CouponId: {CouponId}, UserId: {UserId}, Role: {Role}", id, userId, role);
        
        var coupon = await _couponRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Coupon not found.");

        _logger.LogInformation("[COUPON] Found coupon - Code: {Code}, Current Values - Description: {Desc}, Value: {Value}, MinOrder: {MinOrder}, UsageLimit: {UsageLimit}, IsActive: {IsActive}", 
            coupon.Code, coupon.Description, coupon.Value, coupon.MinOrderAmount, coupon.UsageLimit, coupon.IsActive);

        // Partners can only update their own restaurant coupons
        if (role == "Partner" && coupon.CreatedBy != userId)
            throw new UnauthorizedAccessException("You can only update your own coupons.");

        _logger.LogInformation("[COUPON] Updating with new values - Description: {Desc}, Value: {Value}, MinOrder: {MinOrder}, UsageLimit: {UsageLimit}, IsActive: {IsActive}", 
            dto.Description, dto.Value, dto.MinOrderAmount, dto.UsageLimit, dto.IsActive);

        coupon.Description = dto.Description;
        coupon.Value = dto.Value;
        coupon.MinOrderAmount = dto.MinOrderAmount;
        coupon.MaxDiscountAmount = dto.MaxDiscountAmount;
        coupon.UsageLimit = dto.UsageLimit;
        coupon.ValidUntil = dto.ValidUntil;
        coupon.IsActive = dto.IsActive;
        coupon.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("[COUPON] After assignment - Description: {Desc}, Value: {Value}, MinOrder: {MinOrder}, UsageLimit: {UsageLimit}, IsActive: {IsActive}", 
            coupon.Description, coupon.Value, coupon.MinOrderAmount, coupon.UsageLimit, coupon.IsActive);

        await _couponRepo.UpdateAsync(coupon);
        _logger.LogInformation("[COUPON] UpdateAsync called on repository");
        
        await _couponRepo.SaveChangesAsync();
        _logger.LogInformation("[COUPON] SaveChangesAsync completed");

        // Verify the update by fetching again
        var verifiedCoupon = await _couponRepo.GetByIdAsync(id);
        _logger.LogInformation("[COUPON] Verification fetch - Description: {Desc}, Value: {Value}, MinOrder: {MinOrder}, UsageLimit: {UsageLimit}, IsActive: {IsActive}", 
            verifiedCoupon?.Description, verifiedCoupon?.Value, verifiedCoupon?.MinOrderAmount, verifiedCoupon?.UsageLimit, verifiedCoupon?.IsActive);

        return MapToDto(coupon);
    }

    public async Task DeleteCouponAsync(Guid id, Guid userId, string role)
    {
        var coupon = await _couponRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Coupon not found.");

        // Partners can only delete their own restaurant coupons
        if (role == "Partner" && coupon.CreatedBy != userId)
            throw new UnauthorizedAccessException("You can only delete your own coupons.");

        await _couponRepo.DeleteAsync(id);
        await _couponRepo.SaveChangesAsync();
    }

    public async Task<CouponValidationResultDto> ValidateCouponAsync(ValidateCouponDto dto)
    {
        var coupon = await _couponRepo.GetByCodeAsync(dto.CouponCode);

        if (coupon == null)
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                ErrorMessage = "Coupon code not found."
            };
        }

        // Check if active
        if (!coupon.IsActive)
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                ErrorMessage = "This coupon is no longer active."
            };
        }

        // Check date validity
        var now = DateTime.UtcNow;
        if (now < coupon.ValidFrom)
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                ErrorMessage = $"This coupon is not yet valid. Valid from {coupon.ValidFrom:MMM dd, yyyy}."
            };
        }

        if (now > coupon.ValidUntil)
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                ErrorMessage = "This coupon has expired."
            };
        }

        // Check usage limit
        if (coupon.UsedCount >= coupon.UsageLimit)
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                ErrorMessage = "This coupon has reached its usage limit."
            };
        }

        // Check minimum order amount
        if (dto.OrderAmount < coupon.MinOrderAmount)
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                ErrorMessage = $"Minimum order amount of ₹{coupon.MinOrderAmount} required."
            };
        }

        // Check restaurant restriction
        if (coupon.RestaurantId.HasValue && dto.RestaurantId != coupon.RestaurantId)
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                ErrorMessage = "This coupon is not valid for this restaurant."
            };
        }

        // Calculate discount
        decimal discount = coupon.Type == CouponType.Percentage
            ? Math.Round(dto.OrderAmount * (coupon.Value / 100m), 2)
            : coupon.Value;

        // Apply max discount cap for percentage coupons
        if (coupon.Type == CouponType.Percentage && coupon.MaxDiscountAmount.HasValue)
        {
            discount = Math.Min(discount, coupon.MaxDiscountAmount.Value);
        }

        return new CouponValidationResultDto
        {
            IsValid = true,
            DiscountAmount = discount,
            Coupon = MapToDto(coupon)
        };
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static CouponDto MapToDto(Coupon c)
    {
        var now = DateTime.UtcNow;
        return new CouponDto
        {
            Id = c.Id,
            Code = c.Code,
            Description = c.Description,
            Type = c.Type.ToString(),
            Value = c.Value,
            MinOrderAmount = c.MinOrderAmount,
            MaxDiscountAmount = c.MaxDiscountAmount,
            UsageLimit = c.UsageLimit,
            UsedCount = c.UsedCount,
            RemainingUses = Math.Max(0, c.UsageLimit - c.UsedCount),
            ValidFrom = c.ValidFrom,
            ValidUntil = c.ValidUntil,
            IsActive = c.IsActive,
            IsExpired = now > c.ValidUntil,
            RestaurantId = c.RestaurantId,
            Scope = c.RestaurantId.HasValue ? "Restaurant-specific" : "Platform-wide",
            CreatedAt = c.CreatedAt
        };
    }
}
