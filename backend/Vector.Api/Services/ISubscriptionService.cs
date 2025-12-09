using Vector.Api.DTOs.Subscription;

namespace Vector.Api.Services;

public interface ISubscriptionService
{
    Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync();
    Task<SubscriptionPlanDto?> GetPlanByIdAsync(string planId);
    Task<SubscriptionDto?> GetCurrentSubscriptionAsync(Guid userId);
    Task<SubscriptionDto> UpdateSubscriptionAsync(Guid userId, string planId);
    Task<bool> CancelSubscriptionAsync(Guid userId);
    Task<List<InvoiceDto>> GetInvoicesAsync(Guid userId);
    Task<SubscriptionDto> GetOrCreateFreeSubscriptionAsync(Guid userId);
}

