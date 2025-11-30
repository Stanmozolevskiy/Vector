# Backend Testing Guide

## Overview

This guide shows how to test the backend API separately and verify database communication.

---

## Current Backend Endpoints

### Available Endpoints

1. **Health Check:**
   - `GET /api/health` - Returns API status
   - `GET /health` - Alternative health endpoint

2. **API Root:**
   - `GET /api` - Returns API information and available endpoints

3. **Swagger UI:**
   - `GET /swagger` - API documentation (Development only)

---

## Testing Backend on AWS

### 1. Test Health Check

```powershell
# Test backend health endpoint
$backendUrl = "http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com"
Invoke-WebRequest -Uri "$backendUrl/api/health" -UseBasicParsing | ConvertFrom-Json
```

**Expected Response:**
```json
{
  "message": "Vector API is running",
  "version": "1.0.0",
  "environment": "Development",
  "timestamp": "2024-11-30T..."
}
```

### 2. Test API Root

```powershell
Invoke-WebRequest -Uri "$backendUrl/api" -UseBasicParsing | ConvertFrom-Json
```

### 3. Test Swagger (if available)

```powershell
# Open in browser
Start-Process "$backendUrl/swagger"
```

---

## Testing Database Connection

### Check CloudWatch Logs

```powershell
# View recent backend logs
aws logs tail /ecs/dev-vector --since 10m --region us-east-1 --filter-pattern "migration" --format short
```

**Look for:**
- `Checking for pending database migrations...`
- `Database migrations completed successfully.` or `Database is up to date.`
- Any database connection errors

### Check ECS Task Logs

```powershell
# Get running task ID
$taskId = aws ecs list-tasks --cluster dev-vector-cluster --service-name dev-vector-backend-service --region us-east-1 --query 'taskArns[0]' --output text

# Get logs for the task
aws logs get-log-events --log-group-name /ecs/dev-vector --log-stream-name "ecs/vector-backend/$taskId" --region us-east-1 --limit 50
```

---

## Testing Database Directly

### Connect to RDS Database

```powershell
# Get database endpoint
cd infrastructure/terraform
$dbEndpoint = terraform output -raw database_endpoint
$dbEndpoint = $dbEndpoint -replace ':5432', ''

# Connect using psql (if installed)
# Or use Docker
docker run -it --rm postgres:15 psql `
  -h $dbEndpoint `
  -U postgres `
  -d vector_db `
  -c "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';"
```

### Verify Tables Exist

```sql
-- List all tables
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public';

-- Expected tables:
-- - Users
-- - Subscriptions
-- - Payments
-- - EmailVerifications
-- - __EFMigrationsHistory
```

### Check Migration History

```sql
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
```

---

## Testing Backend Locally

### Prerequisites

1. **Start Local Services:**
   ```powershell
   cd docker
   docker-compose up -d postgres redis
   ```

2. **Update Connection String:**
   - Edit `backend/Vector.Api/appsettings.Development.json`
   - Ensure connection string points to `localhost`

### Run Backend Locally

```powershell
cd backend/Vector.Api
dotnet run
```

**Backend will be available at:**
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger: `https://localhost:5001/swagger`

### Test Local Endpoints

```powershell
# Health check
Invoke-WebRequest -Uri "http://localhost:5000/api/health" -UseBasicParsing

# API root
Invoke-WebRequest -Uri "http://localhost:5000/api" -UseBasicParsing

# Swagger
Start-Process "https://localhost:5001/swagger"
```

---

## Verify Database Communication

### Test Database Connection from Backend

1. **Check Application Logs:**
   - Look for migration messages
   - Check for connection errors

2. **Test with a Simple Query:**
   - Once controllers are implemented, test endpoints that query the database
   - For now, check that migrations ran successfully

### Check Database from Application

The backend automatically runs migrations on startup. Check logs for:
- `Checking for pending database migrations...`
- `Applying X pending migration(s)...`
- `Database migrations completed successfully.`

---

## Testing with Postman/Thunder Client

### Import Collection

Create a new collection with these requests:

1. **Health Check**
   - Method: `GET`
   - URL: `http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/api/health`

2. **API Root**
   - Method: `GET`
   - URL: `http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/api`

---

## Troubleshooting

### Backend Not Responding

1. **Check ECS Service:**
   ```powershell
   aws ecs describe-services --cluster dev-vector-cluster --services dev-vector-backend-service --region us-east-1
   ```

2. **Check Task Status:**
   ```powershell
   aws ecs list-tasks --cluster dev-vector-cluster --service-name dev-vector-backend-service --region us-east-1
   ```

3. **Check CloudWatch Logs:**
   ```powershell
   aws logs tail /ecs/dev-vector --since 30m --region us-east-1
   ```

### Database Connection Issues

1. **Check Security Groups:**
   - Verify ECS security group can access RDS security group
   - Check RDS security group allows port 5432 from ECS

2. **Check Connection String:**
   - Verify connection string in ECS task definition
   - Check password is correct

3. **Check RDS Status:**
   ```powershell
   aws rds describe-db-instances --db-instance-identifier dev-postgres --region us-east-1 --query 'DBInstances[0].DBInstanceStatus'
   ```

---

## Next Steps

Once controllers are implemented, you can test:
- User registration
- User login
- User profile retrieval
- Subscription management

**Current Status:** Backend is deployed and running. Controllers need to be implemented to test full functionality.

---

**Backend URL:** `http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/api`

