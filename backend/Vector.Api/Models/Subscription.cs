namespace Vector.Api.Models;

public class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    public string PlanType { get; set; } = string.Empty; // monthly, annual, lifetime
    
    public string Status { get; set; } = "active"; // active, cancelled, expired, past_due
    
    public DateTime CurrentPeriodStart { get; set; }
    
    public DateTime CurrentPeriodEnd { get; set; }
    
    public string? StripeSubscriptionId { get; set; }
    
    public string? StripeCustomerId { get; set; }
    
    public decimal Price { get; set; }
    
    public string Currency { get; set; } = "USD";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CancelledAt { get; set; }
}

