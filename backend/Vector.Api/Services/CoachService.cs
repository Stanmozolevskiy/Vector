using Microsoft.EntityFrameworkCore;
using Vector.Api.Data;
using Vector.Api.DTOs.Coach;
using Vector.Api.Models;

namespace Vector.Api.Services;

/// <summary>
/// Service for managing coach applications
/// </summary>
public class CoachService : ICoachService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<CoachService> _logger;

    public CoachService(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<CoachService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<CoachApplication> SubmitApplicationAsync(Guid userId, SubmitCoachApplicationDto dto)
    {
        // Check if user already has an application
        var existingApplication = await _context.CoachApplications
            .FirstOrDefaultAsync(a => a.UserId == userId);

        if (existingApplication != null)
        {
            throw new InvalidOperationException("You have already submitted a coach application.");
        }

        // Check if user is already a coach
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (user.Role == "coach")
        {
            throw new InvalidOperationException("You are already a coach.");
        }

        // Create new application
        var application = new CoachApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Motivation = dto.Motivation,
            Experience = dto.Experience,
            Specialization = dto.Specialization,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CoachApplications.Add(application);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Coach application submitted by user {UserId}", userId);

        // Send confirmation email to user
        try
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Coach Application Received",
                $"Hello {user.FirstName ?? "there"},\n\n" +
                "Thank you for your interest in becoming a coach on Vector!\n\n" +
                "We have received your application and our team will review it shortly. " +
                "You will receive an email notification once a decision has been made.\n\n" +
                "Best regards,\nThe Vector Team"
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send confirmation email for coach application {ApplicationId}", application.Id);
        }

        return application;
    }

    public async Task<CoachApplication?> GetApplicationByUserIdAsync(Guid userId)
    {
        return await _context.CoachApplications
            .Include(a => a.User)
            .Include(a => a.Reviewer)
            .FirstOrDefaultAsync(a => a.UserId == userId);
    }

    public async Task<CoachApplication?> GetApplicationByIdAsync(Guid applicationId)
    {
        return await _context.CoachApplications
            .Include(a => a.User)
            .Include(a => a.Reviewer)
            .FirstOrDefaultAsync(a => a.Id == applicationId);
    }

    public async Task<List<CoachApplication>> GetPendingApplicationsAsync()
    {
        return await _context.CoachApplications
            .Include(a => a.User)
            .Include(a => a.Reviewer)
            .Where(a => a.Status == "pending")
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<CoachApplication>> GetAllApplicationsAsync()
    {
        return await _context.CoachApplications
            .Include(a => a.User)
            .Include(a => a.Reviewer)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<CoachApplication> ReviewApplicationAsync(Guid applicationId, Guid adminId, ReviewCoachApplicationDto dto)
    {
        var application = await _context.CoachApplications
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application == null)
        {
            throw new KeyNotFoundException("Coach application not found.");
        }

        if (application.Status != "pending")
        {
            throw new InvalidOperationException($"Application has already been {application.Status}.");
        }

        // Update application status
        application.Status = dto.Status.ToLower();
        application.AdminNotes = dto.AdminNotes;
        application.ReviewedBy = adminId;
        application.ReviewedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        // If approved, update user role
        if (application.Status == "approved")
        {
            var user = application.User;
            if (user != null)
            {
                user.Role = "coach";
                user.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("User {UserId} promoted to coach role", user.Id);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Coach application {ApplicationId} {Status} by admin {AdminId}", 
            applicationId, application.Status, adminId);

        // Send email notification to applicant
        try
        {
            var user = application.User;
            if (user != null)
            {
                var subject = application.Status == "approved" 
                    ? "Congratulations! Your Coach Application Has Been Approved" 
                    : "Update on Your Coach Application";

                var body = application.Status == "approved"
                    ? $"Hello {user.FirstName ?? "there"},\n\n" +
                      "Great news! Your application to become a coach on Vector has been approved!\n\n" +
                      "You can now start helping students prepare for their interviews. " +
                      "Log in to your account to access your coach dashboard.\n\n" +
                      (string.IsNullOrEmpty(dto.AdminNotes) 
                          ? "" 
                          : $"Notes from our team:\n{dto.AdminNotes}\n\n") +
                      "Welcome to the Vector coaching team!\n\n" +
                      "Best regards,\nThe Vector Team"
                    : $"Hello {user.FirstName ?? "there"},\n\n" +
                      "Thank you for your interest in becoming a coach on Vector.\n\n" +
                      "After careful review, we are unable to approve your application at this time. " +
                      "We encourage you to gain more experience and reapply in the future.\n\n" +
                      (string.IsNullOrEmpty(dto.AdminNotes) 
                          ? "" 
                          : $"Feedback from our team:\n{dto.AdminNotes}\n\n") +
                      "Best regards,\nThe Vector Team";

                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send notification email for coach application {ApplicationId}", applicationId);
        }

        return application;
    }
}

