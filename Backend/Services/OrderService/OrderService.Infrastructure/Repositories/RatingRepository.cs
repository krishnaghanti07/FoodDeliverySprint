using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

public class RatingRepository : IRatingRepository
{
    private readonly OrderDbContext _db;
    public RatingRepository(OrderDbContext db) => _db = db;

    public Task<OrderRating?> GetByIdAsync(Guid id) =>
        _db.Set<OrderRating>().FindAsync(id).AsTask();

    public Task<OrderRating?> GetByOrderIdAsync(Guid orderId) =>
        _db.Set<OrderRating>().FirstOrDefaultAsync(r => r.OrderId == orderId);

    public Task<List<OrderRating>> GetByCustomerIdAsync(Guid customerId) =>
        _db.Set<OrderRating>()
           .Where(r => r.CustomerId == customerId)
           .OrderByDescending(r => r.CreatedAt)
           .ToListAsync();

    public async Task AddAsync(OrderRating rating) =>
        await _db.Set<OrderRating>().AddAsync(rating);

    public Task UpdateAsync(OrderRating rating)
    {
        _db.Set<OrderRating>().Update(rating);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var rating = await _db.Set<OrderRating>().FindAsync(id);
        if (rating is not null)
            _db.Set<OrderRating>().Remove(rating);
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
