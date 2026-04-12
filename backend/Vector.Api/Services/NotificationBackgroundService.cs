using Microsoft.EntityFrameworkCore;
using Vector.Api.Data;

namespace Vector.Api.Services;

/// <summary>
/// Background service that periodically sends email notifications:
/// - Mock Interview Reminders: 24 hours before a scheduled session (checked every 15 min)
/// - Weekly Progress: every Sunday at noon UTC
/// </summary>
public class NotificationBackgroundService : BackgroundService
{
    private readonly ILogger<NotificationBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public NotificationBackgroundService(
        ILogger<NotificationBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendInterviewRemindersAsync(stoppingToken);
                await SendWeeklyProgressAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in NotificationBackgroundService.");
            }

            // Check every 15 minutes
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task SendInterviewRemindersAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // Sessions starting in the next 24-hour window (within a 15-min polling tolerance)
        var windowStart = DateTime.UtcNow.AddHours(24).AddMinutes(-7);
        var windowEnd   = DateTime.UtcNow.AddHours(24).AddMinutes(7);

        var upcoming = await db.ScheduledInterviewSessions
            .Include(s => s.User)
            .Where(s => s.Status == "Scheduled"
                     && s.ScheduledStartAt >= windowStart
                     && s.ScheduledStartAt <= windowEnd)
            .ToListAsync(ct);

        foreach (var session in upcoming)
        {
            if (session.User is null || !session.User.NotifyInterviewReminders)
                continue;

            var subject = "Reminder: Mock Interview in 24 Hours";
            var body = $@"
<p>Hello {System.Net.WebUtility.HtmlEncode(session.User.FirstName ?? "there")},</p>
<p>This is a friendly reminder that your <strong>{System.Net.WebUtility.HtmlEncode(session.InterviewType)}</strong>
mock interview is scheduled for <strong>{session.ScheduledStartAt:f} UTC</strong>.</p>
<p>Log in to Vector to review your prep materials and make sure you're ready. Good luck!</p>";

            try
            {
                await emailService.SendEmailAsync(session.User.Email, subject, body);
                _logger.LogInformation("Interview reminder sent to {Email} for session {SessionId}",
                    session.User.Email, session.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send interview reminder to {Email}", session.User.Email);
            }
        }
    }

    private async Task SendWeeklyProgressAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        // Only run on Sundays between 12:00 and 12:14 UTC to avoid duplicate sends within the 15-min poll loop
        if (now.DayOfWeek != DayOfWeek.Sunday || now.Hour != 12 || now.Minute >= 15)
            return;

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var users = await db.Users
            .Where(u => u.NotifyWeeklyProgress)
            .ToListAsync(ct);

        foreach (var user in users)
        {
            var subject = "Your Weekly Progress Report on Vector";
            var body = $@"
<p>Hello {System.Net.WebUtility.HtmlEncode(user.FirstName ?? "there")},</p>
<p>Here is your weekly progress summary. Keep up the great work preparing for your interviews on Vector!</p>
<p>Head over to your <a href=""https://try-vector.com/profile"">dashboard</a> to see your detailed stats,
solved questions, and upcoming interviews.</p>";

            try
            {
                await emailService.SendEmailAsync(user.Email, subject, body);
                _logger.LogInformation("Weekly progress email sent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send weekly progress email to {Email}", user.Email);
            }
        }
    }
}
