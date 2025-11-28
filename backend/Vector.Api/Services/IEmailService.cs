namespace Vector.Api.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string token);
    Task SendPasswordResetEmailAsync(string email, string token);
    Task SendWelcomeEmailAsync(string email, string name);
    Task SendSubscriptionConfirmationEmailAsync(string email, string planName);
}

