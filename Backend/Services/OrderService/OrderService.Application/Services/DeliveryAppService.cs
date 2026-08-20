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
            Status = DeliveryStatus.Assigned,
            EstimatedArrivalTime = DateTime.UtcNow.AddMinutes(20) // 20 min average delivery time
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
        // Load the delivery assignment with tracking
        var assignment = await _deliveryRepo.GetByIdAsync(assignmentId)
            ?? throw new KeyNotFoundException("Delivery assignment not found.");

        if (assignment.AgentId != agentId)
            throw new UnauthorizedAccessException("You can only update your own deliveries.");

        if (!Enum.TryParse<DeliveryStatus>(dto.Status, ignoreCase: true, out var newStatus))
            throw new ArgumentException(
                $"Invalid status '{dto.Status}'. Valid: PickedUp, OutForDelivery, Delivered, Failed.");

        ValidateMilestoneTransition(assignment.Status, newStatus);

        var now = DateTime.UtcNow;
        
        // Load the order first (needed for delivery fee credit)
        var order = await _orderRepo.GetByIdAsync(assignment.OrderId);
        
        // Update delivery assignment status
        assignment.Status = newStatus;

        switch (newStatus)
        {
            case DeliveryStatus.PickedUp: assignment.PickedUpAt = now; break;
            case DeliveryStatus.OutForDelivery: assignment.OutForDeliveryAt = now; break;
            case DeliveryStatus.Delivered:
                assignment.DeliveredAt = now;
                assignment.ActualArrivalTime = now;
                assignment.DeliveryNotes = dto.Note;
                
                // Credit delivery fee to agent's wallet
                if (order is not null)
                {
                    try
                    {
                        var httpClient = new HttpClient();
                        var deliveryFee = order.DeliveryFee; // ₹30
                        var walletRequest = new
                        {
                            userId = agentId,
                            amount = deliveryFee,
                            description = $"Delivery fee for order {order.Id.ToString().Substring(0, 8)}"
                        };

                        var content = new StringContent(
                            System.Text.Json.JsonSerializer.Serialize(walletRequest),
                            System.Text.Encoding.UTF8,
                            "application/json"
                        );

                        var response = await httpClient.PostAsync("http://localhost:5001/api/auth/wallet/add", content);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"✅ Delivery fee ₹{deliveryFee} credited to agent {agentId}");
                        }
                        else
                        {
                            Console.WriteLine($"❌ Failed to credit delivery fee to agent {agentId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error crediting delivery fee: {ex.Message}");
                    }
                }
                break;
            case DeliveryStatus.Failed:
                assignment.FailureReason = dto.Note;
                break;
        }

        // Update the parent order status
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
            
            // Track actual delivery time and check for delays
            if (newStatus == DeliveryStatus.Delivered)
            {
                order.ActualDeliveryTime = now;
                if (order.EstimatedDeliveryTime.HasValue && now > order.EstimatedDeliveryTime.Value)
                {
                    order.IsDelayed = true;
                }
            }
        }

        // Save changes BEFORE adding status history to avoid tracking issues
        await _deliveryRepo.SaveChangesAsync();

        // Now add the status history as a separate operation
        var historyEntry = new DeliveryStatusHistory
        {
            DeliveryAssignmentId = assignment.Id,
            Status = newStatus,
            Note = dto.Note,
            ChangedAt = now
        };
        
        // Add the history entry directly to the database
        await _deliveryRepo.AddStatusHistoryAsync(historyEntry);
        await _deliveryRepo.SaveChangesAsync();

        // Publish event
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

    public async Task<List<AvailableOrderDto>> GetAvailableOrdersAsync()
    {
        // Get all orders with status ReadyForPickup that don't have a delivery assignment yet
        var availableOrders = await _orderRepo.GetOrdersReadyForPickupAsync();
        
        return availableOrders.Select(o => new AvailableOrderDto
        {
            OrderId = o.Id,
            RestaurantId = o.RestaurantId,
            RestaurantName = o.RestaurantName,
            RestaurantAddress = "Restaurant Address", // TODO: Fetch from CatalogService
            DeliveryAddress = o.DeliveryAddress,
            TotalAmount = o.TotalAmount,
            CreatedAt = o.CreatedAt,
            EstimatedDeliveryTime = o.EstimatedDeliveryTime,
            ItemCount = o.Items?.Count ?? 0,
            PaymentMethod = o.PaymentMethod,
            DeliveryInstructions = o.DeliveryInstructions
        }).OrderBy(o => o.CreatedAt).ToList();
    }

    public async Task<DeliveryAssignmentDto> AcceptOrderAsync(Guid orderId, Guid agentId)
    {
        var order = await _orderRepo.GetByIdAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found.");

        if (order.Status != OrderStatus.ReadyForPickup)
            throw new InvalidOperationException(
                $"Order must be ReadyForPickup to accept. Current status: {order.Status}.");

        // Check if order already has a delivery assignment (race condition check)
        var existing = await _deliveryRepo.GetByOrderIdAsync(orderId);
        if (existing is not null)
            throw new InvalidOperationException("This order has already been accepted by another agent.");

        // Get agent details from AuthService (we'll need to add this)
        // For now, we'll use a simplified approach
        var agentName = "Agent"; // TODO: Fetch from AuthService
        var agentMobile = "0000000000"; // TODO: Fetch from AuthService

        var assignment = new DeliveryAssignment
        {
            OrderId = orderId,
            AgentId = agentId,
            AgentName = agentName,
            AgentMobile = agentMobile,
            Status = DeliveryStatus.Assigned,
            IsAutoAssigned = false, // Agent accepted (not admin assigned)
            AcceptedAt = DateTime.UtcNow,
            EstimatedArrivalTime = DateTime.UtcNow.AddMinutes(20) // 20 min average delivery time
        };
        
        assignment.StatusHistory.Add(new DeliveryStatusHistory
        {
            Status = DeliveryStatus.Assigned,
            Note = "Order accepted by delivery agent"
        });

        await _deliveryRepo.AddAsync(assignment);
        await _deliveryRepo.SaveChangesAsync();

        // Publish event for order acceptance
        _publisher.Publish(new DeliveryStatusUpdatedEvent
        {
            DeliveryAssignmentId = assignment.Id,
            OrderId = orderId,
            AgentId = agentId,
            NewStatus = "Assigned",
            Note = "Order accepted by delivery agent"
        }, QueueNames.DeliveryStatusUpdated);

        return MapToDto(assignment);
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
        EstimatedArrivalTime = a.EstimatedArrivalTime,
        ActualArrivalTime = a.ActualArrivalTime,
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
