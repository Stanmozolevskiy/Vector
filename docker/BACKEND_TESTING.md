# Backend Testing Guide

## ✅ Backend is Now Working!

The backend API is successfully running and accessible at `http://localhost:5000`.

## Quick Test

### Test Health Endpoint

```powershell
Invoke-WebRequest -Uri http://localhost:5000 -UseBasicParsing
```

**Expected Response:**
```json
{
  "message": "Vector API is running",
  "version": "1.0.0"
}
```

### Test Swagger UI

Open in browser: http://localhost:5000/swagger

This will show the Swagger UI with all available API endpoints.

### Test with curl (if available)

```powershell
curl http://localhost:5000
```

## What Was Fixed

### Issue 1: Port Configuration
- **Problem:** Backend was listening on port 8080 instead of port 80
- **Solution:** Added `ASPNETCORE_URLS=http://+:80` environment variable in `docker-compose.yml`
- **Result:** Backend now listens on port 80 inside container (mapped to 5000 on host)

### Issue 2: HTTPS Redirection
- **Problem:** `UseHttpsRedirection()` was enabled but we're running HTTP-only in Docker
- **Solution:** Made HTTPS redirection conditional - only enabled if HTTPS URLs are configured
- **Result:** No more connection errors

### Issue 3: Health Check Endpoint
- **Added:** Simple root endpoint (`/`) that returns API status
- **Purpose:** Easy way to verify the API is running

## Available Endpoints

### Health Check
- **GET** `/` - Returns API status

### Swagger Documentation
- **GET** `/swagger` - Swagger UI (Development only)
- **GET** `/swagger/v1/swagger.json` - OpenAPI JSON spec

### API Endpoints (To be implemented)
- `/api/auth/*` - Authentication endpoints
- `/api/users/*` - User management endpoints
- `/api/subscriptions/*` - Subscription endpoints
- `/api/stripe/*` - Stripe webhook endpoints

## Testing Commands

### PowerShell

```powershell
# Health check
Invoke-WebRequest -Uri http://localhost:5000 -UseBasicParsing

# Get response content
$response = Invoke-WebRequest -Uri http://localhost:5000 -UseBasicParsing
$response.Content

# Test Swagger
Start-Process http://localhost:5000/swagger
```

### Using curl (if installed)

```bash
# Health check
curl http://localhost:5000

# Pretty print JSON
curl http://localhost:5000 | ConvertFrom-Json
```

### Using Postman/Insomnia

1. Create a new request
2. Method: GET
3. URL: `http://localhost:5000`
4. Send request

## Viewing Logs

```powershell
cd docker
docker compose logs backend -f
```

## Restarting Backend

```powershell
cd docker
docker compose restart backend
```

## Rebuilding Backend (after code changes)

```powershell
cd docker
docker compose up -d --build backend
```

## Troubleshooting

### Issue: Connection Refused

**Check:**
1. Is Docker Desktop running?
2. Is the backend container running?
   ```powershell
   docker compose ps
   ```
3. Check backend logs:
   ```powershell
   docker compose logs backend
   ```

### Issue: 404 Not Found

**Check:**
- Are you using the correct endpoint?
- Check Swagger UI for available endpoints: http://localhost:5000/swagger

### Issue: 500 Internal Server Error

**Check:**
- Database connection (PostgreSQL should be running)
- Redis connection (Redis should be running)
- Check logs for specific error:
  ```powershell
  docker compose logs backend --tail 50
  ```

## Next Steps

1. ✅ Backend is running and accessible
2. ⏳ Implement API endpoints (Auth, Users, Subscriptions)
3. ⏳ Add authentication middleware
4. ⏳ Test API endpoints with Postman/curl
5. ⏳ Connect frontend to backend

## Configuration

### Environment Variables (docker-compose.yml)

- `ASPNETCORE_ENVIRONMENT=Development` - Development mode
- `ASPNETCORE_URLS=http://+:80` - Listen on port 80
- `ConnectionStrings__DefaultConnection` - PostgreSQL connection
- `ConnectionStrings__Redis` - Redis connection
- `Jwt__Secret` - JWT secret key
- `Jwt__Issuer` - JWT issuer
- `Jwt__Audience` - JWT audience
- `Frontend__Url` - Frontend URL for CORS

### Ports

- **Container:** Port 80 (internal)
- **Host:** Port 5000 (mapped from container port 80)

## Development Workflow

1. Make code changes in `backend/Vector.Api/`
2. Rebuild container:
   ```powershell
   cd docker
   docker compose up -d --build backend
   ```
3. Test endpoints
4. Check logs if issues occur

