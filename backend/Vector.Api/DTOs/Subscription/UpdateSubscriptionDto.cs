using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.Subscription;

public class UpdateSubscriptionDto
{
    [Required]
    public string PlanId { get; set; } = string.Empty; // monthly, annual, lifetime
}

