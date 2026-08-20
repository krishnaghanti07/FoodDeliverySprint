using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly OrderDbContext _db;
    public CartRepository(OrderDbContext db) => _db = db;

    public Task<Cart?> GetByCustomerIdAsync(Guid customerId) =>
        _db.Carts
           .Include(c => c.Items)
           .FirstOrDefaultAsync(c => c.CustomerId == customerId);

    public Task<Cart?> GetByCustomerIdNoTrackingAsync(Guid customerId) =>
        _db.Carts
           .AsNoTracking()
           .Include(c => c.Items)
           .FirstOrDefaultAsync(c => c.CustomerId == customerId);

    public async Task AddAsync(Cart cart) => await _db.Carts.AddAsync(cart);

    public Task UpdateAsync(Cart cart)
    {
        _db.Carts.Update(cart);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid customerId)
    {
        var cart = await _db.Carts.Include(c => c.Items)
                                  .FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (cart is not null) _db.Carts.Remove(cart);
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
