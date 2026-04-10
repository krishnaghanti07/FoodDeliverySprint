using System;
using System.Collections.Generic;
using System.Text;
using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByCustomerIdAsync(Guid customerId);
    Task AddAsync(Cart cart);
    Task UpdateAsync(Cart cart);
    Task DeleteAsync(Guid customerId);
    Task SaveChangesAsync();
}
