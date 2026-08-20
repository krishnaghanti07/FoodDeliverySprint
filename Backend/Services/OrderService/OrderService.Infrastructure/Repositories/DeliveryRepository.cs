using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

public class DeliveryRepository : IDeliveryRepository
{
    private readonly OrderDbContext _db;
    public DeliveryRepository(OrderDbContext db) => _db = db;

    public Task<DeliveryAssignment?> GetByIdAsync(Guid id) =>
        _db.DeliveryAssignments
           .Include(d => d.StatusHistory.OrderBy(h => h.ChangedAt))
           .FirstOrDefaultAsync(d => d.Id == id);

    public Task<DeliveryAssignment?> GetByOrderIdAsync(Guid orderId) =>
        _db.DeliveryAssignments
           .Include(d => d.StatusHistory)
           .FirstOrDefaultAsync(d => d.OrderId == orderId);

    public Task<List<DeliveryAssignment>> GetByAgentIdAsync(Guid agentId) =>
        _db.DeliveryAssignments
           .Include(d => d.StatusHistory.OrderBy(h => h.ChangedAt))
           .Where(d => d.AgentId == agentId)
           .OrderByDescending(d => d.AssignedAt)
           .ToListAsync();

    public Task<List<DeliveryAssignment>> GetPendingUnassignedAsync() =>
        _db.DeliveryAssignments
           .Include(d => d.Order)
           .Where(d => d.Status == DeliveryStatus.Assigned &&
                       d.Order.Status == OrderStatus.ReadyForPickup)
           .ToListAsync();

    public async Task AddAsync(DeliveryAssignment a) =>
        await _db.DeliveryAssignments.AddAsync(a);

    public async Task AddStatusHistoryAsync(DeliveryStatusHistory history) =>
        await _db.DeliveryStatusHistory.AddAsync(history);

    public Task UpdateAsync(DeliveryAssignment a)
    {
        _db.DeliveryAssignments.Update(a);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}