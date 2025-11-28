using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Vector.Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly SendGridClient _client;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
        var apiKey = _configuration["SendGrid:ApiKey"] ?? throw new InvalidOperationException("SendGrid API Key is not configured");
        _client = new SendGridClient(apiKey);
    }

    public async Task SendVerificationEmailAsync(string email, string token)
    {
        var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:3000";
        var verificationUrl = $"{frontendUrl}/verify-email?token={token}";
        var fromEmail = _configuration["SendGrid:FromEmail"] ?? "noreply@vector.com";
        var fromName = _configuration["SendGrid:FromName"] ?? "Vector";

        var msg = new SendGridMessage
        {
            From = new EmailAddress(fromEmail, fromName),
            Subject = "Verify your email address",
            HtmlContent = $@"
                <h1>Welcome to Vector!</h1>
                <p>Please verify your email address by clicking the link below:</p>
                <p><a href=""{verificationUrl}"">Verify Email</a></p>
                <p>If you didn't create an account, please ignore this email.</p>
            "
        };
        msg.AddTo(new EmailAddress(email));

        await _client.SendEmailAsync(msg);
    }

    public async Task SendPasswordResetEmailAsync(string email, string token)
    {
        var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:3000";
        var resetUrl = $"{frontendUrl}/reset-password?token={token}";
        var fromEmail = _configuration["SendGrid:FromEmail"] ?? "noreply@vector.com";
        var fromName = _configuration["SendGrid:FromName"] ?? "Vector";

        var msg = new SendGridMessage
        {
            From = new EmailAddress(fromEmail, fromName),
            Subject = "Reset your password",
            HtmlContent = $@"
                <h1>Password Reset Request</h1>
                <p>Click the link below to reset your password:</p>
                <p><a href=""{resetUrl}"">Reset Password</a></p>
                <p>This link will expire in 1 hour.</p>
                <p>If you didn't request a password reset, please ignore this email.</p>
            "
        };
        msg.AddTo(new EmailAddress(email));

        await _client.SendEmailAsync(msg);
    }

    public async Task SendWelcomeEmailAsync(string email, string name)
    {
        var fromEmail = _configuration["SendGrid:FromEmail"] ?? "noreply@vector.com";
        var fromName = _configuration["SendGrid:FromName"] ?? "Vector";

        var msg = new SendGridMessage
        {
            From = new EmailAddress(fromEmail, fromName),
            Subject = "Welcome to Vector!",
            HtmlContent = $@"
                <h1>Welcome to Vector, {name}!</h1>
                <p>Thank you for joining our platform. We're excited to help you prepare for your technical interviews.</p>
                <p>Get started by exploring our courses and scheduling your first mock interview.</p>
            "
        };
        msg.AddTo(new EmailAddress(email));

        await _client.SendEmailAsync(msg);
    }

    public async Task SendSubscriptionConfirmationEmailAsync(string email, string planName)
    {
        var fromEmail = _configuration["SendGrid:FromEmail"] ?? "noreply@vector.com";
        var fromName = _configuration["SendGrid:FromName"] ?? "Vector";

        var msg = new SendGridMessage
        {
            From = new EmailAddress(fromEmail, fromName),
            Subject = "Subscription Confirmed",
            HtmlContent = $@"
                <h1>Subscription Confirmed</h1>
                <p>Your {planName} subscription has been activated.</p>
                <p>Thank you for your subscription!</p>
            "
        };
        msg.AddTo(new EmailAddress(email));

        await _client.SendEmailAsync(msg);
    }
}

