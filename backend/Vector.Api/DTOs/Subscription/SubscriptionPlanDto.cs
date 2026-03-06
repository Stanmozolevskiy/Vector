namespace Vector.Api.DTOs.Subscription;

public class SubscriptionPlanDto
{
    public string Id { get; set; } = string.Empty; // monthly, annual, lifetime
    public string Name { get; set; } = string.Empty; // Monthly Plan, Annual Plan, Lifetime Plan
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string BillingPeriod { get; set; } = string.Empty; // monthly, annual, one-time
    public List<string> Features { get; set; } = new();
    public bool IsPopular { get; set; }
    public string? StripePriceId { get; set; } // Stripe price ID for this plan
}

