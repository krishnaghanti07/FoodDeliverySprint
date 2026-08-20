using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;
using AdminService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Infrastructure.Repositories;

public class ComplaintRepository : IComplaintRepository
{
    private readonly AdminDbContext _db;
    public ComplaintRepository(AdminDbContext db) => _db = db;

    public async Task<List<Complaint>> GetAllAsync(string? status, string? type)
    {
        var q = _db.Complaints.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(c => c.Status == status);
        
        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(c => c.Type == type);

        return await q.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public Task<Complaint?> GetByIdAsync(Guid id) =>
        _db.Complaints.FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(Complaint complaint)
    {
        await _db.Complaints.AddAsync(complaint);
    }

    public Task UpdateAsync(Complaint complaint)
    {
        _db.Complaints.Update(complaint);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
