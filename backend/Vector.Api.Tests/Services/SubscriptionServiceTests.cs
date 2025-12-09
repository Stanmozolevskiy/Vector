using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Vector.Api.Data;
using Vector.Api.DTOs.Subscription;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Services;

public class SubscriptionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        _subscriptionService = new SubscriptionService(_context, memoryCache);
    }

    [Fact]
    public async Task GetAvailablePlansAsync_ReturnsAllPlans()
    {
        // Act
        var plans = await _subscriptionService.GetAvailablePlansAsync();

        // Assert
        Assert.NotNull(plans);
        Assert.Equal(3, plans.Count);
        Assert.Contains(plans, p => p.Id == "free");
        Assert.Contains(plans, p => p.Id == "monthly");
        Assert.Contains(plans, p => p.Id == "annual");
    }

    [Fact]
    public async Task GetAvailablePlansAsync_ReturnsPlansWithCorrectProperties()
    {
        // Act
        var plans = await _subscriptionService.GetAvailablePlansAsync();

        // Assert
        foreach (var plan in plans)
        {
            Assert.NotEmpty(plan.Id);
            Assert.NotEmpty(plan.Name);
            Assert.NotEmpty(plan.Description);
            Assert.True(plan.Price >= 0); // Free plan has price 0
            Assert.NotEmpty(plan.Currency);
            Assert.NotEmpty(plan.BillingPeriod);
            Assert.NotEmpty(plan.Features);
        }
    }

    [Fact]
    public async Task GetAvailablePlansAsync_FreePlanHasZeroPrice()
    {
        // Act
        var plans = await _subscriptionService.GetAvailablePlansAsync();

        // Assert
        var freePlan = plans.FirstOrDefault(p => p.Id == "free");
        Assert.NotNull(freePlan);
        Assert.Equal(0m, freePlan.Price);
        Assert.Equal("free", freePlan.BillingPeriod);
    }

    [Fact]
    public async Task GetAvailablePlansAsync_AnnualPlanIsPopular()
    {
        // Act
        var plans = await _subscriptionService.GetAvailablePlansAsync();

        // Assert
        var annualPlan = plans.FirstOrDefault(p => p.Id == "annual");
        Assert.NotNull(annualPlan);
        Assert.True(annualPlan.IsPopular);
    }

    [Fact]
    public async Task GetAvailablePlansAsync_MonthlyPlanHasCorrectPrice()
    {
        // Act
        var plans = await _subscriptionService.GetAvailablePlansAsync();

        // Assert
        var monthlyPlan = plans.FirstOrDefault(p => p.Id == "monthly");
        Assert.NotNull(monthlyPlan);
        Assert.Equal(29.99m, monthlyPlan.Price);
        Assert.Equal("monthly", monthlyPlan.BillingPeriod);
    }

    [Fact]
    public async Task GetAvailablePlansAsync_AnnualPlanHasCorrectPrice()
    {
        // Act
        var plans = await _subscriptionService.GetAvailablePlansAsync();

        // Assert
        var annualPlan = plans.FirstOrDefault(p => p.Id == "annual");
        Assert.NotNull(annualPlan);
        Assert.Equal(299.99m, annualPlan.Price);
        Assert.Equal("annual", annualPlan.BillingPeriod);
    }


    [Fact]
    public async Task GetAvailablePlansAsync_AllPlansHaveFeatures()
    {
        // Act
        var plans = await _subscriptionService.GetAvailablePlansAsync();

        // Assert
        foreach (var plan in plans)
        {
            Assert.True(plan.Features.Count > 0);
        }
    }

    [Fact]
    public async Task GetPlanByIdAsync_ReturnsCorrectPlan()
    {
        // Act
        var plan = await _subscriptionService.GetPlanByIdAsync("monthly");

        // Assert
        Assert.NotNull(plan);
        Assert.Equal("monthly", plan.Id);
        Assert.Equal("Monthly Plan", plan.Name);
    }

    [Fact]
    public async Task GetPlanByIdAsync_ReturnsNullForInvalidPlan()
    {
        // Act
        var plan = await _subscriptionService.GetPlanByIdAsync("invalid");

        // Assert
        Assert.Null(plan);
    }

    [Fact]
    public async Task GetPlanByIdAsync_CaseInsensitive()
    {
        // Act
        var plan1 = await _subscriptionService.GetPlanByIdAsync("MONTHLY");
        var plan2 = await _subscriptionService.GetPlanByIdAsync("monthly");

        // Assert
        Assert.NotNull(plan1);
        Assert.NotNull(plan2);
        Assert.Equal(plan1.Id, plan2.Id);
    }

    [Fact]
    public async Task GetOrCreateFreeSubscriptionAsync_CreatesFreeSubscriptionForNewUser()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var subscription = await _subscriptionService.GetOrCreateFreeSubscriptionAsync(userId);

        // Assert
        Assert.NotNull(subscription);
        Assert.Equal("free", subscription.PlanType);
        Assert.Equal("active", subscription.Status);
        Assert.Equal(0m, subscription.Price);
        Assert.NotNull(subscription.Plan);
        Assert.Equal("free", subscription.Plan.Id);

        // Verify it was saved to database
        var dbSubscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
        Assert.NotNull(dbSubscription);
        Assert.Equal("free", dbSubscription.PlanType);
    }

    [Fact]
    public async Task GetOrCreateFreeSubscriptionAsync_ReturnsExistingFreeSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingSubscription = new Subscription
        {
            UserId = userId,
            PlanType = "free",
            Status = "active",
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.MaxValue,
            Price = 0m,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Subscriptions.Add(existingSubscription);
        await _context.SaveChangesAsync();

        // Act
        var subscription = await _subscriptionService.GetOrCreateFreeSubscriptionAsync(userId);

        // Assert
        Assert.NotNull(subscription);
        Assert.Equal(existingSubscription.Id, subscription.Id);
        Assert.Equal("free", subscription.PlanType);

        // Verify only one subscription exists
        var count = await _context.Subscriptions.CountAsync(s => s.UserId == userId && s.PlanType == "free");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetCurrentSubscriptionAsync_ReturnsFreeSubscriptionWhenNoneExists()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var subscription = await _subscriptionService.GetCurrentSubscriptionAsync(userId);

        // Assert
        Assert.NotNull(subscription);
        Assert.Equal("free", subscription.PlanType);
        Assert.Equal("active", subscription.Status);
    }

    [Fact]
    public async Task GetCurrentSubscriptionAsync_ReturnsExistingActiveSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var subscription = new Subscription
        {
            UserId = userId,
            PlanType = "monthly",
            Status = "active",
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
            Price = 29.99m,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.GetCurrentSubscriptionAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(subscription.Id, result.Id);
        Assert.Equal("monthly", result.PlanType);
        Assert.NotNull(result.Plan);
        Assert.Equal("monthly", result.Plan.Id);
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_CreatesNewSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var subscription = await _subscriptionService.UpdateSubscriptionAsync(userId, "monthly");

        // Assert
        Assert.NotNull(subscription);
        Assert.Equal("monthly", subscription.PlanType);
        Assert.Equal("active", subscription.Status);
        Assert.Equal(29.99m, subscription.Price);
        Assert.NotNull(subscription.Plan);
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_CancelsExistingAndCreatesNew()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingSubscription = new Subscription
        {
            UserId = userId,
            PlanType = "free",
            Status = "active",
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.MaxValue,
            Price = 0m,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Subscriptions.Add(existingSubscription);
        await _context.SaveChangesAsync();

        // Act
        var newSubscription = await _subscriptionService.UpdateSubscriptionAsync(userId, "annual");

        // Assert
        Assert.NotNull(newSubscription);
        Assert.Equal("annual", newSubscription.PlanType);

        // Verify old subscription was cancelled
        var cancelled = await _context.Subscriptions.FindAsync(existingSubscription.Id);
        Assert.NotNull(cancelled);
        Assert.Equal("cancelled", cancelled.Status);
        Assert.NotNull(cancelled.CancelledAt);
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_ThrowsExceptionForInvalidPlan()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _subscriptionService.UpdateSubscriptionAsync(userId, "invalid-plan"));
    }

    [Fact]
    public async Task CancelSubscriptionAsync_CancelsActiveSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var subscription = new Subscription
        {
            UserId = userId,
            PlanType = "monthly",
            Status = "active",
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
            Price = 29.99m,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.CancelSubscriptionAsync(userId);

        // Assert
        Assert.True(result);
        var cancelled = await _context.Subscriptions.FindAsync(subscription.Id);
        Assert.NotNull(cancelled);
        Assert.Equal("cancelled", cancelled.Status);
        Assert.NotNull(cancelled.CancelledAt);

        // Verify free subscription was created
        var freeSubscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.PlanType == "free" && s.Status == "active");
        Assert.NotNull(freeSubscription);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ReturnsFalseForFreePlan()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var subscription = new Subscription
        {
            UserId = userId,
            PlanType = "free",
            Status = "active",
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.MaxValue,
            Price = 0m,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.CancelSubscriptionAsync(userId);

        // Assert
        Assert.False(result);
        var stillActive = await _context.Subscriptions.FindAsync(subscription.Id);
        Assert.NotNull(stillActive);
        Assert.Equal("active", stillActive.Status);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ReturnsFalseWhenNoSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _subscriptionService.CancelSubscriptionAsync(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetInvoicesAsync_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var invoices = await _subscriptionService.GetInvoicesAsync(userId);

        // Assert
        Assert.NotNull(invoices);
        Assert.Empty(invoices);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

