# Vector Backend API

**Version:** 2.0 (Stage 2)  
**Last Updated:** March 2026

## Overview

The Vector Backend API is built with ASP.NET Core 8.0 and provides RESTful endpoints for user management, authentication, LeetCode-style problem solving, peer mock interviews, analytics, and gamification. Uses PostgreSQL, Redis, S3-compatible storage (MinIO/R2), and Judge0 for code execution.

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

- ✅ .NET 8.0 Web API, EF Core, PostgreSQL, Redis
- ✅ JWT auth with refresh tokens, Swagger/OpenAPI
- ✅ Question bank, code execution (Judge0), solution submission
- ✅ Peer mock interviews (SignalR, WebRTC)
- ✅ Analytics, coins/leaderboard gamification
- ✅ S3-compatible storage (profile pics, videos)
- ✅ 364+ unit tests (100% passing)

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
- User management, coach application review
- Dashboard video upload (`POST /site-settings/dashboard-video/upload`)

### Stage 2 APIs
- **Questions** (`/api/question`) - CRUD, bookmarks, test cases, solutions
- **Code Execution** (`/api/codeexecution`) - Execute, validate (Judge0)
- **Solutions** (`/api/solutions`) - Submit, history, statistics
- **Peer Interviews** (`/api/peer-interviews`) - Match, sessions, feedback
- **Analytics** (`/api/analytics`) - Progress, streaks, category/difficulty
- **Coins** (`/api/coins`) - Leaderboard, transactions, achievements

See [docs/API_DOCUMENTATION.md](../docs/API_DOCUMENTATION.md) for full API reference.

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
- 364+ unit tests (services and controllers)
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

- [API Documentation](../docs/API_DOCUMENTATION.md) - Full API reference
- [Developer Guide](../docs/DEVELOPER_GUIDE.md) - Setup, code execution, uploads
- [User Guide](../docs/USER_GUIDE.md) - End-user guide

## Architecture

- **Pattern:** Repository pattern with services
- **Database:** PostgreSQL with Entity Framework Core
- **Caching:** Redis for sessions and frequently accessed data
- **File Storage:** AWS S3 for user uploads
- **Authentication:** JWT Bearer tokens
- **Email:** SendGrid for transactional emails

