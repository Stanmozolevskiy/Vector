using Microsoft.Extensions.Configuration;
using Stripe;

namespace Vector.Api.Services;

public class StripeService : IStripeService
{
    private readonly IConfiguration _configuration;
    private readonly StripeClient _stripeClient;

    public StripeService(IConfiguration configuration)
    {
        _configuration = configuration;
        var secretKey = _configuration["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe Secret Key is not configured");
        _stripeClient = new StripeClient(secretKey);
    }

    public async Task<Customer> CreateCustomerAsync(string email, string name)
    {
        var service = new CustomerService(_stripeClient);
        var options = new CustomerCreateOptions
        {
            Email = email,
            Name = name
        };
        return await service.CreateAsync(options);
    }

    public async Task<Subscription> CreateSubscriptionAsync(string customerId, string priceId)
    {
        var service = new Stripe.SubscriptionService(_stripeClient);
        var options = new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items = new List<SubscriptionItemOptions>
            {
                new SubscriptionItemOptions { Price = priceId }
            }
        };
        return await service.CreateAsync(options);
    }

    public async Task<bool> CancelSubscriptionAsync(string subscriptionId)
    {
        try
        {
            var service = new Stripe.SubscriptionService(_stripeClient);
            var options = new SubscriptionCancelOptions();
            await service.CancelAsync(subscriptionId, options);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<Event> ConstructWebhookEventAsync(string json, string signature)
    {
        var webhookSecret = _configuration["Stripe:WebhookSecret"] ?? throw new InvalidOperationException("Stripe Webhook Secret is not configured");
        var stripeEvent = EventUtility.ParseEvent(json);
        // Validate signature - throws exception if invalid
        EventUtility.ValidateSignature(json, signature, webhookSecret);
        return Task.FromResult(stripeEvent);
    }

    public Task HandleWebhookEventAsync(Event stripeEvent)
    {
        // TODO: Implement webhook event handling
        // Handle different event types:
        // - customer.subscription.created
        // - customer.subscription.updated
        // - customer.subscription.deleted
        // - invoice.payment_succeeded
        // - invoice.payment_failed
        return Task.CompletedTask;
    }
}

