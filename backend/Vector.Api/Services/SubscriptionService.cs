using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Vector.Api.Data;
using Vector.Api.DTOs.Subscription;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private const string PLANS_CACHE_KEY = "subscription_plans";
    private static readonly TimeSpan PLANS_CACHE_DURATION = TimeSpan.FromHours(24);

    public SubscriptionService(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync()
    {
        // Try to get from cache first
        if (_cache.TryGetValue(PLANS_CACHE_KEY, out List<SubscriptionPlanDto>? cachedPlans) && cachedPlans != null)
        {
            return Task.FromResult(cachedPlans);
        }

        // Define subscription plans (including free plan)
        var plans = new List<SubscriptionPlanDto>
        {
            new SubscriptionPlanDto
            {
                Id = "free",
                Name = "Free Plan",
                Description = "Get started with basic features. Perfect for exploring Vector.",
                Price = 0m,
                Currency = "USD",
                BillingPeriod = "free",
                Features = new List<string>
                {
                    "Limited course access",
                    "Basic features",
                    "Community access"
                },
                IsPopular = false,
                StripePriceId = null
            },
            new SubscriptionPlanDto
            {
                Id = "monthly",
                Name = "Monthly Plan",
                Description = "Perfect for trying out Vector. Cancel anytime.",
                Price = 29.99m,
                Currency = "USD",
                BillingPeriod = "monthly",
                Features = new List<string>
                {
                    "Access to all courses",
                    "Unlimited mock interviews",
                    "Resume reviews",
                    "Community access",
                    "Email support"
                },
                IsPopular = false,
                StripePriceId = null // Will be configured when Stripe is set up
            },
            new SubscriptionPlanDto
            {
                Id = "annual",
                Name = "Annual Plan",
                Description = "Best value! Save 2 months with annual billing.",
                Price = 299.99m,
                Currency = "USD",
                BillingPeriod = "annual",
                Features = new List<string>
                {
                    "Access to all courses",
                    "Unlimited mock interviews",
                    "Resume reviews",
                    "Community access",
                    "Priority email support",
                    "2 months free (save $60)"
                },
                IsPopular = true,
                StripePriceId = null // Will be configured when Stripe is set up
            },
        };

        // Cache the plans
        _cache.Set(PLANS_CACHE_KEY, plans, PLANS_CACHE_DURATION);

        return Task.FromResult(plans);
    }

    public async Task<SubscriptionPlanDto?> GetPlanByIdAsync(string planId)
    {
        var plans = await GetAvailablePlansAsync();
        return plans.FirstOrDefault(p => p.Id.Equals(planId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<SubscriptionDto?> GetCurrentSubscriptionAsync(Guid userId)
    {
        // Optimize query: Use Select to only fetch needed fields and AsNoTracking for read-only
        var subscription = await _context.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.Status == "active")
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.PlanType,
                s.Status,
                s.CurrentPeriodStart,
                s.CurrentPeriodEnd,
                s.Price,
                s.Currency,
                s.CreatedAt,
                s.CancelledAt
            })
            .FirstOrDefaultAsync();

        if (subscription == null)
        {
            // Return free subscription as default
            return await GetOrCreateFreeSubscriptionAsync(userId);
        }

        var plan = await GetPlanByIdAsync(subscription.PlanType);
        
        return new SubscriptionDto
        {
            Id = subscription.Id,
            PlanType = subscription.PlanType,
            Status = subscription.Status,
            CurrentPeriodStart = subscription.CurrentPeriodStart,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd,
            Price = subscription.Price,
            Currency = subscription.Currency,
            CreatedAt = subscription.CreatedAt,
            CancelledAt = subscription.CancelledAt,
            Plan = plan
        };
    }

    public async Task<SubscriptionDto> GetOrCreateFreeSubscriptionAsync(Guid userId)
    {
        // Optimize query: Use Select to only fetch needed fields
        var existingSubscription = await _context.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.PlanType == "free" && s.Status == "active")
            .Select(s => new
            {
                s.Id,
                s.PlanType,
                s.Status,
                s.CurrentPeriodStart,
                s.CurrentPeriodEnd,
                s.Price,
                s.Currency,
                s.CreatedAt,
                s.CancelledAt
            })
            .FirstOrDefaultAsync();

        if (existingSubscription != null)
        {
            var plan = await GetPlanByIdAsync("free");
            return new SubscriptionDto
            {
                Id = existingSubscription.Id,
                PlanType = existingSubscription.PlanType,
                Status = existingSubscription.Status,
                CurrentPeriodStart = existingSubscription.CurrentPeriodStart,
                CurrentPeriodEnd = existingSubscription.CurrentPeriodEnd,
                Price = existingSubscription.Price,
                Currency = existingSubscription.Currency,
                CreatedAt = existingSubscription.CreatedAt,
                CancelledAt = existingSubscription.CancelledAt,
                Plan = plan
            };
        }

        // Create free subscription
        var freePlan = await GetPlanByIdAsync("free");
        var now = DateTime.UtcNow;
        var freeSubscription = new Subscription
        {
            UserId = userId,
            PlanType = "free",
            Status = "active",
            CurrentPeriodStart = now,
            CurrentPeriodEnd = DateTime.MaxValue, // Free plan never expires
            Price = 0m,
            Currency = "USD",
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Subscriptions.Add(freeSubscription);
        await _context.SaveChangesAsync();

        return new SubscriptionDto
        {
            Id = freeSubscription.Id,
            PlanType = freeSubscription.PlanType,
            Status = freeSubscription.Status,
            CurrentPeriodStart = freeSubscription.CurrentPeriodStart,
            CurrentPeriodEnd = freeSubscription.CurrentPeriodEnd,
            Price = freeSubscription.Price,
            Currency = freeSubscription.Currency,
            CreatedAt = freeSubscription.CreatedAt,
            CancelledAt = freeSubscription.CancelledAt,
            Plan = freePlan
        };
    }

    public async Task<SubscriptionDto> UpdateSubscriptionAsync(Guid userId, string planId)
    {
        var plan = await GetPlanByIdAsync(planId);
        if (plan == null)
        {
            throw new ArgumentException($"Plan '{planId}' not found");
        }

        // Cancel existing active subscription
        var existingSubscription = await _context.Subscriptions
            .Where(s => s.UserId == userId && s.Status == "active")
            .FirstOrDefaultAsync();

        if (existingSubscription != null && existingSubscription.PlanType != planId)
        {
            existingSubscription.Status = "cancelled";
            existingSubscription.CancelledAt = DateTime.UtcNow;
            existingSubscription.UpdatedAt = DateTime.UtcNow;
        }

        // Create new subscription
        var now = DateTime.UtcNow;
        DateTime periodEnd;
        
        if (plan.BillingPeriod == "monthly")
        {
            periodEnd = now.AddMonths(1);
        }
        else if (plan.BillingPeriod == "annual")
        {
            periodEnd = now.AddYears(1);
        }
        else if (plan.BillingPeriod == "one-time" || plan.BillingPeriod == "free")
        {
            periodEnd = DateTime.MaxValue;
        }
        else
        {
            periodEnd = now.AddMonths(1); // Default to monthly
        }

        var newSubscription = new Subscription
        {
            UserId = userId,
            PlanType = planId,
            Status = "active",
            CurrentPeriodStart = now,
            CurrentPeriodEnd = periodEnd,
            Price = plan.Price,
            Currency = plan.Currency,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Subscriptions.Add(newSubscription);
        await _context.SaveChangesAsync();

        return new SubscriptionDto
        {
            Id = newSubscription.Id,
            PlanType = newSubscription.PlanType,
            Status = newSubscription.Status,
            CurrentPeriodStart = newSubscription.CurrentPeriodStart,
            CurrentPeriodEnd = newSubscription.CurrentPeriodEnd,
            Price = newSubscription.Price,
            Currency = newSubscription.Currency,
            CreatedAt = newSubscription.CreatedAt,
            CancelledAt = newSubscription.CancelledAt,
            Plan = plan
        };
    }

    public async Task<bool> CancelSubscriptionAsync(Guid userId)
    {
        // Optimize: Combine queries to find most recent active subscription (excluding free plan)
        // This ensures we cancel paid subscriptions first
        var subscription = await _context.Subscriptions
            .Where(s => s.UserId == userId && s.Status == "active")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
        
        // If found subscription is free, return false (can't cancel free plan)
        if (subscription != null && subscription.PlanType == "free")
        {
            return false;
        }

        if (subscription == null)
        {
            return false;
        }

        subscription.Status = "cancelled";
        subscription.CancelledAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;

        // Create free subscription as replacement
        await GetOrCreateFreeSubscriptionAsync(userId);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<InvoiceDto>> GetInvoicesAsync(Guid userId)
    {
        // For now, return empty list. Will be implemented when Stripe integration is complete
        // This will fetch invoices from Stripe or Payment table
        return await Task.FromResult(new List<InvoiceDto>());
    }
}

