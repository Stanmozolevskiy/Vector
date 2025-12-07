using Vector.Api.DTOs.Subscription;

namespace Vector.Api.Services;

public class SubscriptionService : ISubscriptionService
{
    public Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync()
    {
        // Define subscription plans
        var plans = new List<SubscriptionPlanDto>
        {
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
            new SubscriptionPlanDto
            {
                Id = "lifetime",
                Name = "Lifetime Plan",
                Description = "One-time payment for lifetime access. Never pay again!",
                Price = 999.99m,
                Currency = "USD",
                BillingPeriod = "one-time",
                Features = new List<string>
                {
                    "Lifetime access to all courses",
                    "Unlimited mock interviews",
                    "Unlimited resume reviews",
                    "Community access",
                    "Priority support",
                    "All future features included"
                },
                IsPopular = false,
                StripePriceId = null // Will be configured when Stripe is set up
            }
        };

        return Task.FromResult(plans);
    }

    public async Task<SubscriptionPlanDto?> GetPlanByIdAsync(string planId)
    {
        var plans = await GetAvailablePlansAsync();
        return plans.FirstOrDefault(p => p.Id.Equals(planId, StringComparison.OrdinalIgnoreCase));
    }
}

