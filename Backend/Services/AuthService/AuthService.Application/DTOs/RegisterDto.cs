using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public class RegisterDto
{
    [Required] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(10), MaxLength(10)] public string Mobile { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;

    // Allowed: Customer | Partner | DeliveryAgent  (Admin only via seeding)
    public string Role { get; set; } = "Customer";

    // Profile Image (Base64 or URL)
    public string? ProfileImageUrl { get; set; }

    // ── Delivery Agent only ─────────────────────────
    public string? VehicleType { get; set; }
    public string? VehicleNumber { get; set; }
}

public class LoginDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class VerifyOtpDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string OtpCode { get; set; } = string.Empty;
}

public class RefreshTokenDto
{
    [Required] public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    [System.Text.Json.Serialization.JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("requiresOtp")]
    public bool RequiresOtp { get; set; } = false;
}

public class SendOtpDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    public string Purpose { get; set; } = "EmailVerification"; // EmailVerification | PasswordReset | Enable2FA
}

public class Toggle2FADto
{
    [Required] public bool Enable { get; set; }
}

public class VerifyEmailDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string OtpCode { get; set; } = string.Empty;
}

public class ForgotPasswordDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string OtpCode { get; set; } = string.Empty;
    [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty;
}

public class UserProfileDto
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("mobile")]
    public string Mobile { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("profileImageUrl")]
    public string? ProfileImageUrl { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("isEmailVerified")]
    public bool IsEmailVerified { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("twoFactorEnabled")]
    public bool TwoFactorEnabled { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("registeredAt")]
    public DateTime RegisteredAt { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
    
    // Delivery Agent fields
    [System.Text.Json.Serialization.JsonPropertyName("vehicleType")]
    public string? VehicleType { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("vehicleNumber")]
    public string? VehicleNumber { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("isAvailableForDelivery")]
    public bool IsAvailableForDelivery { get; set; }
    
    // Wallet Balance
    [System.Text.Json.Serialization.JsonPropertyName("walletBalance")]
    public decimal WalletBalance { get; set; }
    
    // Soft Delete fields
    [System.Text.Json.Serialization.JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("deletedAt")]
    public DateTime? DeletedAt { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("deletionReason")]
    public string? DeletionReason { get; set; }
    
    // Approval fields
    [System.Text.Json.Serialization.JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("isApproved")]
    public bool IsApproved { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("approvedAt")]
    public DateTime? ApprovedAt { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("approvalNotes")]
    public string? ApprovalNotes { get; set; }
}

public class UpdateProfileDto
{
    [System.Text.Json.Serialization.JsonPropertyName("fullName")]
    public string? FullName { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("mobile")]
    public string? Mobile { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("profileImageUrl")]
    public string? ProfileImageUrl { get; set; }
    
    // Delivery Agent fields
    [System.Text.Json.Serialization.JsonPropertyName("vehicleType")]
    public string? VehicleType { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("vehicleNumber")]
    public string? VehicleNumber { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("isAvailableForDelivery")]
    public bool? IsAvailableForDelivery { get; set; }
}