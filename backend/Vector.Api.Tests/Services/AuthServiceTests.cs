using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;

    public AuthServiceTests()
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

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Jwt:Secret", "test-secret-key-that-is-at-least-32-characters-long" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" }
        });
        _configuration = configurationBuilder.Build();

        var redisServiceMock = new Mock<IRedisService>();
        redisServiceMock.Setup(r => r.StoreRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        redisServiceMock.Setup(r => r.CheckRateLimitAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        _authService = new AuthService(
            _context,
            _emailServiceMock.Object,
            serviceProviderMock.Object,
            _loggerMock.Object,
            _jwtServiceMock.Object,
            redisServiceMock.Object
        );
    }

    [Fact]
    public async Task RegisterUserAsync_WithValidData_CreatesUser()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test123!@#",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var result = await _authService.RegisterUserAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Email, result.Email);
        Assert.Equal(dto.FirstName, result.FirstName);
        Assert.Equal(dto.LastName, result.LastName);
        Assert.False(result.EmailVerified);
        Assert.NotNull(result.PasswordHash);
        Assert.NotEqual(dto.Password, result.PasswordHash);

        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        Assert.NotNull(userInDb);
        Assert.True(PasswordHasher.VerifyPassword(dto.Password, userInDb.PasswordHash));
    }

    [Fact]
    public async Task RegisterUserAsync_WithDuplicateEmail_ThrowsException()
    {
        // Arrange
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@example.com",
            PasswordHash = PasswordHasher.HashPassword("Password123!"),
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(existingUser);
        await _context.SaveChangesAsync();

        var dto = new RegisterDto
        {
            Email = "existing@example.com",
            Password = "Test123!@#",
            FirstName = "Test",
            LastName = "User"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterUserAsync(dto)
        );
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var password = "Test123!@#";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = PasswordHasher.HashPassword(password),
            Role = "student",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var dto = new LoginDto
        {
            Email = user.Email,
            Password = password
        };

        var expectedToken = "test-access-token";
        _jwtServiceMock.Setup(x => x.GenerateAccessToken(user.Id, user.Role))
            .Returns(expectedToken);
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken(user.Id))
            .Returns("test-refresh-token");

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedToken, result);
        
        // Verify refresh token was created
        var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == user.Id);
        Assert.NotNull(refreshToken);
        Assert.Equal("test-refresh-token", refreshToken.Token);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = PasswordHasher.HashPassword("CorrectPassword123!"),
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var dto = new LoginDto
        {
            Email = user.Email,
            Password = "WrongPassword"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(dto)
        );
    }

    [Fact]
    public async Task LoginAsync_WithUnverifiedEmail_ThrowsException()
    {
        // Arrange
        var password = "Test123!@#";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = PasswordHasher.HashPassword(password),
            Role = "User",
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var dto = new LoginDto
        {
            Email = user.Email,
            Password = password
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.LoginAsync(dto)
        );
    }

    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = PasswordHasher.HashPassword("Password123!"),
            Role = "User",
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var token = "valid-token";
        var emailVerification = new EmailVerification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.EmailVerifications.AddAsync(emailVerification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.VerifyEmailAsync(token);

        // Assert
        Assert.True(result);
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.True(updatedUser!.EmailVerified);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}

