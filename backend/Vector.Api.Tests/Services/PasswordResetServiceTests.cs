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

public class PasswordResetServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly AuthService _authService;

    public PasswordResetServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _jwtServiceMock = new Mock<IJwtService>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        
        var redisServiceMock = new Mock<IRedisService>();
        redisServiceMock.Setup(r => r.StoreRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        _authService = new AuthService(
            _context,
            _emailServiceMock.Object,
            _serviceProviderMock.Object,
            _loggerMock.Object,
            _jwtServiceMock.Object,
            redisServiceMock.Object
        );
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithValidEmail_SendsEmail()
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
        var passwordReset = await _context.PasswordResets.FirstOrDefaultAsync(pr => pr.UserId == user.Id);
        Assert.NotNull(passwordReset);
        Assert.False(passwordReset.IsUsed);
        Assert.True(passwordReset.ExpiresAt > DateTime.UtcNow);
        _emailServiceMock.Verify(x => x.SendPasswordResetEmailAsync(user.Email, passwordReset.Token), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithNonExistentEmail_ReturnsTrue()
    {
        // Act
        var result = await _authService.ForgotPasswordAsync("nonexistent@example.com");

        // Assert (returns true for security - don't reveal if email exists)
        Assert.True(result);
        var passwordResets = await _context.PasswordResets.ToListAsync();
        Assert.Empty(passwordResets);
        _emailServiceMock.Verify(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_InvalidatesExistingTokens()
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

        var existingReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "old-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.PasswordResets.AddAsync(existingReset);
        await _context.SaveChangesAsync();

        // Act
        await _authService.ForgotPasswordAsync(user.Email);

        // Assert
        var oldReset = await _context.PasswordResets.FindAsync(existingReset.Id);
        Assert.True(oldReset!.IsUsed);
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

        var resetToken = "valid-reset-token";
        var passwordReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = resetToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.PasswordResets.AddAsync(passwordReset);
        await _context.SaveChangesAsync();

        var dto = new ResetPasswordDto
        {
            Token = resetToken,
            Email = user.Email,
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _authService.ResetPasswordAsync(dto);

        // Assert
        Assert.True(result);
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.True(PasswordHasher.VerifyPassword("NewPassword123!", updatedUser!.PasswordHash));
        
        var usedReset = await _context.PasswordResets.FindAsync(passwordReset.Id);
        Assert.True(usedReset!.IsUsed);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_ReturnsFalse()
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

        var resetToken = "expired-token";
        var passwordReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = resetToken,
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            IsUsed = false,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };
        await _context.PasswordResets.AddAsync(passwordReset);
        await _context.SaveChangesAsync();

        var dto = new ResetPasswordDto
        {
            Token = resetToken,
            Email = user.Email,
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _authService.ResetPasswordAsync(dto);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithUsedToken_ReturnsFalse()
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

        var resetToken = "used-token";
        var passwordReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = resetToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = true, // Already used
            CreatedAt = DateTime.UtcNow
        };
        await _context.PasswordResets.AddAsync(passwordReset);
        await _context.SaveChangesAsync();

        var dto = new ResetPasswordDto
        {
            Token = resetToken,
            Email = user.Email,
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _authService.ResetPasswordAsync(dto);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithEmailMismatch_ReturnsFalse()
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

        var resetToken = "valid-token";
        var passwordReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = resetToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.PasswordResets.AddAsync(passwordReset);
        await _context.SaveChangesAsync();

        var dto = new ResetPasswordDto
        {
            Token = resetToken,
            Email = "wrong@example.com", // Wrong email
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

