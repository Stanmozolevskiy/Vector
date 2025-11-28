using Microsoft.EntityFrameworkCore;
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
    }
}

