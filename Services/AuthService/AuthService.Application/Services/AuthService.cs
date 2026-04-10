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
            VehicleType = dto.VehicleType,
            VehicleNumber = dto.VehicleNumber
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

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is inactive. Contact support.");

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
        // In a real scenario query by refresh token — simplified here
        throw new NotImplementedException("Implement token refresh lookup.");
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

    private static string GenerateOtp() =>
        Random.Shared.Next(100000, 999999).ToString();
}
