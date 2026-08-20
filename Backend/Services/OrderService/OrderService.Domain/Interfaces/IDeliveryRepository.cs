using System;
using System.Collections.Generic;
using System.Text;
using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

public interface IDeliveryRepository
{
    Task<DeliveryAssignment?> GetByIdAsync(Guid id);
    Task<DeliveryAssignment?> GetByOrderIdAsync(Guid orderId);
    Task<List<DeliveryAssignment>> GetByAgentIdAsync(Guid agentId);
    Task<List<DeliveryAssignment>> GetPendingUnassignedAsync();
    Task AddAsync(DeliveryAssignment assignment);
    Task AddStatusHistoryAsync(DeliveryStatusHistory history);
    Task UpdateAsync(DeliveryAssignment assignment);
    Task SaveChangesAsync();
}
