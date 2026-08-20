using System;
using System.Collections.Generic;
using System.Text;
using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using FoodDelivery.Shared.Constants;
using FoodDelivery.Shared.Events;
using FoodDelivery.Shared.Messaging;

namespace AuthService.Application.Services;

public class AuthAppService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IRabbitMqPublisher _publisher;

    public AuthAppService(IUserRepository userRepo, IJwtService jwtService,
        IEmailService emailService, IRabbitMqPublisher publisher)
    {
        _userRepo = userRepo;
        _jwtService = jwtService;
        _emailService = emailService;
        _publisher = publisher;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (await _userRepo.EmailExistsAsync(dto.Email))
            throw new InvalidOperationException("An account with this email already exists.");

        // Allow Customer | Partner | DeliveryAgent — Admin created via seed only
        var allowedPublicRoles = new[] { Roles.Customer, Roles.Partner, Roles.DeliveryAgent };
        if (!allowedPublicRoles.Contains(dto.Role))
            throw new ArgumentException("Invalid role. Allowed: Customer, Partner, DeliveryAgent.");

        if (dto.Role == Roles.DeliveryAgent &&
            string.IsNullOrWhiteSpace(dto.VehicleType))
            throw new ArgumentException("VehicleType is required for Delivery Agent registration.");

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = dto.Email.ToLowerInvariant(),
            Mobile = dto.Mobile,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            ProfileImageUrl = dto.ProfileImageUrl,
            VehicleType = dto.VehicleType,
            VehicleNumber = dto.VehicleNumber,
            // Delivery agents require admin approval before they can accept deliveries
            IsApproved = dto.Role != Roles.DeliveryAgent
        };

        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();

        _publisher.Publish(new UserRegisteredEvent
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role
        }, QueueNames.UserRegistered);

        return await IssueTokens(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userRepo.GetByEmailAsync(dto.Email.ToLowerInvariant())
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        // Log user status for debugging
        Console.WriteLine($"[DEBUG] Login attempt for {user.Email} - IsActive: {user.IsActive}, IsDeleted: {user.IsDeleted}, IsApproved: {user.IsApproved}");

        if (user.IsDeleted)
            throw new UnauthorizedAccessException("Your account has been deleted. Please contact support if you believe this is an error.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Your account has been deactivated. Please contact support.");

        // Prevent unapproved delivery agents from logging in
        if (user.Role == Roles.DeliveryAgent && !user.IsApproved)
            throw new UnauthorizedAccessException("Your account is pending admin approval. Please wait for approval before logging in.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        // 2FA — send OTP if enabled
        if (user.TwoFactorEnabled)
        {
            var otp = GenerateOtp();
            user.OtpCode = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            await _userRepo.UpdateAsync(user);
            await _userRepo.SaveChangesAsync();

            await _emailService.SendOtpEmailAsync(user.Email, user.FullName, otp);

            return new AuthResponseDto { RequiresOtp = true, Role = user.Role, FullName = user.FullName };
        }

        return await IssueTokens(user);
    }

    public async Task<AuthResponseDto> VerifyOtpAsync(VerifyOtpDto dto)
    {
        var user = await _userRepo.GetByEmailAsync(dto.Email.ToLowerInvariant())
            ?? throw new UnauthorizedAccessException("User not found.");

        if (user.OtpCode != dto.OtpCode || user.OtpExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired OTP.");

        user.OtpCode = null;
        user.OtpExpiry = null;

        return await IssueTokens(user);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        // Find user by refresh token
        var user = await _userRepo.GetByRefreshTokenAsync(refreshToken);
        
        if (user == null)
            throw new UnauthorizedAccessException("Invalid refresh token.");
        
        // Check if refresh token is expired
        if (user.RefreshTokenExpiry == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired.");
        
        // Check if user is active
        if (!user.IsActive)
            throw new UnauthorizedAccessException("User account is inactive.");
        
        // Issue new tokens
        return await IssueTokens(user);
    }

    public async Task<string> SendOtpAsync(SendOtpDto dto)
    {
        var user = await _userRepo.GetByEmailAsync(dto.Email.ToLowerInvariant())
            ?? throw new InvalidOperationException("User not found.");

        var otp = GenerateOtp();
        user.OtpCode = otp;
        user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();

        await _emailService.SendOtpEmailAsync(user.Email, user.FullName, otp);

        return $"OTP sent to {user.Email} for {dto.Purpose}.";
    }

    public async Task<string> VerifyEmailAsync(VerifyEmailDto dto)
    {
        var user = await _userRepo.GetByEmailAsync(dto.Email.ToLowerInvariant())
            ?? throw new UnauthorizedAccessException("User not found.");

        if (user.OtpCode != dto.OtpCode || user.OtpExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired OTP.");

        user.IsEmailVerified = true;
        user.OtpCode = null;
        user.OtpExpiry = null;
        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();

        return "Email verified successfully.";
    }

    public async Task<string> Toggle2FAAsync(Guid userId, bool enable)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        user.TwoFactorEnabled = enable;
        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();

        return enable ? "Two-factor authentication enabled." : "Two-factor authentication disabled.";
    }

    public async Task<string> ResendOtpAsync(string email)
    {
        var user = await _userRepo.GetByEmailAsync(email.ToLowerInvariant())
            ?? throw new InvalidOperationException("User not found.");

        var otp = GenerateOtp();
        user.OtpCode = otp;
        user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();

        await _emailService.SendOtpEmailAsync(user.Email, user.FullName, otp);

        return $"OTP resent to {user.Email}.";
    }

    private async Task<AuthResponseDto> IssueTokens(User user)
    {
        var access = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role);
        var refresh = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refresh;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = access,
            RefreshToken = refresh,
            Role = user.Role,
            FullName = user.FullName
        };
    }

    public async Task<string> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userRepo.GetByEmailAsync(dto.Email.ToLowerInvariant())
            ?? throw new InvalidOperationException("If an account exists with this email, a password reset code has been sent.");

        var otp = GenerateOtp();
        user.PasswordResetToken = otp;
        user.PasswordResetExpiry = DateTime.UtcNow.AddMinutes(15);
        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();

        await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, otp);

        return "Password reset code sent to your email.";
    }

    public async Task<string> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _userRepo.GetByEmailAsync(dto.Email.ToLowerInvariant())
            ?? throw new UnauthorizedAccessException("Invalid reset code.");

        if (user.PasswordResetToken != dto.OtpCode || user.PasswordResetExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired reset code.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        
        // Invalidate all existing sessions
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        
        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();

        return "Password reset successfully. Please login with your new password.";
    }

    public async Task<string> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();

        return "Password changed successfully.";
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Mobile = user.Mobile,
            Role = user.Role,
            ProfileImageUrl = user.ProfileImageUrl,
            IsEmailVerified = user.IsEmailVerified,
            TwoFactorEnabled = user.TwoFactorEnabled,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            VehicleType = user.VehicleType,
            VehicleNumber = user.VehicleNumber,
            IsAvailableForDelivery = user.IsAvailableForDelivery,
            WalletBalance = user.WalletBalance
        };
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (!string.IsNullOrWhiteSpace(dto.FullName))
            user.FullName = dto.FullName.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Mobile))
            user.Mobile = dto.Mobile;

        if (dto.ProfileImageUrl != null)
            user.ProfileImageUrl = dto.ProfileImageUrl;

        // Delivery Agent specific updates
        if (user.Role == Roles.DeliveryAgent)
        {
            if (!string.IsNullOrWhiteSpace(dto.VehicleType))
                user.VehicleType = dto.VehicleType;

            if (!string.IsNullOrWhiteSpace(dto.VehicleNumber))
                user.VehicleNumber = dto.VehicleNumber;

            if (dto.IsAvailableForDelivery.HasValue)
                user.IsAvailableForDelivery = dto.IsAvailableForDelivery.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();

        return await GetProfileAsync(userId);
    }

    public async Task UpdateAgentApprovalAsync(Guid userId, bool isApproved, Guid approvedBy, string? notes)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (user.Role != Roles.DeliveryAgent)
            throw new InvalidOperationException("User is not a delivery agent.");

        user.IsApproved = isApproved;
        user.ApprovedBy = isApproved ? approvedBy : null;
        user.ApprovedAt = isApproved ? DateTime.UtcNow : null;
        user.ApprovalNotes = notes;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();
    }

    public async Task UpdateUserActiveStatusAsync(Guid userId, bool isActive, string reason)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (user.IsDeleted)
            throw new InvalidOperationException("Cannot activate/deactivate a deleted user. Please restore the user first.");

        Console.WriteLine($"[DEBUG] Updating user {user.Email} (ID: {userId}) - Current IsActive: {user.IsActive}, New IsActive: {isActive}");

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();

        Console.WriteLine($"[DEBUG] User {user.Email} status updated and saved. IsActive is now: {user.IsActive}");

        // Invalidate all sessions if deactivating
        if (!isActive)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _userRepo.UpdateAsync(user);
            await _userRepo.SaveChangesAsync();
            Console.WriteLine($"[DEBUG] User {user.Email} refresh tokens invalidated");
        }
    }

    public async Task SoftDeleteUserAsync(Guid userId, Guid deletedBy, string reason)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (user.IsDeleted)
            throw new InvalidOperationException("User is already deleted.");

        user.IsDeleted = true;
        user.DeletedBy = deletedBy;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletionReason = reason;
        user.IsActive = false; // Also deactivate
        user.RefreshToken = null; // Invalidate sessions
        user.RefreshTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();
    }

    public async Task RestoreUserAsync(Guid userId, Guid restoredBy, string reason)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (!user.IsDeleted)
            throw new InvalidOperationException("User is not deleted.");

        user.IsDeleted = false;
        user.DeletedBy = null;
        user.DeletedAt = null;
        user.DeletionReason = $"Restored by admin: {reason}";
        user.IsActive = true; // Reactivate user
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();
    }

    public async Task ToggleEmailVerificationAsync(Guid userId, bool isVerified, Guid adminId, string reason)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        user.IsEmailVerified = isVerified;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();

        Console.WriteLine($"[ADMIN] User {user.Email} email verification set to {isVerified} by admin {adminId}. Reason: {reason}");
    }

    public async Task<UserProfileDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return null;

        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Mobile = user.Mobile,
            Role = user.Role,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            ProfileImageUrl = user.ProfileImageUrl,
            WalletBalance = user.WalletBalance,
            TwoFactorEnabled = user.TwoFactorEnabled,
            VehicleType = user.VehicleType,
            VehicleNumber = user.VehicleNumber,
            IsAvailableForDelivery = user.IsAvailableForDelivery,
            IsDeleted = user.IsDeleted,
            DeletedAt = user.DeletedAt,
            DeletionReason = user.DeletionReason,
            IsApproved = user.IsApproved,
            ApprovedAt = user.ApprovedAt,
            ApprovalNotes = user.ApprovalNotes,
            CreatedAt = user.CreatedAt,
            RegisteredAt = user.CreatedAt
        };
    }

    public async Task<List<UserProfileDto>> GetAllUsersAsync(string? role, bool? isActive)
    {
        var users = await _userRepo.GetAllAsync();
        
        // Apply filters
        var filtered = users.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(role))
        {
            filtered = filtered.Where(u => u.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
        }
        
        if (isActive.HasValue)
        {
            filtered = filtered.Where(u => u.IsActive == isActive.Value);
        }
        
        return filtered.Select(user => new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Mobile = user.Mobile,
            Role = user.Role,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            ProfileImageUrl = user.ProfileImageUrl,
            WalletBalance = user.WalletBalance,
            TwoFactorEnabled = user.TwoFactorEnabled,
            VehicleType = user.VehicleType,
            VehicleNumber = user.VehicleNumber,
            IsAvailableForDelivery = user.IsAvailableForDelivery,
            IsDeleted = user.IsDeleted,
            DeletedAt = user.DeletedAt,
            DeletionReason = user.DeletionReason,
            IsApproved = user.IsApproved,
            ApprovedAt = user.ApprovedAt,
            ApprovalNotes = user.ApprovalNotes,
            CreatedAt = user.CreatedAt,
            RegisteredAt = user.CreatedAt
        }).ToList();
    }

    public async Task<decimal> GetWalletBalanceAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");
        return user.WalletBalance;
    }

    public async Task<bool> DeductFromWalletAsync(Guid userId, decimal amount, string description)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (user.WalletBalance < amount)
            throw new InvalidOperationException("Insufficient wallet balance.");

        user.WalletBalance -= amount;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();

        Console.WriteLine($"[WALLET] Deducted ₹{amount} from user {user.Email}. New balance: ₹{user.WalletBalance}. Reason: {description}");
        return true;
    }

    public async Task<bool> AddToWalletAsync(Guid userId, decimal amount, string description)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        user.WalletBalance += amount;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();

        Console.WriteLine($"[WALLET] Added ₹{amount} to user {user.Email}. New balance: ₹{user.WalletBalance}. Reason: {description}");
        return true;
    }

    private static string GenerateOtp() =>
        Random.Shared.Next(100000, 999999).ToString();
}
