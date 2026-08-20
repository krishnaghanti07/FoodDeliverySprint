using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;

namespace AdminService.Application.Services;

public class AdminDeliveryAgentService : IAdminDeliveryAgentService
{
    private readonly IDeliveryAgentSnapshotRepository _agentRepo;
    private readonly IAdminAuditLogRepository _auditRepo;

    public AdminDeliveryAgentService(
        IDeliveryAgentSnapshotRepository agentRepo,
        IAdminAuditLogRepository auditRepo)
    {
        _agentRepo = agentRepo;
        _auditRepo = auditRepo;
    }

    public async Task<List<DeliveryAgentListDto>> GetAllAgentsAsync(bool? isActive, bool? isOnline, int? page, int? pageSize)
    {
        var agents = await _agentRepo.GetAllAsync(isActive, isOnline, page, pageSize);
        return agents.Select(a => new DeliveryAgentListDto
        {
            Id = a.Id,
            FullName = a.FullName,
            Mobile = a.Mobile,
            IsActive = a.IsActive,
            IsOnline = a.IsOnline,
            IsAvailable = a.IsAvailable,
            VehicleType = a.VehicleType,
            TotalDeliveries = a.TotalDeliveries,
            AverageRating = a.AverageRating,
            RegisteredAt = a.RegisteredAt
        }).ToList();
    }

    public async Task<DeliveryAgentDetailDto?> GetAgentByIdAsync(Guid id)
    {
        var a = await _agentRepo.GetByIdAsync(id);
        if (a is null) return null;

        return new DeliveryAgentDetailDto
        {
            Id = a.Id,
            FullName = a.FullName,
            Email = a.Email,
            Mobile = a.Mobile,
            IsActive = a.IsActive,
            IsOnline = a.IsOnline,
            IsAvailable = a.IsAvailable,
            VehicleType = a.VehicleType,
            TotalDeliveries = a.TotalDeliveries,
            AverageRating = a.AverageRating,
            RegisteredAt = a.RegisteredAt,
            UpdatedAt = a.UpdatedAt
        };
    }

    public async Task UpdateAgentStatusAsync(Guid id, UpdateAgentStatusDto dto, Guid adminId)
    {
        var agent = await _agentRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Delivery agent {id} not found.");

        // Call AuthService to update the actual user status
        try
        {
            using var httpClient = new HttpClient();
            var authServiceUrl = "http://localhost:5001/api/auth/admin/toggle-user-status";
            var payload = new { userId = id, isActive = dto.IsActive, reason = dto.Reason };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            var response = await httpClient.PostAsync(authServiceUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to update user status in AuthService: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to update agent status: {ex.Message}");
        }

        // Update snapshot
        var oldStatus = agent.IsActive;
        await _agentRepo.SetActiveAsync(id, dto.IsActive);
        await _agentRepo.SaveChangesAsync();

        await _auditRepo.AddAsync(new AdminAuditLog
        {
            AdminUserId = adminId,
            Action = dto.IsActive ? "ActivateAgent" : "DeactivateAgent",
            EntityType = "DeliveryAgent",
            EntityId = id,
            OldValue = oldStatus.ToString(),
            NewValue = dto.IsActive.ToString(),
            Reason = dto.Reason
        });
        await _auditRepo.SaveChangesAsync();
    }

    public async Task ApproveAgentAsync(Guid id, ApproveAgentDto dto, Guid adminId)
    {
        var agent = await _agentRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Delivery agent {id} not found.");

        if (agent.IsApproved)
            throw new InvalidOperationException("Agent is already approved.");

        // Update snapshot
        agent.IsApproved = true;
        agent.ApprovedBy = adminId;
        agent.ApprovedAt = DateTime.UtcNow;
        agent.ApprovalNotes = dto.Notes;
        agent.UpdatedAt = DateTime.UtcNow;

        await _agentRepo.UpdateAsync(agent);
        await _agentRepo.SaveChangesAsync();

        // TODO: Call AuthService API to update User.IsApproved = true
        // For now, we'll use HTTP call to AuthService
        try
        {
            using var httpClient = new HttpClient();
            var authServiceUrl = "http://localhost:5001/api/auth/admin/approve-agent";
            var payload = new { userId = id, isApproved = true, approvedBy = adminId, notes = dto.Notes };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            await httpClient.PostAsync(authServiceUrl, content);
        }
        catch (Exception ex)
        {
            // Log but don't fail - snapshot is updated
            Console.WriteLine($"Failed to update AuthService: {ex.Message}");
        }

        await _auditRepo.AddAsync(new AdminAuditLog
        {
            AdminUserId = adminId,
            Action = "ApproveAgent",
            EntityType = "DeliveryAgent",
            EntityId = id,
            OldValue = "Pending",
            NewValue = "Approved",
            Reason = dto.Notes
        });
        await _auditRepo.SaveChangesAsync();
    }

    public async Task RejectAgentAsync(Guid id, RejectAgentDto dto, Guid adminId)
    {
        var agent = await _agentRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Delivery agent {id} not found.");

        if (agent.IsApproved)
            throw new InvalidOperationException("Cannot reject an already approved agent.");

        // Deactivate the agent in snapshot
        agent.IsActive = false;
        agent.IsApproved = false;
        agent.ApprovalNotes = dto.Reason;
        agent.UpdatedAt = DateTime.UtcNow;

        await _agentRepo.UpdateAsync(agent);
        await _agentRepo.SaveChangesAsync();

        // TODO: Call AuthService API to update User.IsApproved = false
        try
        {
            using var httpClient = new HttpClient();
            var authServiceUrl = "http://localhost:5001/api/auth/admin/approve-agent";
            var payload = new { userId = id, isApproved = false, approvedBy = adminId, notes = dto.Reason };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            await httpClient.PostAsync(authServiceUrl, content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to update AuthService: {ex.Message}");
        }

        await _auditRepo.AddAsync(new AdminAuditLog
        {
            AdminUserId = adminId,
            Action = "RejectAgent",
            EntityType = "DeliveryAgent",
            EntityId = id,
            OldValue = "Pending",
            NewValue = "Rejected",
            Reason = dto.Reason
        });
        await _auditRepo.SaveChangesAsync();
    }

    public async Task<List<DeliveryAgentListDto>> GetPendingAgentsAsync()
    {
        var agents = await _agentRepo.GetPendingApprovalAsync();
        return agents.Select(a => new DeliveryAgentListDto
        {
            Id = a.Id,
            FullName = a.FullName,
            Mobile = a.Mobile,
            IsActive = a.IsActive,
            IsOnline = a.IsOnline,
            IsAvailable = a.IsAvailable,
            VehicleType = a.VehicleType,
            TotalDeliveries = a.TotalDeliveries,
            AverageRating = a.AverageRating,
            RegisteredAt = a.RegisteredAt
        }).ToList();
    }
}
