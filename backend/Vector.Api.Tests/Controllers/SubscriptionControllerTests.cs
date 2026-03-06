using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Vector.Api.Controllers;
using Vector.Api.DTOs.Subscription;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Controllers;

public class SubscriptionControllerTests
{
    private readonly Mock<ISubscriptionService> _subscriptionServiceMock;
    private readonly SubscriptionController _controller;

    public SubscriptionControllerTests()
    {
        _subscriptionServiceMock = new Mock<ISubscriptionService>();
        _controller = new SubscriptionController(_subscriptionServiceMock.Object);
    }

    [Fact]
    public async Task GetPlans_ReturnsOkWithPlans()
    {
        // Arrange
        var plans = new List<SubscriptionPlanDto>
        {
            new SubscriptionPlanDto { Id = "free", Name = "Free Plan", Price = 0m },
            new SubscriptionPlanDto { Id = "monthly", Name = "Monthly Plan", Price = 29.99m },
            new SubscriptionPlanDto { Id = "annual", Name = "Annual Plan", Price = 299.99m }
        };
        _subscriptionServiceMock.Setup(x => x.GetAvailablePlansAsync())
            .ReturnsAsync(plans);

        // Act
        var result = await _controller.GetPlans();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPlans = Assert.IsAssignableFrom<List<SubscriptionPlanDto>>(okResult.Value);
        Assert.Equal(3, returnedPlans.Count);
        _subscriptionServiceMock.Verify(x => x.GetAvailablePlansAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPlan_WithValidId_ReturnsOk()
    {
        // Arrange
        var plan = new SubscriptionPlanDto { Id = "monthly", Name = "Monthly Plan", Price = 29.99m };
        _subscriptionServiceMock.Setup(x => x.GetPlanByIdAsync("monthly"))
            .ReturnsAsync(plan);

        // Act
        var result = await _controller.GetPlan("monthly");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPlan = Assert.IsType<SubscriptionPlanDto>(okResult.Value);
        Assert.Equal("monthly", returnedPlan.Id);
        _subscriptionServiceMock.Verify(x => x.GetPlanByIdAsync("monthly"), Times.Once);
    }

    [Fact]
    public async Task GetPlan_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _subscriptionServiceMock.Setup(x => x.GetPlanByIdAsync("invalid"))
            .ReturnsAsync((SubscriptionPlanDto?)null);

        // Act
        var result = await _controller.GetPlan("invalid");

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        _subscriptionServiceMock.Verify(x => x.GetPlanByIdAsync("invalid"), Times.Once);
    }

    [Fact]
    public async Task GetMySubscription_WithValidUser_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var subscription = new SubscriptionDto
        {
            Id = Guid.NewGuid(),
            PlanType = "free",
            Status = "active",
            Price = 0m,
            Currency = "USD"
        };

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _subscriptionServiceMock.Setup(x => x.GetCurrentSubscriptionAsync(userId))
            .ReturnsAsync(subscription);

        // Act
        var result = await _controller.GetMySubscription();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedSubscription = Assert.IsType<SubscriptionDto>(okResult.Value);
        Assert.Equal("free", returnedSubscription.PlanType);
        _subscriptionServiceMock.Verify(x => x.GetCurrentSubscriptionAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetMySubscription_WithInvalidUser_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await _controller.GetMySubscription();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        _subscriptionServiceMock.Verify(x => x.GetCurrentSubscriptionAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task UpdateSubscription_WithValidData_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new UpdateSubscriptionDto { PlanId = "monthly" };
        var subscription = new SubscriptionDto
        {
            Id = Guid.NewGuid(),
            PlanType = "monthly",
            Status = "active",
            Price = 29.99m,
            Currency = "USD"
        };

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _subscriptionServiceMock.Setup(x => x.UpdateSubscriptionAsync(userId, "monthly"))
            .ReturnsAsync(subscription);

        // Act
        var result = await _controller.UpdateSubscription(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedSubscription = Assert.IsType<SubscriptionDto>(okResult.Value);
        Assert.Equal("monthly", returnedSubscription.PlanType);
        _subscriptionServiceMock.Verify(x => x.UpdateSubscriptionAsync(userId, "monthly"), Times.Once);
    }

    [Fact]
    public async Task UpdateSubscription_WithInvalidPlan_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new UpdateSubscriptionDto { PlanId = "invalid" };

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _subscriptionServiceMock.Setup(x => x.UpdateSubscriptionAsync(userId, "invalid"))
            .ThrowsAsync(new ArgumentException("Plan 'invalid' not found"));

        // Act
        var result = await _controller.UpdateSubscription(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        _subscriptionServiceMock.Verify(x => x.UpdateSubscriptionAsync(userId, "invalid"), Times.Once);
    }

    [Fact]
    public async Task CancelSubscription_WithValidUser_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _subscriptionServiceMock.Setup(x => x.CancelSubscriptionAsync(userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CancelSubscription();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _subscriptionServiceMock.Verify(x => x.CancelSubscriptionAsync(userId), Times.Once);
    }

    [Fact]
    public async Task CancelSubscription_WithFreePlan_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _subscriptionServiceMock.Setup(x => x.CancelSubscriptionAsync(userId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CancelSubscription();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        _subscriptionServiceMock.Verify(x => x.CancelSubscriptionAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetInvoices_WithValidUser_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invoices = new List<InvoiceDto>();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _subscriptionServiceMock.Setup(x => x.GetInvoicesAsync(userId))
            .ReturnsAsync(invoices);

        // Act
        var result = await _controller.GetInvoices();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedInvoices = Assert.IsAssignableFrom<List<InvoiceDto>>(okResult.Value);
        Assert.Empty(returnedInvoices);
        _subscriptionServiceMock.Verify(x => x.GetInvoicesAsync(userId), Times.Once);
    }
}

