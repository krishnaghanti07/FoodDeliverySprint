using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IAddressService
{
    Task<AddressDto?> GetByIdAsync(Guid id, Guid userId);
    Task<List<AddressDto>> GetMyAddressesAsync(Guid userId);
    Task<AddressDto?> GetDefaultAddressAsync(Guid userId);
    Task<AddressDto> CreateAddressAsync(Guid userId, CreateAddressDto dto);
    Task<AddressDto> UpdateAddressAsync(Guid id, Guid userId, UpdateAddressDto dto);
    Task DeleteAddressAsync(Guid id, Guid userId);
    Task SetDefaultAddressAsync(Guid userId, Guid addressId);
}
