using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;

public interface IAddressRepository
{
    Task<Address?> GetByIdAsync(Guid id);
    Task<List<Address>> GetByUserIdAsync(Guid userId);
    Task<Address?> GetDefaultAddressAsync(Guid userId);
    Task<Address> CreateAsync(Address address);
    Task<Address> UpdateAsync(Address address);
    Task DeleteAsync(Guid id);
    Task SetDefaultAddressAsync(Guid userId, Guid addressId);
}
