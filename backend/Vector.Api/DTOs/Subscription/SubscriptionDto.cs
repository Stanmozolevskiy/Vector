namespace Vector.Api.DTOs.Subscription;

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public string PlanType { get; set; } = string.Empty; // free, monthly, annual, lifetime
    public string Status { get; set; } = string.Empty; // active, cancelled, expired, past_due
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public SubscriptionPlanDto? Plan { get; set; }
}

