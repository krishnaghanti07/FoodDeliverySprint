using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Services;

public class AddressService : IAddressService
{
    private readonly IAddressRepository _addressRepository;

    public AddressService(IAddressRepository addressRepository)
    {
        _addressRepository = addressRepository;
    }

    public async Task<AddressDto?> GetByIdAsync(Guid id, Guid userId)
    {
        var address = await _addressRepository.GetByIdAsync(id);
        if (address == null || address.UserId != userId)
            return null;

        return MapToDto(address);
    }

    public async Task<List<AddressDto>> GetMyAddressesAsync(Guid userId)
    {
        var addresses = await _addressRepository.GetByUserIdAsync(userId);
        return addresses.Select(MapToDto).ToList();
    }

    public async Task<AddressDto?> GetDefaultAddressAsync(Guid userId)
    {
        var address = await _addressRepository.GetDefaultAddressAsync(userId);
        return address == null ? null : MapToDto(address);
    }

    public async Task<AddressDto> CreateAddressAsync(Guid userId, CreateAddressDto dto)
    {
        var address = new Address
        {
            UserId = userId,
            Label = dto.Label,
            FullAddress = dto.FullAddress,
            City = dto.City,
            State = dto.State,
            Pincode = dto.Pincode,
            Landmark = dto.Landmark,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            IsDefault = dto.IsDefault
        };

        var created = await _addressRepository.CreateAsync(address);
        return MapToDto(created);
    }

    public async Task<AddressDto> UpdateAddressAsync(Guid id, Guid userId, UpdateAddressDto dto)
    {
        var address = await _addressRepository.GetByIdAsync(id);
        if (address == null || address.UserId != userId)
            throw new UnauthorizedAccessException("Address not found or access denied");

        address.Label = dto.Label;
        address.FullAddress = dto.FullAddress;
        address.City = dto.City;
        address.State = dto.State;
        address.Pincode = dto.Pincode;
        address.Landmark = dto.Landmark;
        address.Latitude = dto.Latitude;
        address.Longitude = dto.Longitude;

        var updated = await _addressRepository.UpdateAsync(address);
        return MapToDto(updated);
    }

    public async Task DeleteAddressAsync(Guid id, Guid userId)
    {
        var address = await _addressRepository.GetByIdAsync(id);
        if (address == null || address.UserId != userId)
            throw new UnauthorizedAccessException("Address not found or access denied");

        await _addressRepository.DeleteAsync(id);
    }

    public async Task SetDefaultAddressAsync(Guid userId, Guid addressId)
    {
        var address = await _addressRepository.GetByIdAsync(addressId);
        if (address == null || address.UserId != userId)
            throw new UnauthorizedAccessException("Address not found or access denied");

        await _addressRepository.SetDefaultAddressAsync(userId, addressId);
    }

    private static AddressDto MapToDto(Address address)
    {
        return new AddressDto
        {
            Id = address.Id,
            UserId = address.UserId,
            Label = address.Label,
            FullAddress = address.FullAddress,
            City = address.City,
            State = address.State,
            Pincode = address.Pincode,
            Landmark = address.Landmark,
            Latitude = address.Latitude,
            Longitude = address.Longitude,
            IsDefault = address.IsDefault,
            CreatedAt = address.CreatedAt,
            UpdatedAt = address.UpdatedAt
        };
    }
}
