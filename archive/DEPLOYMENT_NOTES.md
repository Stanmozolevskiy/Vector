# Deployment Notes

## Unit Tests Requirement

**⚠️ CRITICAL: All unit tests must pass before deploying code.**

### Running Tests
```powershell
# Stop the backend API first (it locks files)
cd backend
dotnet test Vector.Api.Tests/Vector.Api.Tests.csproj
```

### Test Coverage
- ✅ AuthController tests (Register, Login, Verify Email, Forgot Password, Reset Password)
- ✅ UserController tests (Get Current User)
- ✅ AuthService tests (Registration, Login, Email Verification)

## Email Validation Stability

Email validation functionality has been stabilized with the following measures:

1. **SendGrid Configuration**
   - `.env` file created in `docker/` directory
   - Environment variables properly configured
   - Enhanced logging for debugging

2. **Configuration Files**
   - `docker/docker-compose.yml` - Fixed indentation issues
   - `backend/Vector.Api/Services/EmailService.cs` - Enhanced error handling and logging

3. **Troubleshooting Guide**
   - See `docker/EMAIL_TROUBLESHOOTING.md` for detailed troubleshooting steps

## Recent Fixes

### 1. Login After Email Verification
- **Issue**: Users couldn't login after verifying email (404 on `/api/users/me`)
- **Fix**: Implemented `GET /api/users/me` endpoint in `UserController`
- **Status**: ✅ Fixed

### 2. Auth Pages Alignment
- **Issue**: Forgot password and success pages had alignment issues
- **Fix**: Updated CSS and inline styles for proper centering
- **Status**: ✅ Fixed

### 3. Unit Tests
- **Created**: Comprehensive unit test suite
- **Location**: `backend/Vector.Api.Tests/`
- **Status**: ✅ Created (must run before deployment)

## Deployment Checklist

Before deploying to Docker:

1. [ ] Run unit tests: `dotnet test`
2. [ ] Ensure all tests pass
3. [ ] Check email configuration (SendGrid API key set)
4. [ ] Verify CORS settings
5. [ ] Check environment variables
6. [ ] Build Docker images
7. [ ] Deploy containers
8. [ ] Test registration flow
9. [ ] Test login flow
10. [ ] Test email verification

## Email Configuration

### Local Development
Set environment variables in `docker/.env`:
```
SENDGRID_API_KEY=your_api_key_here
SENDGRID_FROM_EMAIL=your_email@example.com
SENDGRID_FROM_NAME=Vector
```

### Docker Compose
Environment variables are automatically loaded from `.env` file.

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login user
- `GET /api/auth/verify-email?token=xxx` - Verify email
- `POST /api/auth/forgot-password` - Request password reset
- `POST /api/auth/reset-password` - Reset password

### User Management
- `GET /api/users/me` - Get current user (requires authentication)

## Testing

### Manual Testing
1. Register a new account
2. Check email for verification link
3. Click verification link
4. Login with credentials
5. Verify user profile loads

### Automated Testing
```powershell
cd backend
dotnet test Vector.Api.Tests/Vector.Api.Tests.csproj --verbosity normal
```

