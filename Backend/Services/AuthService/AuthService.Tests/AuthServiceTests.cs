using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using FoodDelivery.Shared.Messaging;
using Moq;

namespace AuthService.Tests;

// ══════════════════════════════════════════════════════════════════════
// AUTH SERVICE — UNIT TESTS
// Covers: Registration, Login, OTP, Password, Profile, Wallet, Soft-Delete
// ══════════════════════════════════════════════════════════════════════
[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserRepository> _userRepo = null!;
    private Mock<IJwtService>     _jwtSvc   = null!;
    private Mock<IEmailService>   _emailSvc = null!;
    private Mock<IRabbitMqPublisher> _publisher = null!;
    private AuthAppService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo  = new Mock<IUserRepository>();
        _jwtSvc    = new Mock<IJwtService>();
        _emailSvc  = new Mock<IEmailService>();
        _publisher = new Mock<IRabbitMqPublisher>();

        _jwtSvc.Setup(j => j.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
               .Returns("mock-access-token");
        _jwtSvc.Setup(j => j.GenerateRefreshToken())
               .Returns("mock-refresh-token");

        _sut = new AuthAppService(_userRepo.Object, _jwtSvc.Object, _emailSvc.Object, _publisher.Object);
    }

    // ── Registration ──────────────────────────────────────────────────

    [Test]
    public async Task Register_ValidCustomer_ReturnsTokens()
    {
        _userRepo.Setup(r => r.EmailExistsAsync("john@example.com")).ReturnsAsync(false);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var dto = new RegisterDto
        {
            FullName = "John Doe",
            Email    = "john@example.com",
            Mobile   = "9876543210",
            Password = "Password@123",
            Role     = "Customer"
        };

        var result = await _sut.RegisterAsync(dto);

        Assert.That(result.AccessToken,  Is.EqualTo("mock-access-token"));
        Assert.That(result.RefreshToken, Is.EqualTo("mock-refresh-token"));
        Assert.That(result.Role,         Is.EqualTo("Customer"));
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Test]
    public void Register_DuplicateEmail_ThrowsInvalidOperationException()
    {
        _userRepo.Setup(r => r.EmailExistsAsync("dup@example.com")).ReturnsAsync(true);

        var dto = new RegisterDto
        {
            FullName = "Dup User", Email = "dup@example.com",
            Mobile = "9000000000", Password = "Pass@123", Role = "Customer"
        };

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RegisterAsync(dto));
    }

    [Test]
    public void Register_AdminRole_ThrowsArgumentException()
    {
        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

        var dto = new RegisterDto
        {
            FullName = "Hacker", Email = "hack@example.com",
            Mobile = "9000000001", Password = "Pass@123", Role = "Admin"
        };

        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(dto));
    }

    [Test]
    public void Register_DeliveryAgent_WithoutVehicleType_ThrowsArgumentException()
    {
        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

        var dto = new RegisterDto
        {
            FullName = "Agent", Email = "agent@example.com",
            Mobile = "9000000002", Password = "Pass@123", Role = "DeliveryAgent"
            // VehicleType intentionally omitted
        };

        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(dto));
    }

    [Test]
    public async Task Register_DeliveryAgent_IsApproved_False()
    {
        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        User? capturedUser = null;
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
                 .Callback<User>(u => capturedUser = u)
                 .Returns(Task.CompletedTask);

        var dto = new RegisterDto
        {
            FullName = "Agent", Email = "agent2@example.com",
            Mobile = "9000000003", Password = "Pass@123",
            Role = "DeliveryAgent", VehicleType = "Bike"
        };

        await _sut.RegisterAsync(dto);

        Assert.That(capturedUser, Is.Not.Null);
        Assert.That(capturedUser!.IsApproved, Is.False,
            "Delivery agents must start unapproved until admin approves them.");
    }

    // ── Login ─────────────────────────────────────────────────────────

    [Test]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@123"),
            Role = "Customer", IsActive = true, IsDeleted = false,
            IsApproved = true, FullName = "Test User"
        };
        _userRepo.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.LoginAsync(new LoginDto { Email = "user@example.com", Password = "Pass@123" });

        Assert.That(result.AccessToken, Is.EqualTo("mock-access-token"));
    }

    [Test]
    public void Login_WrongPassword_ThrowsUnauthorized()
    {
        var user = new User
        {
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass"),
            IsActive = true, IsDeleted = false, IsApproved = true
        };
        _userRepo.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.LoginAsync(new LoginDto { Email = "user@example.com", Password = "WrongPass" }));
    }

    [Test]
    public void Login_DeletedUser_ThrowsUnauthorized()
    {
        var user = new User
        {
            Email = "deleted@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@123"),
            IsDeleted = true, IsActive = false
        };
        _userRepo.Setup(r => r.GetByEmailAsync("deleted@example.com")).ReturnsAsync(user);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.LoginAsync(new LoginDto { Email = "deleted@example.com", Password = "Pass@123" }));
    }

    [Test]
    public void Login_InactiveUser_ThrowsUnauthorized()
    {
        var user = new User
        {
            Email = "inactive@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@123"),
            IsActive = false, IsDeleted = false
        };
        _userRepo.Setup(r => r.GetByEmailAsync("inactive@example.com")).ReturnsAsync(user);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.LoginAsync(new LoginDto { Email = "inactive@example.com", Password = "Pass@123" }));
    }

    [Test]
    public void Login_UnapprovedDeliveryAgent_ThrowsUnauthorized()
    {
        var user = new User
        {
            Email = "agent@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@123"),
            Role = "DeliveryAgent", IsActive = true, IsDeleted = false, IsApproved = false
        };
        _userRepo.Setup(r => r.GetByEmailAsync("agent@example.com")).ReturnsAsync(user);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.LoginAsync(new LoginDto { Email = "agent@example.com", Password = "Pass@123" }));
    }

    [Test]
    public void Login_NonExistentUser_ThrowsUnauthorized()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.LoginAsync(new LoginDto { Email = "ghost@example.com", Password = "Pass@123" }));
    }

    // ── OTP Verification ──────────────────────────────────────────────

    [Test]
    public async Task VerifyOtp_ValidCode_ReturnsTokens()
    {
        var user = new User
        {
            Email = "otp@example.com", OtpCode = "123456",
            OtpExpiry = DateTime.UtcNow.AddMinutes(5),
            IsActive = true, IsDeleted = false, FullName = "OTP User"
        };
        _userRepo.Setup(r => r.GetByEmailAsync("otp@example.com")).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.VerifyOtpAsync(new VerifyOtpDto { Email = "otp@example.com", OtpCode = "123456" });

        Assert.That(result.AccessToken, Is.EqualTo("mock-access-token"));
    }

    [Test]
    public void VerifyOtp_ExpiredCode_ThrowsUnauthorized()
    {
        var user = new User
        {
            Email = "otp@example.com", OtpCode = "123456",
            OtpExpiry = DateTime.UtcNow.AddMinutes(-1) // expired
        };
        _userRepo.Setup(r => r.GetByEmailAsync("otp@example.com")).ReturnsAsync(user);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.VerifyOtpAsync(new VerifyOtpDto { Email = "otp@example.com", OtpCode = "123456" }));
    }

    [Test]
    public void VerifyOtp_WrongCode_ThrowsUnauthorized()
    {
        var user = new User
        {
            Email = "otp@example.com", OtpCode = "999999",
            OtpExpiry = DateTime.UtcNow.AddMinutes(5)
        };
        _userRepo.Setup(r => r.GetByEmailAsync("otp@example.com")).ReturnsAsync(user);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.VerifyOtpAsync(new VerifyOtpDto { Email = "otp@example.com", OtpCode = "000000" }));
    }

    // ── Refresh Token ─────────────────────────────────────────────────

    [Test]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        var user = new User
        {
            Email = "user@example.com", RefreshToken = "valid-refresh",
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(1),
            IsActive = true, FullName = "User"
        };
        _userRepo.Setup(r => r.GetByRefreshTokenAsync("valid-refresh")).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.RefreshTokenAsync("valid-refresh");

        Assert.That(result.AccessToken, Is.EqualTo("mock-access-token"));
    }

    [Test]
    public void RefreshToken_ExpiredToken_ThrowsUnauthorized()
    {
        var user = new User
        {
            RefreshToken = "expired-token",
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1), // expired
            IsActive = true
        };
        _userRepo.Setup(r => r.GetByRefreshTokenAsync("expired-token")).ReturnsAsync(user);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.RefreshTokenAsync("expired-token"));
    }

    [Test]
    public void RefreshToken_InvalidToken_ThrowsUnauthorized()
    {
        _userRepo.Setup(r => r.GetByRefreshTokenAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.RefreshTokenAsync("bad-token"));
    }

    // ── Password Management ───────────────────────────────────────────

    [Test]
    public async Task ResetPassword_ValidOtp_UpdatesHash()
    {
        var user = new User
        {
            Email = "reset@example.com",
            PasswordResetToken = "654321",
            PasswordResetExpiry = DateTime.UtcNow.AddMinutes(10),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass")
        };
        _userRepo.Setup(r => r.GetByEmailAsync("reset@example.com")).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.ResetPasswordAsync(new ResetPasswordDto
        {
            Email = "reset@example.com", OtpCode = "654321", NewPassword = "NewPass@123"
        });

        Assert.That(result, Does.Contain("successfully"));
        Assert.That(BCrypt.Net.BCrypt.Verify("NewPass@123", user.PasswordHash), Is.True);
    }

    [Test]
    public async Task ChangePassword_CorrectCurrentPassword_Succeeds()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CurrentPass@1")
        };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.ChangePasswordAsync(userId, new ChangePasswordDto
        {
            CurrentPassword = "CurrentPass@1", NewPassword = "NewPass@123"
        });

        Assert.That(result, Does.Contain("successfully"));
    }

    [Test]
    public void ChangePassword_WrongCurrentPassword_ThrowsUnauthorized()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, PasswordHash = BCrypt.Net.BCrypt.HashPassword("RealPass") };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ChangePasswordAsync(userId, new ChangePasswordDto
            {
                CurrentPassword = "WrongPass", NewPassword = "NewPass@123"
            }));
    }

    // ── Profile ───────────────────────────────────────────────────────

    [Test]
    public async Task GetProfile_ExistingUser_ReturnsDto()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId, FullName = "Jane Doe", Email = "jane@example.com",
            Role = "Customer", WalletBalance = 250m
        };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = await _sut.GetProfileAsync(userId);

        Assert.That(result.FullName, Is.EqualTo("Jane Doe"));
        Assert.That(result.WalletBalance, Is.EqualTo(250m));
    }

    [Test]
    public void GetProfile_NonExistentUser_ThrowsInvalidOperation()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetProfileAsync(Guid.NewGuid()));
    }

    // ── Wallet ────────────────────────────────────────────────────────

    [Test]
    public async Task DeductFromWallet_SufficientBalance_DeductsCorrectly()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, WalletBalance = 500m };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.DeductFromWalletAsync(userId, 200m, "Order payment");

        Assert.That(result, Is.True);
        Assert.That(user.WalletBalance, Is.EqualTo(300m));
    }

    [Test]
    public void DeductFromWallet_InsufficientBalance_ThrowsInvalidOperation()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, WalletBalance = 100m };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.DeductFromWalletAsync(userId, 500m, "Order payment"));
    }

    [Test]
    public async Task AddToWallet_ValidAmount_IncreasesBalance()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, WalletBalance = 100m };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.AddToWalletAsync(userId, 300m, "Refund credit");

        Assert.That(result, Is.True);
        Assert.That(user.WalletBalance, Is.EqualTo(400m));
    }

    // ── Soft Delete / Restore ─────────────────────────────────────────

    [Test]
    public async Task SoftDeleteUser_ActiveUser_SetsDeletedFlags()
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = new User { Id = userId, IsDeleted = false, IsActive = true };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.SoftDeleteUserAsync(userId, adminId, "Policy violation");

        Assert.That(user.IsDeleted, Is.True);
        Assert.That(user.IsActive, Is.False);
        Assert.That(user.RefreshToken, Is.Null);
    }

    [Test]
    public void SoftDeleteUser_AlreadyDeleted_ThrowsInvalidOperation()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, IsDeleted = true };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.SoftDeleteUserAsync(userId, Guid.NewGuid(), "Reason"));
    }

    [Test]
    public async Task RestoreUser_DeletedUser_ClearsDeletedFlags()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, IsDeleted = true, IsActive = false };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.RestoreUserAsync(userId, Guid.NewGuid(), "Reinstated");

        Assert.That(user.IsDeleted, Is.False);
        Assert.That(user.IsActive, Is.True);
    }
}
