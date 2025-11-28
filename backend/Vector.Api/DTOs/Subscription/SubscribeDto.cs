using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.Subscription;

public class SubscribeDto
{
    [Required]
    public string PlanId { get; set; } = string.Empty; // monthly, annual, lifetime

    [Required]
    public string PaymentMethodId { get; set; } = string.Empty; // Stripe payment method ID
}

