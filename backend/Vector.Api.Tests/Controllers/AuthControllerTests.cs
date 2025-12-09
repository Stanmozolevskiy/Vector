using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Vector.Api.Controllers;
using Vector.Api.Data;
using Vector.Api.DTOs.Auth;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Controllers;

public class AuthControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IRedisService> _redisServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _authServiceMock = new Mock<IAuthService>();
        _redisServiceMock = new Mock<IRedisService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        
        // Setup Redis mocks
        _redisServiceMock.Setup(r => r.CheckRateLimitAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true); // Allow all requests in tests
        _redisServiceMock.Setup(r => r.ResetRateLimitAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        
        _controller = new AuthController(_authServiceMock.Object, _redisServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test123!@#",
            FirstName = "Test",
            LastName = "User"
        };

        var user = new Vector.Api.Models.User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = "User",
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        _authServiceMock.Setup(x => x.RegisterUserAsync(dto))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdAtResult.StatusCode);
        _authServiceMock.Verify(x => x.RegisterUserAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Register_WithInvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var dto = new RegisterDto();
        _controller.ModelState.AddModelError("Email", "Email is required");

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
        _authServiceMock.Verify(x => x.RegisterUserAsync(It.IsAny<RegisterDto>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Test123!@#"
        };

        var accessToken = "test-token";
        var refreshToken = "test-refresh-token";

        _authServiceMock.Setup(x => x.LoginAsync(dto))
            .ReturnsAsync((accessToken, refreshToken));

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _authServiceMock.Verify(x => x.LoginAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        _authServiceMock.Setup(x => x.LoginAsync(dto))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password."));

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_WithValidToken_ReturnsOk()
    {
        // Arrange
        var token = "valid-token";
        _authServiceMock.Setup(x => x.VerifyEmailAsync(token))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.VerifyEmail(token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _authServiceMock.Verify(x => x.VerifyEmailAsync(token), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var token = "invalid-token";
        _authServiceMock.Setup(x => x.VerifyEmailAsync(token))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.VerifyEmail(token);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_WithValidEmail_ReturnsOk()
    {
        // Arrange
        var dto = new ForgotPasswordDto
        {
            Email = "test@example.com"
        };

        _authServiceMock.Setup(x => x.ForgotPasswordAsync(dto.Email))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ForgotPassword(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _authServiceMock.Verify(x => x.ForgotPasswordAsync(dto.Email), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ReturnsOk()
    {
        // Arrange
        var dto = new ResetPasswordDto
        {
            Token = "valid-token",
            Email = "test@example.com",
            NewPassword = "NewPassword123!@#"
        };

        _authServiceMock.Setup(x => x.ResetPasswordAsync(dto))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ResetPassword(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _authServiceMock.Verify(x => x.ResetPasswordAsync(dto), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var dto = new ResetPasswordDto
        {
            Token = "invalid-token",
            Email = "test@example.com",
            NewPassword = "NewPassword123!@#"
        };

        _authServiceMock.Setup(x => x.ResetPasswordAsync(dto))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ResetPassword(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidToken_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext
        {
            User = principal
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _authServiceMock.Setup(x => x.LogoutAsync(userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Logout();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _authServiceMock.Verify(x => x.LogoutAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Logout_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var claims = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(claims);
        
        var httpContext = new DefaultHttpContext
        {
            User = principal
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.Logout();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
        _authServiceMock.Verify(x => x.LogoutAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Logout_WithServiceException_StillReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext
        {
            User = principal
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _authServiceMock.Setup(x => x.LogoutAsync(userId))
            .ThrowsAsync(new Exception("Redis error"));

        // Act
        var result = await _controller.Logout();

        // Assert - Should still return OK even if service fails
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}

