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
    /// Seeds all initial data
    /// </summary>
    public static async Task SeedDatabase(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Starting database seeding...");

        await SeedAdminUser(context, logger);

        logger.LogInformation("Database seeding completed successfully");
    }
}

