using System;
using System.Collections.Generic;
using System.Text;
using FoodDelivery.Shared.Constants;
using FoodDelivery.Shared.Events;
using FoodDelivery.Shared.Messaging;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Services;

public class DeliveryAppService : IDeliveryService
{
    private readonly IDeliveryRepository _deliveryRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IRabbitMqPublisher _publisher;

    public DeliveryAppService(
        IDeliveryRepository deliveryRepo,
        IOrderRepository orderRepo,
        IRabbitMqPublisher publisher)
    {
        _deliveryRepo = deliveryRepo;
        _orderRepo = orderRepo;
        _publisher = publisher;
    }

    public async Task<DeliveryAssignmentDto> AssignAgentAsync(AssignDeliveryAgentDto dto)
    {
        var order = await _orderRepo.GetByIdAsync(dto.OrderId)
            ?? throw new KeyNotFoundException("Order not found.");

        if (order.Status != OrderStatus.ReadyForPickup)
            throw new InvalidOperationException(
                $"Order must be ReadyForPickup to assign an agent. Current: {order.Status}.");

        var existing = await _deliveryRepo.GetByOrderIdAsync(dto.OrderId);
        if (existing is not null)
            throw new InvalidOperationException("An agent is already assigned to this order.");

        var assignment = new DeliveryAssignment
        {
            OrderId = dto.OrderId,
            AgentId = dto.AgentId,
            AgentName = dto.AgentName,
            AgentMobile = dto.AgentMobile,
            Status = DeliveryStatus.Assigned
        };
        assignment.StatusHistory.Add(new DeliveryStatusHistory
        {
            Status = DeliveryStatus.Assigned,
            Note = "Assigned by admin"
        });

        await _deliveryRepo.AddAsync(assignment);
        await _deliveryRepo.SaveChangesAsync();
        return MapToDto(assignment);
    }

    public async Task<List<DeliveryAssignmentDto>> GetMyDeliveriesAsync(Guid agentId)
    {
        var list = await _deliveryRepo.GetByAgentIdAsync(agentId);
        return list.Select(MapToDto).ToList();
    }

    public async Task<DeliveryAssignmentDto?> GetByIdAsync(Guid assignmentId)
    {
        var a = await _deliveryRepo.GetByIdAsync(assignmentId);
        return a is null ? null : MapToDto(a);
    }

    public async Task<DeliveryAssignmentDto?> GetByOrderIdAsync(Guid orderId)
    {
        var a = await _deliveryRepo.GetByOrderIdAsync(orderId);
        return a is null ? null : MapToDto(a);
    }

    public async Task<DeliveryAssignmentDto> UpdateStatusAsync(
        Guid assignmentId, UpdateDeliveryStatusDto dto, Guid agentId)
    {
        var assignment = await _deliveryRepo.GetByIdAsync(assignmentId)
            ?? throw new KeyNotFoundException("Delivery assignment not found.");

        if (assignment.AgentId != agentId)
            throw new UnauthorizedAccessException("You can only update your own deliveries.");

        if (!Enum.TryParse<DeliveryStatus>(dto.Status, ignoreCase: true, out var newStatus))
            throw new ArgumentException(
                $"Invalid status '{dto.Status}'. Valid: PickedUp, OutForDelivery, Delivered, Failed.");

        ValidateMilestoneTransition(assignment.Status, newStatus);

        var now = DateTime.UtcNow;
        assignment.Status = newStatus;

        switch (newStatus)
        {
            case DeliveryStatus.PickedUp: assignment.PickedUpAt = now; break;
            case DeliveryStatus.OutForDelivery: assignment.OutForDeliveryAt = now; break;
            case DeliveryStatus.Delivered:
                assignment.DeliveredAt = now;
                assignment.DeliveryNotes = dto.Note;
                break;
            case DeliveryStatus.Failed:
                assignment.FailureReason = dto.Note;
                break;
        }

        assignment.StatusHistory.Add(new DeliveryStatusHistory
        {
            DeliveryAssignmentId = assignment.Id,
            Status = newStatus,
            Note = dto.Note
        });

        // Mirror onto parent Order
        var order = await _orderRepo.GetByIdAsync(assignment.OrderId);
        if (order is not null)
        {
            order.Status = newStatus switch
            {
                DeliveryStatus.PickedUp => OrderStatus.PickedUp,
                DeliveryStatus.OutForDelivery => OrderStatus.OutForDelivery,
                DeliveryStatus.Delivered => OrderStatus.Delivered,
                DeliveryStatus.Failed => OrderStatus.Cancelled,
                _ => order.Status
            };
            order.UpdatedAt = now;
            await _orderRepo.UpdateAsync(order);
        }

        await _deliveryRepo.UpdateAsync(assignment);
        await _deliveryRepo.SaveChangesAsync();

        _publisher.Publish(new DeliveryStatusUpdatedEvent
        {
            DeliveryAssignmentId = assignment.Id,
            OrderId = assignment.OrderId,
            AgentId = agentId,
            NewStatus = newStatus.ToString(),
            Note = dto.Note
        }, QueueNames.DeliveryStatusUpdated);

        return MapToDto(assignment);
    }

    public async Task<List<DeliveryAssignmentDto>> GetPendingUnassignedAsync()
    {
        var list = await _deliveryRepo.GetPendingUnassignedAsync();
        return list.Select(MapToDto).ToList();
    }

    private static void ValidateMilestoneTransition(DeliveryStatus current, DeliveryStatus next)
    {
        var allowed = current switch
        {
            DeliveryStatus.Assigned => new[] { DeliveryStatus.PickedUp, DeliveryStatus.Failed },
            DeliveryStatus.PickedUp => new[] { DeliveryStatus.OutForDelivery, DeliveryStatus.Failed },
            DeliveryStatus.OutForDelivery => new[] { DeliveryStatus.Delivered, DeliveryStatus.Failed },
            _ => Array.Empty<DeliveryStatus>()
        };

        if (!allowed.Contains(next))
            throw new InvalidOperationException(
                $"Cannot transition from {current} to {next}. Allowed: {string.Join(", ", allowed)}.");
    }

    private static DeliveryAssignmentDto MapToDto(DeliveryAssignment a) => new()
    {
        Id = a.Id,
        OrderId = a.OrderId,
        AgentId = a.AgentId,
        AgentName = a.AgentName,
        AgentMobile = a.AgentMobile,
        Status = a.Status.ToString(),
        AssignedAt = a.AssignedAt,
        PickedUpAt = a.PickedUpAt,
        OutForDeliveryAt = a.OutForDeliveryAt,
        DeliveredAt = a.DeliveredAt,
        DeliveryNotes = a.DeliveryNotes,
        StatusHistory = a.StatusHistory
            .OrderBy(h => h.ChangedAt)
            .Select(h => new DeliveryHistoryDto
            {
                Status = h.Status.ToString(),
                Note = h.Note,
                ChangedAt = h.ChangedAt
            }).ToList()
    };
}
