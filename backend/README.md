# Vector Backend API

**Version:** 1.0  
**Last Updated:** December 6, 2025

## Overview

The Vector Backend API is built with ASP.NET Core 8.0 and provides RESTful endpoints for user management, authentication, subscriptions, and more. The API uses PostgreSQL for data persistence, Redis for caching, and AWS S3 for file storage.

## Quick Start

```bash
# Restore dependencies
dotnet restore

# Run database migrations
cd Vector.Api
dotnet ef database update

# Run the application
dotnet run
```

API will be available at `http://localhost:5000`  
Swagger UI: `http://localhost:5000/swagger`

## Project Structure

```
backend/
├── Vector.sln                    # Solution file
└── Vector.Api/                   # Main API project
    ├── Controllers/              # API Controllers
    ├── Services/                # Business logic services
    ├── Models/                  # Entity models
    ├── Data/                    # Database context
    ├── DTOs/                    # Data Transfer Objects
    │   ├── Auth/
    │   ├── User/
    │   └── Subscription/
    ├── Middleware/              # Custom middleware
    ├── Helpers/                 # Helper classes
    ├── Program.cs               # Application entry point
    └── appsettings.json         # Configuration
```

## Current Status ✅

The backend project includes:

- ✅ .NET 8.0 Web API project
- ✅ Entity Framework Core with PostgreSQL
- ✅ JWT Authentication with refresh token rotation
- ✅ Redis connection and caching
- ✅ Swagger/OpenAPI documentation with XML comments
- ✅ CORS configuration
- ✅ Error handling middleware
- ✅ Database models (User, Subscription, CoachApplication, EmailVerification, PasswordReset, RefreshToken)
- ✅ ApplicationDbContext configured
- ✅ 151 unit and integration tests (100% passing)
- ✅ Optimized database queries with caching
- ✅ Comprehensive error handling with ApiErrorResponse
- ✅ Image upload validation and optimization

## API Endpoints

### Authentication (`/api/auth`)
- `POST /register` - Register new user
- `GET /verify-email` - Verify email address
- `POST /login` - Authenticate user
- `POST /forgot-password` - Request password reset
- `POST /reset-password` - Reset password
- `POST /logout` - Logout user

### User Management (`/api/users`) - Requires Auth
- `GET /me` - Get current user
- `PUT /me` - Update profile
- `PUT /me/password` - Change password
- `POST /me/profile-picture` - Upload profile picture
- `DELETE /me/profile-picture` - Delete profile picture
- `DELETE /me` - Delete account

### Subscriptions (`/api/subscriptions`)
- `GET /plans` - Get all plans (public)
- `GET /plans/{planId}` - Get specific plan (public)
- `GET /me` - Get current subscription (requires auth)
- `PUT /update` - Update subscription (requires auth)
- `PUT /cancel` - Cancel subscription (requires auth)
- `GET /invoices` - Get billing history (requires auth)

### Coach Applications (`/api/coach`) - Requires Auth
- `POST /apply` - Submit coach application
- `GET /my-application` - Get application status

### Admin (`/api/admin`) - Requires Admin Role
- `GET /users` - Get all users
- `PUT /users/{userId}/role` - Update user role
- `DELETE /users/{userId}` - Delete user
- `GET /coach-applications/pending` - Get pending applications
- `POST /coach-applications/{id}/review` - Review application

See [API_DOCUMENTATION.md](../API_DOCUMENTATION.md) for detailed API documentation.

## Running the Application

```bash
cd backend/Vector.Api
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## Dependencies Installed

- Microsoft.EntityFrameworkCore (8.0.0)
- Microsoft.EntityFrameworkCore.Design (8.0.0)
- Npgsql.EntityFrameworkCore.PostgreSQL (8.0.0)
- Microsoft.AspNetCore.Authentication.JwtBearer (8.0.0)
- BCrypt.Net-Next (4.0.3)
- Stripe.net (50.0.0)
- SendGrid (9.29.3)
- AWSSDK.S3 (4.0.13.1)
- StackExchange.Redis (2.10.1)
- FluentValidation.AspNetCore (11.3.1)
- Swashbuckle.AspNetCore (10.0.1)

## Testing

```bash
# Run all tests
cd Vector.Api.Tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

**Test Coverage:**
- 134 unit tests (services and controllers)
- 17 integration tests (API endpoints)
- 100% passing

## Configuration

See [ENVIRONMENT_VARIABLES.md](../ENVIRONMENT_VARIABLES.md) for complete environment variable documentation.

## Security

- ✅ BCrypt password hashing
- ✅ JWT authentication with refresh tokens
- ✅ SQL injection protection (EF Core)
- ✅ Input validation
- ✅ CORS configuration
- ⚠️ Rate limiting (recommended for production)

See [SECURITY_AUDIT.md](../SECURITY_AUDIT.md) for detailed security audit.

## Documentation

- [API Documentation](../API_DOCUMENTATION.md) - Complete API reference
- [Deployment Guide](../DEPLOYMENT_GUIDE.md) - Deployment procedures
- [Environment Variables](../ENVIRONMENT_VARIABLES.md) - Configuration guide
- [Security Audit](../SECURITY_AUDIT.md) - Security assessment

## Architecture

- **Pattern:** Repository pattern with services
- **Database:** PostgreSQL with Entity Framework Core
- **Caching:** Redis for sessions and frequently accessed data
- **File Storage:** AWS S3 for user uploads
- **Authentication:** JWT Bearer tokens
- **Email:** SendGrid for transactional emails

