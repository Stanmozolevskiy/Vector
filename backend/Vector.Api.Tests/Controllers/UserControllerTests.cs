using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Vector.Api.Controllers;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Controllers;

public class UserControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _jwtServiceMock = new Mock<IJwtService>();
        _controller = new UserController(_userServiceMock.Object, _jwtServiceMock.Object);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "User")
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

        _userServiceMock.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _userServiceMock.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUser_WithInvalidToken_ReturnsUnauthorized()
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
        var result = await _controller.GetCurrentUser();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
        _userServiceMock.Verify(x => x.GetUserByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentUser_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "User")
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

        _userServiceMock.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }
}

