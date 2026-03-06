# Database Migrations

This folder contains Entity Framework Core migrations for the Vector database schema.

## Creating Migrations

To create a new migration:

```bash
cd backend/Vector.Api
dotnet ef migrations add MigrationName --output-dir Data/Migrations
```

## Applying Migrations

### Development (Local)
Migrations are automatically applied when the application starts in Development mode (see `Program.cs`).

### Manual Application
To manually apply migrations:

```bash
cd backend/Vector.Api
dotnet ef database update
```

### Production
For production environments, migrations should be run as part of the deployment process or manually:

```bash
dotnet ef database update --connection "YourProductionConnectionString"
```

## Migration Files

- **InitialCreate**: Initial database schema with Users, Subscriptions, Payments, and EmailVerifications tables

## Notes

- Always review migration files before applying them
- Test migrations in development/staging before production
- Keep migrations small and focused
- Never edit existing migration files after they've been applied to production

