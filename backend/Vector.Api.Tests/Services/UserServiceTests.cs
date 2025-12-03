using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Vector.Api.Data;
using Vector.Api.DTOs.User;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserService _userService;
    private readonly Mock<ILogger<UserService>> _mockLogger;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<UserService>>();
        _userService = new UserService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _userService.GetUserByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithValidEmail_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByEmailAsync("test@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Email.ToLower(), result.Email.ToLower());
    }

    [Fact]
    public async Task GetUserByEmailAsync_CaseInsensitive_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "Test@Example.com",
            PasswordHash = "hash",
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByEmailAsync("test@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithInvalidEmail_ReturnsNull()
    {
        // Act
        var result = await _userService.GetUserByEmailAsync("nonexistent@example.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithValidData_UpdatesUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            FirstName = "John",
            LastName = "Doe",
            Bio = "Old bio",
            PhoneNumber = null,
            Location = null,
            Role = "student",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateProfileDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            Bio = "New bio",
            PhoneNumber = "+1 (555) 123-4567",
            Location = "San Francisco, CA"
        };

        // Act
        var result = await _userService.UpdateProfileAsync(userId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Jane", result.FirstName);
        Assert.Equal("Smith", result.LastName);
        Assert.Equal("New bio", result.Bio);
        Assert.Equal("+1 (555) 123-4567", result.PhoneNumber);
        Assert.Equal("San Francisco, CA", result.Location);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithPhoneAndLocation_SavesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            FirstName = "John",
            LastName = "Doe",
            Role = "student",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+1-234-567-8900",
            Location = "New York, NY"
        };

        // Act
        var result = await _userService.UpdateProfileAsync(userId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("+1-234-567-8900", result.PhoneNumber);
        Assert.Equal("New York, NY", result.Location);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithEmptyPhoneAndLocation_ClearsFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+1-234-567-8900",
            Location = "New York, NY",
            Role = "student",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "",
            Location = ""
        };

        // Act
        var result = await _userService.UpdateProfileAsync(userId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.PhoneNumber);
        Assert.Null(result.Location);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithInvalidUserId_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateProfileDto
        {
            FirstName = "Jane",
            LastName = "Smith"
        };

        // Act
        var result = await _userService.UpdateProfileAsync(Guid.NewGuid(), updateDto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_UpdatesPassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentPasswordHash = BCrypt.Net.BCrypt.HashPassword("CurrentPass123!");
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = currentPasswordHash,
            Role = "student",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.ChangePasswordAsync(userId, "CurrentPass123!", "NewPass123!");

        // Assert
        Assert.True(result);
        var updatedUser = await _context.Users.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass123!", updatedUser.PasswordHash));
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInvalidCurrentPassword_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentPasswordHash = BCrypt.Net.BCrypt.HashPassword("CurrentPass123!");
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = currentPasswordHash,
            Role = "student",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.ChangePasswordAsync(userId, "WrongPassword!", "NewPass123!");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInvalidUserId_ReturnsFalse()
    {
        // Act
        var result = await _userService.ChangePasswordAsync(Guid.NewGuid(), "CurrentPass123!", "NewPass123!");

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
