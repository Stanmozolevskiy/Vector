using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Vector.Api.Data;
using Vector.Api.DTOs.Auth;
using Vector.Api.Helpers;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Services;

public class PasswordResetTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public PasswordResetTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _jwtServiceMock = new Mock<IJwtService>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IServiceProvider)))
            .Returns(serviceProviderMock.Object);

        _authService = new AuthService(
            _context,
            _emailServiceMock.Object,
            serviceProviderMock.Object,
            _loggerMock.Object,
            _jwtServiceMock.Object
        );
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithValidEmail_CreatesTokenAndSendsEmail()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = PasswordHasher.HashPassword("Password123!"),
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.ForgotPasswordAsync(user.Email);

        // Assert
        Assert.True(result);
        
        var passwordReset = await _context.PasswordResets
            .FirstOrDefaultAsync(pr => pr.UserId == user.Id);
        Assert.NotNull(passwordReset);
        Assert.False(passwordReset.IsUsed);
        Assert.True(passwordReset.ExpiresAt > DateTime.UtcNow);
        
        _emailServiceMock.Verify(
            x => x.SendPasswordResetEmailAsync(user.Email, It.IsAny<string>()), 
            Times.Once
        );
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithNonExistentEmail_ReturnsTrue()
    {
        // Arrange
        var email = "nonexistent@example.com";

        // Act
        var result = await _authService.ForgotPasswordAsync(email);

        // Assert - Should return true for security (prevent email enumeration)
        Assert.True(result);
        
        var passwordResets = await _context.PasswordResets.ToListAsync();
        Assert.Empty(passwordResets);
    }

    [Fact]
    public async Task ForgotPasswordAsync_InvalidatesOldTokens()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = PasswordHasher.HashPassword("Password123!"),
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);

        var oldToken = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "old-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        await _context.PasswordResets.AddAsync(oldToken);
        await _context.SaveChangesAsync();

        // Act
        await _authService.ForgotPasswordAsync(user.Email);

        // Assert
        var updatedOldToken = await _context.PasswordResets.FindAsync(oldToken.Id);
        Assert.True(updatedOldToken!.IsUsed);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ResetsPassword()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = PasswordHasher.HashPassword("OldPassword123!"),
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);

        var token = "valid-reset-token";
        var passwordReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.PasswordResets.AddAsync(passwordReset);
        await _context.SaveChangesAsync();

        var dto = new ResetPasswordDto
        {
            Token = token,
            Email = user.Email,
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _authService.ResetPasswordAsync(dto);

        // Assert
        Assert.True(result);
        
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.True(PasswordHasher.VerifyPassword("NewPassword123!", updatedUser!.PasswordHash));
        
        var updatedToken = await _context.PasswordResets.FindAsync(passwordReset.Id);
        Assert.True(updatedToken!.IsUsed);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = PasswordHasher.HashPassword("OldPassword123!"),
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);

        var token = "expired-token";
        var passwordReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            IsUsed = false,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };
        await _context.PasswordResets.AddAsync(passwordReset);
        await _context.SaveChangesAsync();

        var dto = new ResetPasswordDto
        {
            Token = token,
            Email = user.Email,
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _authService.ResetPasswordAsync(dto);

        // Assert
        Assert.False(result);
        
        var userAfterAttempt = await _context.Users.FindAsync(user.Id);
        Assert.True(PasswordHasher.VerifyPassword("OldPassword123!", userAfterAttempt!.PasswordHash));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithUsedToken_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = PasswordHasher.HashPassword("OldPassword123!"),
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);

        var token = "used-token";
        var passwordReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = true, // Already used
            CreatedAt = DateTime.UtcNow
        };
        await _context.PasswordResets.AddAsync(passwordReset);
        await _context.SaveChangesAsync();

        var dto = new ResetPasswordDto
        {
            Token = token,
            Email = user.Email,
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _authService.ResetPasswordAsync(dto);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithMismatchedEmail_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = PasswordHasher.HashPassword("OldPassword123!"),
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);

        var token = "valid-token";
        var passwordReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.PasswordResets.AddAsync(passwordReset);
        await _context.SaveChangesAsync();

        var dto = new ResetPasswordDto
        {
            Token = token,
            Email = "different@example.com", // Wrong email
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _authService.ResetPasswordAsync(dto);

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}

