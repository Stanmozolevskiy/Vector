using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Vector.Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly SendGridClient? _client;
    private readonly bool _isEnabled;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var apiKey = _configuration["SendGrid:ApiKey"];
        if (string.IsNullOrEmpty(apiKey) || apiKey == "your_sendgrid_api_key")
        {
            _isEnabled = false;
            _logger.LogWarning("SendGrid API Key is not configured. Email sending is disabled. Emails will be logged to console instead.");
        }
        else
        {
            _isEnabled = true;
            _client = new SendGridClient(apiKey);
            _logger.LogInformation("SendGrid email service initialized successfully.");
        }
    }

    public async Task SendVerificationEmailAsync(string email, string token)
    {
        var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:3000";
        var verificationUrl = $"{frontendUrl}/verify-email?token={token}";
        var fromEmail = _configuration["SendGrid:FromEmail"] ?? "noreply@vector.com";
        var fromName = _configuration["SendGrid:FromName"] ?? "Vector";

        if (!_isEnabled || _client == null)
        {
            // Log email to console in development
            _logger.LogWarning("=== EMAIL VERIFICATION (SendGrid not configured) ===");
            _logger.LogWarning("To: {Email}", email);
            _logger.LogWarning("Subject: Verify your email address");
            _logger.LogWarning("Verification URL: {Url}", verificationUrl);
            _logger.LogWarning("Token: {Token}", token);
            _logger.LogWarning("===================================================");
            return;
        }

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

        try
        {
            await _client.SendEmailAsync(msg);
            _logger.LogInformation("Verification email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", email);
            // Don't throw - email sending failure shouldn't break registration
        }
    }

    public async Task SendPasswordResetEmailAsync(string email, string token)
    {
        var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:3000";
        var resetUrl = $"{frontendUrl}/reset-password?token={token}";
        var fromEmail = _configuration["SendGrid:FromEmail"] ?? "noreply@vector.com";
        var fromName = _configuration["SendGrid:FromName"] ?? "Vector";

        if (!_isEnabled || _client == null)
        {
            _logger.LogWarning("=== PASSWORD RESET EMAIL (SendGrid not configured) ===");
            _logger.LogWarning("To: {Email}", email);
            _logger.LogWarning("Subject: Reset your password");
            _logger.LogWarning("Reset URL: {Url}", resetUrl);
            _logger.LogWarning("Token: {Token}", token);
            _logger.LogWarning("=====================================================");
            return;
        }

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

        try
        {
            await _client.SendEmailAsync(msg);
            _logger.LogInformation("Password reset email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
        }
    }

    public async Task SendWelcomeEmailAsync(string email, string name)
    {
        if (!_isEnabled || _client == null)
        {
            _logger.LogWarning("=== WELCOME EMAIL (SendGrid not configured) ===");
            _logger.LogWarning("To: {Email}", email);
            _logger.LogWarning("Subject: Welcome to Vector!");
            _logger.LogWarning("Name: {Name}", name);
            _logger.LogWarning("==============================================");
            return;
        }

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

        try
        {
            await _client.SendEmailAsync(msg);
            _logger.LogInformation("Welcome email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", email);
        }
    }

    public async Task SendSubscriptionConfirmationEmailAsync(string email, string planName)
    {
        if (!_isEnabled || _client == null)
        {
            _logger.LogWarning("=== SUBSCRIPTION CONFIRMATION EMAIL (SendGrid not configured) ===");
            _logger.LogWarning("To: {Email}", email);
            _logger.LogWarning("Subject: Subscription Confirmed");
            _logger.LogWarning("Plan: {PlanName}", planName);
            _logger.LogWarning("================================================================");
            return;
        }

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

        try
        {
            await _client.SendEmailAsync(msg);
            _logger.LogInformation("Subscription confirmation email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription confirmation email to {Email}", email);
        }
    }
}

