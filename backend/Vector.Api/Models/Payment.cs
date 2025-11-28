namespace Vector.Api.Models;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    public Guid? SubscriptionId { get; set; }
    
    public Subscription? Subscription { get; set; }
    
    public decimal Amount { get; set; }
    
    public string Currency { get; set; } = "USD";
    
    public string PaymentType { get; set; } = string.Empty; // subscription, service, one_time
    
    public string Status { get; set; } = "pending"; // pending, completed, failed, refunded
    
    public string? StripePaymentIntentId { get; set; }
    
    public string? StripeChargeId { get; set; }
    
    public string? TransactionId { get; set; }
    
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

