using Microsoft.EntityFrameworkCore;
using Vector.Api.Constants;
using Vector.Api.Models;

namespace Vector.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Run migrations
        if ((await context.Database.GetPendingMigrationsAsync()).Any())
        {
            await context.Database.MigrateAsync();
        }

        // Seed initial data if needed
        if (!await context.Users.AnyAsync())
        {
            // TODO: Add seed data if needed
            // Example: Create default admin user
        }

        // Note: Achievement seeding is now called from DbSeeder.SeedDatabase
        // to ensure it runs with proper logging during application startup
    }

    public static async Task SeedAchievementDefinitionsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.AchievementDefinitions.AnyAsync())
        {
            logger.LogInformation("Achievement definitions already seeded, skipping...");
            return; // Already seeded
        }

        logger.LogInformation("Seeding achievement definitions...");

        var achievements = new[]
        {
            // Interview Activities
            new AchievementDefinition
            {
                ActivityType = AchievementTypes.InterviewCompleted,
                DisplayName = "You completed a mock interview",
                Description = "Complete any type of scheduled mock interview (peer/expert)",
                CoinsAwarded = 10,
                Icon = "🪙",
                IsActive = true
            },
            new AchievementDefinition
            {
                ActivityType = AchievementTypes.GreatMockInterviewPartner,
                DisplayName = "You are a great mock interview partner",
                Description = "Earn additional karma when your mock interview partner rates you highly (5 stars)",
                CoinsAwarded = 15,
                Icon = "🌟",
                IsActive = true
            },

            // Question Activities
            new AchievementDefinition
            {
                ActivityType = AchievementTypes.QuestionPublished,
                DisplayName = "Your interview question is published",
                Description = "Create and publish a high-quality interview question",
                CoinsAwarded = 25,
                Icon = "📝",
                IsActive = true
            },
            new AchievementDefinition
            {
                ActivityType = AchievementTypes.QuestionUpvoted,
                DisplayName = "Your interview question is upvoted",
                Description = "Receive an upvote on your interview question",
                CoinsAwarded = 5,
                Icon = "👍",
                IsActive = true
            },
            new AchievementDefinition
            {
                ActivityType = AchievementTypes.QuestionInAnotherInterview,
                DisplayName = "Your question appears in another interview",
                Description = "Your question is selected for use in another mock interview",
                CoinsAwarded = 5,
                Icon = "🔄",
                IsActive = true
            },

            // Engagement
            new AchievementDefinition
            {
                ActivityType = AchievementTypes.LessonCompleted,
                DisplayName = "You complete a lesson",
                Description = "Complete a learning module or lesson",
                CoinsAwarded = 1,
                Icon = "📚",
                IsActive = true
            },
            new AchievementDefinition
            {
                ActivityType = AchievementTypes.CommentUpvoted,
                DisplayName = "Your comment is upvoted",
                Description = "Receive an upvote on your comment",
                CoinsAwarded = 5,
                Icon = "💬",
                IsActive = true
            },
            new AchievementDefinition
            {
                ActivityType = AchievementTypes.ProfileCompleted,
                DisplayName = "You fill out your profile",
                Description = "Complete all required profile information",
                CoinsAwarded = 10,
                Icon = "👤",
                IsActive = true,
                MaxOccurrences = 1 // One-time reward
            },

            // Referral
            new AchievementDefinition
            {
                ActivityType = AchievementTypes.ReferralSuccess,
                DisplayName = "You refer someone to Exponent",
                Description = "Successfully refer a new user to the platform",
                CoinsAwarded = 100,
                Icon = "🎁",
                IsActive = true
            },

            // Feedback
            new AchievementDefinition
            {
                ActivityType = AchievementTypes.FeedbackSubmitted,
                DisplayName = "You submit feedback to Vector",
                Description = "Provide valuable feedback to improve the platform",
                CoinsAwarded = 10,
                Icon = "💡",
                IsActive = true
            }
        };

        context.AchievementDefinitions.AddRange(achievements);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Successfully seeded {Count} achievement definitions", achievements.Length);
    }
}

