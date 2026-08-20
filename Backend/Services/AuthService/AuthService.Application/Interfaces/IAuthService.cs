using System;
using System.Collections.Generic;
using System.Text;
using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> VerifyOtpAsync(VerifyOtpDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    
    // OTP Management
    Task<string> SendOtpAsync(SendOtpDto dto);
    Task<string> VerifyEmailAsync(VerifyEmailDto dto);
    Task<string> Toggle2FAAsync(Guid userId, bool enable);
    Task<string> ResendOtpAsync(string email);
    
    // Password Management
    Task<string> ForgotPasswordAsync(ForgotPasswordDto dto);
    Task<string> ResetPasswordAsync(ResetPasswordDto dto);
    Task<string> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    
    // Profile Management
    Task<UserProfileDto> GetProfileAsync(Guid userId);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
    
    // Admin Management
    Task UpdateAgentApprovalAsync(Guid userId, bool isApproved, Guid approvedBy, string? notes);
    Task UpdateUserActiveStatusAsync(Guid userId, bool isActive, string reason);
    Task ToggleEmailVerificationAsync(Guid userId, bool isVerified, Guid adminId, string reason);
    Task SoftDeleteUserAsync(Guid userId, Guid deletedBy, string reason);
    Task RestoreUserAsync(Guid userId, Guid restoredBy, string reason);
    Task<UserProfileDto?> GetUserByIdAsync(Guid userId);
    Task<List<UserProfileDto>> GetAllUsersAsync(string? role, bool? isActive);
    
    // Wallet Management
    Task<decimal> GetWalletBalanceAsync(Guid userId);
    Task<bool> DeductFromWalletAsync(Guid userId, decimal amount, string description);
    Task<bool> AddToWalletAsync(Guid userId, decimal amount, string description);
}

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode);
    Task SendPasswordResetEmailAsync(string toEmail, string fullName, string otpCode);
}

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email, string role);
    string GenerateRefreshToken();
}
