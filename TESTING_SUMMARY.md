# Testing Summary

## ✅ Current Status

Both frontend and backend have been deployed successfully to AWS Dev environment.

---

## Quick Test Commands

### Backend Tests

```powershell
# 1. Health Check
$backendUrl = "http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com"
Invoke-WebRequest -Uri "$backendUrl/api/health" -UseBasicParsing | ConvertFrom-Json

# 2. API Root
Invoke-WebRequest -Uri "$backendUrl/api" -UseBasicParsing | ConvertFrom-Json

# 3. Check Database Connection (via logs)
aws logs tail /ecs/dev-vector --since 10m --region us-east-1 --filter-pattern "migration" --format short
```

### Frontend Tests

```powershell
# 1. Open Frontend
$frontendUrl = "http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com"
Start-Process $frontendUrl

# 2. Health Check
Invoke-WebRequest -Uri "$frontendUrl/health" -UseBasicParsing

# 3. Test in Browser
# - Open Developer Tools (F12)
# - Go to Network tab
# - Look for API calls to /api/*
```

### Database Tests

```powershell
# Get database endpoint
cd infrastructure/terraform
$dbEndpoint = terraform output -raw database_endpoint
$dbEndpoint = $dbEndpoint -replace ':5432', ''

# Connect via Docker
docker run -it --rm postgres:15 psql -h $dbEndpoint -U postgres -d vector_db
```

---

## Testing Backend Separately

### Verify Backend is Running

1. **Check ECS Service:**
   ```powershell
   aws ecs describe-services --cluster dev-vector-cluster --services dev-vector-backend-service --region us-east-1
   ```

2. **Test Health Endpoint:**
   ```powershell
   Invoke-WebRequest -Uri "http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/api/health" -UseBasicParsing
   ```

### Verify Database Communication

1. **Check Migration Logs:**
   ```powershell
   aws logs tail /ecs/dev-vector --since 10m --region us-east-1 --filter-pattern "migration" --format short
   ```

2. **Look for:**
   - `Checking for pending database migrations...`
   - `Database migrations completed successfully.`
   - `Database is up to date.`

3. **Check for Errors:**
   ```powershell
   aws logs tail /ecs/dev-vector --since 10m --region us-east-1 --filter-pattern "error" --format short
   ```

### Test Database Directly

```powershell
# Connect to database
cd infrastructure/terraform
$dbEndpoint = terraform output -raw database_endpoint
$dbEndpoint = $dbEndpoint -replace ':5432', ''

# Using Docker
docker run -it --rm postgres:15 psql `
  -h $dbEndpoint `
  -U postgres `
  -d vector_db `
  -c "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';"
```

**Expected Tables:**
- `Users`
- `Subscriptions`
- `Payments`
- `EmailVerifications`
- `__EFMigrationsHistory`

---

## Testing Frontend Separately

### Verify Frontend is Running

1. **Check ECS Service:**
   ```powershell
   aws ecs describe-services --cluster dev-vector-cluster --services dev-vector-frontend-service --region us-east-1
   ```

2. **Test Frontend:**
   ```powershell
   Start-Process "http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/"
   ```

### Verify Frontend-Backend Communication

1. **Open Browser Developer Tools:**
   - Press `F12`
   - Go to **Network** tab
   - Filter by `XHR` or `Fetch`

2. **Check API Calls:**
   - Frontend should make requests to: `http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/api/*`
   - Verify requests are being made
   - Check response status codes

3. **Check Console:**
   - Look for JavaScript errors
   - Verify API URL is correct
   - Check for CORS errors

### Test Data Flow (When Controllers Implemented)

**Full Flow:** Database → Backend → Frontend

1. **Register User:**
   - Frontend: `POST /api/auth/register`
   - Backend: Stores user in database
   - Verify: Check database for new user

2. **Login:**
   - Frontend: `POST /api/auth/login`
   - Backend: Validates credentials from database
   - Returns: JWT token
   - Frontend: Stores token in localStorage

3. **Get User Data:**
   - Frontend: `GET /api/users/me` (with token)
   - Backend: Queries database for user
   - Returns: User data
   - Frontend: Displays user information

---

## Staging Deployment

### What It Takes

1. **Infrastructure:**
   - Deploy Terraform with `environment=staging`
   - Separate VPC, RDS, Redis, ECS cluster
   - Estimated cost: ~$150/month

2. **GitHub Secrets:**
   - `STAGING_DB_CONNECTION_STRING`
   - `STAGING_API_URL`

3. **Deploy:**
   - Push to `staging` branch
   - CI/CD automatically deploys

**See:** `STAGING_DEPLOYMENT_GUIDE.md` for complete instructions.

---

## Current Limitations

### Backend
- ✅ Health endpoints working
- ✅ Database migrations running
- ⏳ Controllers not yet implemented (no business logic endpoints)

### Frontend
- ✅ Frontend loads correctly
- ✅ API calls configured
- ⏳ Waiting for backend controllers to be implemented

---

## Next Steps for Full Testing

1. **Implement Controllers:**
   - `AuthController` - Register, login, logout
   - `UserController` - Get user, update user

2. **Test Full Flow:**
   - Register → Database stores user
   - Login → Backend validates → Returns token
   - Get user → Backend queries database → Frontend displays

---

**For detailed testing procedures, see:**
- `BACKEND_TESTING_GUIDE.md`
- `FRONTEND_TESTING_GUIDE.md`
- `STAGING_DEPLOYMENT_GUIDE.md`

