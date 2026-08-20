using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class AddressRepository : IAddressRepository
{
    private readonly AuthDbContext _context;

    public AddressRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<Address?> GetByIdAsync(Guid id)
    {
        return await _context.Addresses.FindAsync(id);
    }

    public async Task<List<Address>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Addresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Address?> GetDefaultAddressAsync(Guid userId)
    {
        return await _context.Addresses
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);
    }

    public async Task<Address> CreateAsync(Address address)
    {
        // If this is set as default, unset other defaults
        if (address.IsDefault)
        {
            var existingDefaults = await _context.Addresses
                .Where(a => a.UserId == address.UserId && a.IsDefault)
                .ToListAsync();

            foreach (var addr in existingDefaults)
            {
                addr.IsDefault = false;
            }
        }

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();
        return address;
    }

    public async Task<Address> UpdateAsync(Address address)
    {
        address.UpdatedAt = DateTime.UtcNow;
        _context.Addresses.Update(address);
        await _context.SaveChangesAsync();
        return address;
    }

    public async Task DeleteAsync(Guid id)
    {
        var address = await _context.Addresses.FindAsync(id);
        if (address != null)
        {
            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();
        }
    }

    public async Task SetDefaultAddressAsync(Guid userId, Guid addressId)
    {
        // Unset all defaults for this user
        var allAddresses = await _context.Addresses
            .Where(a => a.UserId == userId)
            .ToListAsync();

        foreach (var addr in allAddresses)
        {
            addr.IsDefault = (addr.Id == addressId);
            addr.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}
