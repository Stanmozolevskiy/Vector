using Microsoft.EntityFrameworkCore;
using Vector.Api.Helpers;
using Vector.Api.Models;

namespace Vector.Api.Data;

/// <summary>
/// Seeds the database with initial data (admin user, etc.)
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Seeds the database with default admin user if it doesn't exist
    /// </summary>
    public static async Task SeedAdminUser(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // Check if any admin users exist
            var adminExists = await context.Users.AnyAsync(u => u.Role == "admin");

            if (adminExists)
            {
                logger.LogInformation("Admin user already exists. Skipping seed.");
                return;
            }

            // Create default admin user
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@vector.com",
                PasswordHash = PasswordHasher.HashPassword("Admin@123"), // Default password
                FirstName = "System",
                LastName = "Administrator",
                Role = "admin",
                EmailVerified = true, // Admin is pre-verified
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            logger.LogWarning(
                "⚠️ DEFAULT ADMIN USER CREATED ⚠️\n" +
                "Email: admin@vector.com\n" +
                "Password: Admin@123\n" +
                "⚠️ CHANGE THIS PASSWORD IMMEDIATELY IN PRODUCTION! ⚠️"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed admin user");
            throw;
        }
    }

    /// <summary>
    /// Seeds mock interview videos
    /// </summary>
    public static async Task SeedMockInterviews(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // Check if mock interviews exist
            var interviewsExist = await context.MockInterviews.AnyAsync();

            if (interviewsExist)
            {
                logger.LogInformation("Mock interviews already exist. Skipping seed.");
                return;
            }

            // Add initial mock interview video
            var mockInterview = new MockInterview
            {
                Id = Guid.NewGuid(),
                Title = "What Is Exponent? - Introduction to Mock Interviews",
                Description = "An introduction to Exponent's mock interview platform and how to prepare for technical interviews effectively.",
                VideoUrl = "https://dev-vector-user-uploads.s3.us-east-1.amazonaws.com/videos/mock-interviews/what-is-exponent.mp4",
                ThumbnailUrl = "",
                DurationSeconds = 180, // 3 minutes (approximate)
                Category = "Introduction",
                Difficulty = "Easy",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.MockInterviews.Add(mockInterview);
            await context.SaveChangesAsync();

            logger.LogInformation("Mock interview video seeded successfully: {Title}", mockInterview.Title);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed mock interviews");
            throw;
        }
    }

    /// <summary>
    /// Seeds all initial data
    /// </summary>
    public static async Task SeedDatabase(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Starting database seeding...");

        await SeedAdminUser(context, logger);
        await SeedMockInterviews(context, logger);

        logger.LogInformation("Database seeding completed successfully");
    }
}

