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

    // 2FA OTP
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiry { get; set; }
    public bool TwoFactorEnabled { get; set; } = false;

    // ── Delivery Agent profile fields ────────────────
    public string? VehicleType { get; set; }       // Bike | Scooter | Car
    public string? VehicleNumber { get; set; }
    public bool IsAvailableForDelivery { get; set; } = false;
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