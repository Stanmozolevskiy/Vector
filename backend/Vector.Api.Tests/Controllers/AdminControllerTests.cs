using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Vector.Api.Controllers;
using Vector.Api.Data;
using Vector.Api.Models;
using Xunit;

namespace Vector.Api.Tests.Controllers;

public class AdminControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AdminController _controller;
    private readonly ILogger<AdminController> _logger;

    public AdminControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _logger = new LoggerFactory().CreateLogger<AdminController>();
        _controller = new AdminController(_context, _logger);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private ClaimsPrincipal CreateClaimsPrincipal(Guid userId, string role = "admin")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        return new ClaimsPrincipal(identity);
    }

    private async Task SeedTestUsers()
    {
        var users = new List<User>
        {
            new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@test.com",
                FirstName = "Admin",
                LastName = "User",
                Role = "admin",
                EmailVerified = true,
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "student@test.com",
                FirstName = "Student",
                LastName = "User",
                Role = "student",
                EmailVerified = true,
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "coach@test.com",
                FirstName = "Coach",
                LastName = "User",
                Role = "coach",
                EmailVerified = false,
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllUsers_ReturnsOkWithUsers()
    {
        // Arrange
        await SeedTestUsers();
        var adminId = Guid.NewGuid();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateClaimsPrincipal(adminId) }
        };

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var response = okResult.Value.GetType().GetProperty("users")?.GetValue(okResult.Value);
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetStatistics_ReturnsCorrectCounts()
    {
        // Arrange
        await SeedTestUsers();
        var adminId = Guid.NewGuid();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateClaimsPrincipal(adminId) }
        };

        // Act
        var result = await _controller.GetStatistics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var value = okResult.Value;
        var totalUsers = value.GetType().GetProperty("totalUsers")?.GetValue(value);
        Assert.Equal(3, totalUsers);
    }

    [Fact]
    public async Task UpdateUserRole_WithValidRole_ReturnsOk()
    {
        // Arrange
        await SeedTestUsers();
        var user = await _context.Users.FirstAsync(u => u.Role == "student");
        var adminId = Guid.NewGuid();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateClaimsPrincipal(adminId) }
        };

        var dto = new UpdateRoleDto { Role = "coach" };

        // Act
        var result = await _controller.UpdateUserRole(user.Id, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // Verify role was updated
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.Equal("coach", updatedUser!.Role);
    }

    [Fact]
    public async Task UpdateUserRole_WithInvalidRole_ReturnsBadRequest()
    {
        // Arrange
        await SeedTestUsers();
        var user = await _context.Users.FirstAsync();
        var adminId = Guid.NewGuid();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateClaimsPrincipal(adminId) }
        };

        var dto = new UpdateRoleDto { Role = "invalid-role" };

        // Act
        var result = await _controller.UpdateUserRole(user.Id, dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateUserRole_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateClaimsPrincipal(adminId) }
        };

        var dto = new UpdateRoleDto { Role = "student" };

        // Act
        var result = await _controller.UpdateUserRole(Guid.NewGuid(), dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteUser_WithValidUser_ReturnsOk()
    {
        // Arrange
        await SeedTestUsers();
        var user = await _context.Users.FirstAsync(u => u.Role == "student");
        var adminId = Guid.NewGuid();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateClaimsPrincipal(adminId) }
        };

        // Act
        var result = await _controller.DeleteUser(user.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Verify user was deleted
        var deletedUser = await _context.Users.FindAsync(user.Id);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task DeleteUser_LastAdmin_ReturnsBadRequest()
    {
        // Arrange
        await SeedTestUsers();
        var admin = await _context.Users.FirstAsync(u => u.Role == "admin");
        var adminId = Guid.NewGuid();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateClaimsPrincipal(adminId) }
        };

        // Act
        var result = await _controller.DeleteUser(admin.Id);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteUser_NonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateClaimsPrincipal(adminId) }
        };

        // Act
        var result = await _controller.DeleteUser(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}

