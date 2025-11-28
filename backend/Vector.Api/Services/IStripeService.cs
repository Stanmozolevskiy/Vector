using Stripe;

namespace Vector.Api.Services;

public interface IStripeService
{
    Task<Customer> CreateCustomerAsync(string email, string name);
    Task<Subscription> CreateSubscriptionAsync(string customerId, string priceId);
    Task<bool> CancelSubscriptionAsync(string subscriptionId);
    Task<Event> ConstructWebhookEventAsync(string json, string signature);
    Task HandleWebhookEventAsync(Event stripeEvent);
}

