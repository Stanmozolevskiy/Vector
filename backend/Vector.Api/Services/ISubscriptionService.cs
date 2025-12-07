using Vector.Api.DTOs.Subscription;

namespace Vector.Api.Services;

public interface ISubscriptionService
{
    Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync();
    Task<SubscriptionPlanDto?> GetPlanByIdAsync(string planId);
}

