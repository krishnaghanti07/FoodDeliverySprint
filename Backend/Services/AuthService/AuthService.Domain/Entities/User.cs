namespace AuthService.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Customer;
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = false;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Profile Image
    public string? ProfileImageUrl { get; set; }

    // Wallet Balance (for refunds and credits)
    public decimal WalletBalance { get; set; } = 0;

    // 2FA OTP
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiry { get; set; }
    public bool TwoFactorEnabled { get; set; } = false;

    // Password Reset
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiry { get; set; }

    // ── Delivery Agent profile fields ────────────────
    public string? VehicleType { get; set; }       // Bike | Scooter | Car
    public string? VehicleNumber { get; set; }
    public bool IsAvailableForDelivery { get; set; } = false;

    // ── Soft Delete fields ────────────────────────────
    public bool IsDeleted { get; set; } = false;
    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletionReason { get; set; }

    // ── Delivery Agent Approval fields ────────────────
    public bool IsApproved { get; set; } = true;  // Default true for non-agents
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }
    public string? RejectionReason { get; set; }
}

public static class Roles
{
    public const string Customer = "Customer";
    public const string Partner = "Partner";
    public const string Admin = "Admin";
    public const string DeliveryAgent = "DeliveryAgent";   // ← NEW

    public static readonly string[] AllRoles =
        { Customer, Partner, Admin, DeliveryAgent };
}