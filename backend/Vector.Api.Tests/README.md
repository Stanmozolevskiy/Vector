# Vector API Unit Tests

## Overview
This project contains unit tests for the Vector API backend. All tests must pass before deploying code.

## Running Tests

### Prerequisites
- .NET 8.0 SDK
- Ensure the backend API is **NOT running** (it locks files and prevents building)

### Run All Tests
```powershell
cd backend
dotnet test Vector.Api.Tests/Vector.Api.Tests.csproj
```

### Run Tests with Coverage
```powershell
dotnet test Vector.Api.Tests/Vector.Api.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Run Specific Test Class
```powershell
dotnet test --filter "FullyQualifiedName~AuthControllerTests"
```

## Test Structure

### Controllers
- `AuthControllerTests.cs` - Tests for authentication endpoints
  - Register
  - Login
  - Verify Email
  - Forgot Password
  - Reset Password
- `UserControllerTests.cs` - Tests for user management endpoints
  - Get Current User
  - CreateSession (8 tests)
  - GetMySessions (4 tests)
  - GetSession (5 tests)
  - UpdateSessionStatus (4 tests)
  - CancelSession (4 tests)
  - FindMatch (3 tests)
  - UpdateMatchPreferences (2 tests)
  - GetMatchPreferences (3 tests)

### Services
- `AuthServiceTests.cs` - Tests for authentication business logic
  - User Registration
  - User Login
  - Email Verification
  - CreateSessionAsync (20 tests)
    - Valid data creation
    - Automatic question assignment by interview level (Beginner/Intermediate/Advanced)
    - Invalid interview level handling
    - No questions available for level
    - Null/optional parameter handling
    - Default time and duration
    - Last match date updates
    - Case-insensitive level matching
  - GetSessionByIdAsync (2 tests)
  - GetUserSessionsAsync (4 tests)
  - UpdateSessionStatusAsync (3 tests)
  - CancelSessionAsync (8 tests)
  - FindMatchAsync (8 tests)
  - UpdateMatchPreferencesAsync (3 tests)
  - GetMatchPreferencesAsync (2 tests)

## Important Notes

1. **Always run tests before deploying** - This is a requirement
2. **Stop the backend API** before running tests to avoid file locking issues
3. Tests use **In-Memory Database** for isolation
4. Tests use **Moq** for mocking dependencies

## Adding New Tests

When adding new API endpoints or services:
1. Create corresponding test files
2. Test all success and failure scenarios
3. Ensure tests are isolated and don't depend on external services
4. Run tests before committing code

## CI/CD Integration

Tests are automatically run in CI/CD pipeline. If tests fail, deployment is blocked.

