using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vector.Api.Data;
using Vector.Api.DTOs.Subscription;
using Vector.Api.Models;
using Xunit;

namespace Vector.Api.Tests.Integration;

public class SubscriptionIntegrationTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private IServiceScope? _scope;
    private ApplicationDbContext? _context;

    public SubscriptionIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    private ApplicationDbContext GetContext()
    {
        if (_context == null)
        {
            _scope = _factory.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }
        return _context;
    }

    [Fact]
    public async Task GetPlans_ReturnsAllPlans()
    {
        // Act
        var response = await _client.GetAsync("/api/subscriptions/plans");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var plans = await response.Content.ReadFromJsonAsync<List<SubscriptionPlanDto>>();
        Assert.NotNull(plans);
        Assert.Equal(3, plans.Count);
        Assert.Contains(plans, p => p.Id == "free");
        Assert.Contains(plans, p => p.Id == "monthly");
        Assert.Contains(plans, p => p.Id == "annual");
    }

    [Fact]
    public async Task GetPlan_WithValidId_ReturnsPlan()
    {
        // Act
        var response = await _client.GetAsync("/api/subscriptions/plans/monthly");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var plan = await response.Content.ReadFromJsonAsync<SubscriptionPlanDto>();
        Assert.NotNull(plan);
        Assert.Equal("monthly", plan.Id);
        Assert.Equal("Monthly Plan", plan.Name);
        Assert.Equal(29.99m, plan.Price);
    }

    [Fact]
    public async Task GetPlan_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/subscriptions/plans/invalid");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMySubscription_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/subscriptions/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMySubscription_WithValidToken_ReturnsFreeSubscription()
    {
        // Arrange - Create user and get token
        var user = await CreateAuthenticatedUserAsync("subscription@example.com");

        // Act
        var response = await _client.GetAsync("/api/subscriptions/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var subscription = await response.Content.ReadFromJsonAsync<SubscriptionDto>();
        Assert.NotNull(subscription);
        Assert.Equal("free", subscription.PlanType);
        Assert.Equal("active", subscription.Status);
        Assert.Equal(0m, subscription.Price);
    }

    [Fact]
    public async Task UpdateSubscription_WithValidPlan_CreatesNewSubscription()
    {
        // Arrange - Create user and get token
        var user = await CreateAuthenticatedUserAsync("update@example.com");
        var updateDto = new UpdateSubscriptionDto { PlanId = "monthly" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/subscriptions/update", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var subscription = await response.Content.ReadFromJsonAsync<SubscriptionDto>();
        Assert.NotNull(subscription);
        Assert.Equal("monthly", subscription.PlanType);
        Assert.Equal(29.99m, subscription.Price);

        // Verify subscription was saved to database - use the same context instance
        var verifyContext = GetContext();
        var dbSubscription = await verifyContext.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.PlanType == "monthly");
        Assert.NotNull(dbSubscription);
        Assert.Equal("active", dbSubscription.Status);
    }

    [Fact]
    public async Task UpdateSubscription_WithInvalidPlan_ReturnsBadRequest()
    {
        // Arrange - Create user and get token
        var user = await CreateAuthenticatedUserAsync("invalid@example.com");
        var updateDto = new UpdateSubscriptionDto { PlanId = "invalid-plan" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/subscriptions/update", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CancelSubscription_WithActiveSubscription_ReturnsOk()
    {
        // Arrange - Create user and upgrade to monthly subscription via API
        var user = await CreateAuthenticatedUserAsync("cancel@example.com");
        
        // Upgrade to monthly subscription using the API (this ensures it's in the same database context)
        var updateDto = new UpdateSubscriptionDto { PlanId = "monthly" };
        var updateResponse = await _client.PutAsJsonAsync("/api/subscriptions/update", updateDto);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        
        var updatedSubscription = await updateResponse.Content.ReadFromJsonAsync<SubscriptionDto>();
        Assert.NotNull(updatedSubscription);
        Assert.Equal("monthly", updatedSubscription.PlanType);
        
        // Wait a moment for the database to be updated
        await Task.Delay(100);

        // Act - Cancel the subscription
        var response = await _client.PutAsync("/api/subscriptions/cancel", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Wait a moment for the cancellation to be saved
        await Task.Delay(100);
        
        // Verify subscription was cancelled - use the same context instance
        var verifyContext = GetContext();
        var cancelled = await verifyContext.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.PlanType == "monthly");
        Assert.NotNull(cancelled);
        Assert.Equal("cancelled", cancelled.Status);
        Assert.NotNull(cancelled.CancelledAt);

        // Verify free subscription was created
        var freeSubscription = await verifyContext.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.PlanType == "free" && s.Status == "active");
        Assert.NotNull(freeSubscription);
    }

    [Fact]
    public async Task CancelSubscription_WithFreePlan_ReturnsBadRequest()
    {
        // Arrange - Create user (will have free plan by default)
        var user = await CreateAuthenticatedUserAsync("freecancel@example.com");

        // Act
        var response = await _client.PutAsync("/api/subscriptions/cancel", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetInvoices_WithValidToken_ReturnsEmptyList()
    {
        // Arrange - Create user and get token
        var user = await CreateAuthenticatedUserAsync("invoices@example.com");

        // Act
        var response = await _client.GetAsync("/api/subscriptions/invoices");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invoices = await response.Content.ReadFromJsonAsync<List<InvoiceDto>>();
        Assert.NotNull(invoices);
        Assert.Empty(invoices);
    }

    private async Task<User> CreateAuthenticatedUserAsync(string email)
    {
        // Register user
        var registerDto = new Vector.Api.DTOs.Auth.RegisterDto
        {
            Email = email,
            Password = "Test1234!",
            FirstName = "Test",
            LastName = "User"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        
        // Ensure registration succeeded
        if (registerResponse.StatusCode != HttpStatusCode.Created)
        {
            var errorContent = await registerResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"User registration failed: {errorContent}");
        }

        // Wait a moment for the database to be updated (InMemory database should be immediate, but just in case)
        await Task.Delay(100);

        // Verify email - use the same context instance and retry if needed
        var context = GetContext();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());
        
        // Retry once if user not found (database might need a moment)
        if (user == null)
        {
            await Task.Delay(100);
            user = await context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());
        }

        if (user == null)
        {
            throw new InvalidOperationException($"User {email} was not created after registration");
        }

        // Verify email
        user.EmailVerified = true;
        await context.SaveChangesAsync();

        // Login to get token
        var loginDto = new Vector.Api.DTOs.Auth.LoginDto
        {
            Email = email,
            Password = "Test1234!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        if (loginResponse.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await loginResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed for {email}: {errorContent}");
        }

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        if (!loginResult.TryGetProperty("accessToken", out var tokenElement))
        {
            throw new InvalidOperationException($"Login response missing accessToken for {email}");
        }

        var token = tokenElement.GetString();
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException($"Access token is null or empty for {email}");
        }

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return user;
    }

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
        _scope?.Dispose();
        _client.Dispose();
    }
}

