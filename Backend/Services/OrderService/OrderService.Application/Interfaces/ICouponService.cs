using OrderService.Application.DTOs;

namespace OrderService.Application.Interfaces;

public interface ICouponService
{
    Task<CouponDto> CreateCouponAsync(CreateCouponDto dto, Guid createdBy, string role);
    Task<CouponDto> GetByIdAsync(Guid id);
    Task<List<CouponDto>> GetAllCouponsAsync();
    Task<List<CouponDto>> GetMyCouponsAsync(Guid restaurantId);
    Task<List<CouponDto>> GetActiveCouponsAsync();
    Task<CouponDto> UpdateCouponAsync(Guid id, UpdateCouponDto dto, Guid userId, string role);
    Task DeleteCouponAsync(Guid id, Guid userId, string role);
    Task<CouponValidationResultDto> ValidateCouponAsync(ValidateCouponDto dto);
}
