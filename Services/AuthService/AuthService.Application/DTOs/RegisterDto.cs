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
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
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