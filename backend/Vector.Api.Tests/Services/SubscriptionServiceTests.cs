using Xunit;
using Vector.Api.Services;
using Vector.Api.DTOs.Subscription;

namespace Vector.Api.Tests.Services;

public class SubscriptionServiceTests
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionServiceTests()
    {
        _subscriptionService = new SubscriptionService();
    }

    [Fact]
    public async Task GetAvailablePlansAsync_ReturnsAllPlans()
    {
        // Act
        var plans = await _subscriptionService.GetAvailablePlansAsync();

        // Assert
        Assert.NotNull(plans);
        Assert.Equal(3, plans.Count);
        Assert.Contains(plans, p => p.Id == "monthly");
        Assert.Contains(plans, p => p.Id == "annual");
        Assert.Contains(plans, p => p.Id == "lifetime");
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
            Assert.True(plan.Price > 0);
            Assert.NotEmpty(plan.Currency);
            Assert.NotEmpty(plan.BillingPeriod);
            Assert.NotEmpty(plan.Features);
        }
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
    public async Task GetAvailablePlansAsync_LifetimePlanHasCorrectPrice()
    {
        // Act
        var plans = await _subscriptionService.GetAvailablePlansAsync();

        // Assert
        var lifetimePlan = plans.FirstOrDefault(p => p.Id == "lifetime");
        Assert.NotNull(lifetimePlan);
        Assert.Equal(999.99m, lifetimePlan.Price);
        Assert.Equal("one-time", lifetimePlan.BillingPeriod);
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
}

