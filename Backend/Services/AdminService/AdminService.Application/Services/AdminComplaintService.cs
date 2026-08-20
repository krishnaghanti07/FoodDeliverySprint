using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;

namespace AdminService.Application.Services;

public class AdminComplaintService : IAdminComplaintService
{
    private readonly IComplaintRepository _complaintRepo;
    private readonly IAdminAuditLogRepository _auditRepo;

    public AdminComplaintService(
        IComplaintRepository complaintRepo,
        IAdminAuditLogRepository auditRepo)
    {
        _complaintRepo = complaintRepo;
        _auditRepo = auditRepo;
    }

    public async Task<List<ComplaintListDto>> GetAllComplaintsAsync(string? status, string? type)
    {
        var complaints = await _complaintRepo.GetAllAsync(status, type);
        return complaints.Select(c => new ComplaintListDto
        {
            Id = c.Id,
            CustomerEmail = c.CustomerEmail,
            Type = c.Type,
            Subject = c.Subject,
            Status = c.Status,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<ComplaintDetailDto?> GetComplaintByIdAsync(Guid id)
    {
        var c = await _complaintRepo.GetByIdAsync(id);
        if (c is null) return null;

        return new ComplaintDetailDto
        {
            Id = c.Id,
            CustomerId = c.CustomerId,
            CustomerEmail = c.CustomerEmail,
            Type = c.Type,
            OrderId = c.OrderId,
            RestaurantId = c.RestaurantId,
            AgentId = c.AgentId,
            Subject = c.Subject,
            Description = c.Description,
            Status = c.Status,
            Resolution = c.Resolution,
            CreatedAt = c.CreatedAt,
            ResolvedAt = c.ResolvedAt
        };
    }

    public async Task<ComplaintDetailDto> ResolveComplaintAsync(Guid id, ResolveComplaintDto dto, Guid adminId)
    {
        var complaint = await _complaintRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Complaint {id} not found.");

        if (complaint.Status == "Resolved")
            throw new InvalidOperationException("Complaint is already resolved.");

        complaint.Status = "Resolved";
        complaint.Resolution = $"{dto.Action}\n\nNotes: {dto.Notes}";
        complaint.ResolvedBy = adminId;
        complaint.ResolvedAt = DateTime.UtcNow;

        await _complaintRepo.UpdateAsync(complaint);
        await _complaintRepo.SaveChangesAsync();

        await _auditRepo.AddAsync(new AdminAuditLog
        {
            AdminUserId = adminId,
            Action = "ResolveComplaint",
            EntityType = "Complaint",
            EntityId = id,
            OldValue = "Pending",
            NewValue = "Resolved",
            Reason = dto.Action
        });
        await _auditRepo.SaveChangesAsync();

        return new ComplaintDetailDto
        {
            Id = complaint.Id,
            CustomerId = complaint.CustomerId,
            CustomerEmail = complaint.CustomerEmail,
            Type = complaint.Type,
            OrderId = complaint.OrderId,
            RestaurantId = complaint.RestaurantId,
            AgentId = complaint.AgentId,
            Subject = complaint.Subject,
            Description = complaint.Description,
            Status = complaint.Status,
            Resolution = complaint.Resolution,
            CreatedAt = complaint.CreatedAt,
            ResolvedAt = complaint.ResolvedAt
        };
    }
}
