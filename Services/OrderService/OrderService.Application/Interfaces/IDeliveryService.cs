using System;
using System.Collections.Generic;
using System.Text;
using OrderService.Application.DTOs;

namespace OrderService.Application.Interfaces;

public interface IDeliveryService
{
    Task<DeliveryAssignmentDto> AssignAgentAsync(AssignDeliveryAgentDto dto);
    Task<List<DeliveryAssignmentDto>> GetMyDeliveriesAsync(Guid agentId);
    Task<DeliveryAssignmentDto?> GetByIdAsync(Guid assignmentId);
    Task<DeliveryAssignmentDto?> GetByOrderIdAsync(Guid orderId);
    Task<DeliveryAssignmentDto> UpdateStatusAsync(Guid assignmentId, UpdateDeliveryStatusDto dto, Guid agentId);
    Task<List<DeliveryAssignmentDto>> GetPendingUnassignedAsync();
}