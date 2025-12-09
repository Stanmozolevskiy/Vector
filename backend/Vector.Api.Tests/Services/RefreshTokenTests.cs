using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Vector.Api.Data;
using Vector.Api.DTOs.Auth;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Services;

public class RefreshTokenTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;

    public RefreshTokenTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _emailServiceMock = new Mock<IEmailService>();
        _jwtServiceMock = new Mock<IJwtService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        var configData = new Dictionary<string, string>
        {
            { "Jwt:Secret", "test-secret-key-for-jwt-token-generation-minimum-32-chars" },
            { "Jwt:Issuer", "Vector" },
            { "Jwt:Audience", "Vector" },
            { "Jwt:AccessTokenExpirationMinutes", "15" },
            { "Jwt:RefreshTokenExpirationDays", "7" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var serviceProviderMock = new Mock<IServiceProvider>();
        var redisServiceMock = new Mock<IRedisService>();
        redisServiceMock.Setup(r => r.IsTokenBlacklistedAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        redisServiceMock.Setup(r => r.StoreRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        redisServiceMock.Setup(r => r.BlacklistTokenAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
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

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task<(User user, RefreshToken refreshToken)> CreateUserWithRefreshToken()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            Role = "student",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Token = "valid-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        _context.Users.Add(user);
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return (user, refreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewAccessToken()
    {
        // Arrange
        var (user, oldToken) = await CreateUserWithRefreshToken();

        var claims = new[]
        {
            new Claim("nameid", user.Id.ToString()),
            new Claim("type", "refresh")
        };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _jwtServiceMock.Setup(j => j.ValidateRefreshToken(oldToken.Token))
            .Returns(claimsPrincipal);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(user.Id, user.Role))
            .Returns("new-access-token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken(user.Id))
            .Returns("new-refresh-token");

        // Act
        var (accessToken, refreshToken) = await _authService.RefreshTokenAsync(oldToken.Token);

        // Assert
        Assert.Equal("new-access-token", accessToken);
        Assert.Equal("new-refresh-token", refreshToken);

        // Verify old token was revoked
        var revokedToken = await _context.RefreshTokens.FindAsync(oldToken.Id);
        Assert.NotNull(revokedToken);
        Assert.True(revokedToken.IsRevoked);
        Assert.NotNull(revokedToken.RevokedAt);

        // Verify new token was created
        var newTokenCount = await _context.RefreshTokens
            .CountAsync(rt => rt.UserId == user.Id && !rt.IsRevoked);
        Assert.Equal(1, newTokenCount);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ThrowsUnauthorized()
    {
        // Arrange
        var (user, oldToken) = await CreateUserWithRefreshToken();
        oldToken.IsRevoked = true;
        oldToken.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var claims = new[]
        {
            new Claim("nameid", user.Id.ToString()),
            new Claim("type", "refresh")
        };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _jwtServiceMock.Setup(j => j.ValidateRefreshToken(oldToken.Token))
            .Returns(claimsPrincipal);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.RefreshTokenAsync(oldToken.Token)
        );
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ThrowsUnauthorized()
    {
        // Arrange
        var (user, oldToken) = await CreateUserWithRefreshToken();
        oldToken.ExpiresAt = DateTime.UtcNow.AddDays(-1); // Expired
        await _context.SaveChangesAsync();

        var claims = new[]
        {
            new Claim("nameid", user.Id.ToString()),
            new Claim("type", "refresh")
        };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _jwtServiceMock.Setup(j => j.ValidateRefreshToken(oldToken.Token))
            .Returns(claimsPrincipal);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.RefreshTokenAsync(oldToken.Token)
        );
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ThrowsUnauthorized()
    {
        // Arrange
        _jwtServiceMock.Setup(j => j.ValidateRefreshToken(It.IsAny<string>()))
            .Returns((ClaimsPrincipal?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.RefreshTokenAsync("invalid-token")
        );
    }

    [Fact]
    public async Task RefreshTokenAsync_RotatesToken_CreatesNewToken()
    {
        // Arrange
        var (user, oldToken) = await CreateUserWithRefreshToken();

        var claims = new[]
        {
            new Claim("nameid", user.Id.ToString()),
            new Claim("type", "refresh")
        };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _jwtServiceMock.Setup(j => j.ValidateRefreshToken(oldToken.Token))
            .Returns(claimsPrincipal);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(user.Id, user.Role))
            .Returns("new-access-token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken(user.Id))
            .Returns("rotated-refresh-token");

        var initialTokenCount = await _context.RefreshTokens.CountAsync();

        // Act
        await _authService.RefreshTokenAsync(oldToken.Token);

        // Assert
        var finalTokenCount = await _context.RefreshTokens.CountAsync();
        Assert.Equal(initialTokenCount + 1, finalTokenCount); // One new token added

        // Verify rotation: old revoked, new created
        var revokedTokens = await _context.RefreshTokens
            .CountAsync(rt => rt.UserId == user.Id && rt.IsRevoked);
        var activeTokens = await _context.RefreshTokens
            .CountAsync(rt => rt.UserId == user.Id && !rt.IsRevoked);

        Assert.Equal(1, revokedTokens); // Old token revoked
        Assert.Equal(1, activeTokens); // New token active
    }

    [Fact]
    public async Task LogoutAsync_RevokesAllActiveTokens()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            Role = "student",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create multiple active refresh tokens
        var token1 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token-1",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        var token2 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token-2",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        _context.Users.Add(user);
        _context.RefreshTokens.AddRange(token1, token2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.LogoutAsync(user.Id);

        // Assert
        Assert.True(result);

        // Verify all tokens are revoked
        var revokedTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.IsRevoked)
            .ToListAsync();

        Assert.Equal(2, revokedTokens.Count);
        Assert.All(revokedTokens, token => Assert.NotNull(token.RevokedAt));
    }

    [Fact]
    public async Task GetLatestRefreshTokenAsync_ReturnsNewestToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            Role = "student",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var oldToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "old-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            IsRevoked = false
        };

        var newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "newest-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        _context.Users.Add(user);
        _context.RefreshTokens.AddRange(oldToken, newToken);
        await _context.SaveChangesAsync();

        var claims = new[]
        {
            new Claim("nameid", user.Id.ToString()),
            new Claim("type", "refresh")
        };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _jwtServiceMock.Setup(j => j.ValidateRefreshToken(It.IsAny<string>()))
            .Returns(claimsPrincipal);

        // Act
        var result = await _authService.GetLatestRefreshTokenAsync("old-token");

        // Assert
        Assert.Equal("newest-token", result);
    }
}

