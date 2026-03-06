# Testing Docker Locally - Step by Step Guide

## Prerequisites

1. ✅ Docker Desktop installed
2. ✅ Docker Desktop running (check system tray for whale icon)

## Quick Test

### Step 1: Start Docker Desktop

1. Open Docker Desktop application
2. Wait for it to fully start (whale icon should be steady, not animated)
3. Verify it's running:
   ```powershell
   docker ps
   ```
   Should return an empty list or running containers (no errors).

### Step 2: Navigate to Docker Directory

```powershell
cd c:\Users\stanm\source\repos\Vecotr\docker
```

### Step 3: Validate Configuration

```powershell
docker compose config
```

This validates the YAML syntax without starting containers. Should show no errors.

### Step 4: Pull Images (First Time)

```powershell
docker compose pull
```

This downloads the PostgreSQL and Redis images. The backend and frontend will be built from source.

### Step 5: Start Services

```powershell
docker compose up -d
```

The `-d` flag runs containers in detached mode (background).

**Expected Output:**
```
[+] Running 4/4
 ✔ Container vector-postgres    Started
 ✔ Container vector-redis       Started
 ✔ Container vector-backend     Started
 ✔ Container vector-frontend    Started
```

### Step 6: Check Service Status

```powershell
docker compose ps
```

**Expected Output:**
```
NAME                IMAGE                    STATUS          PORTS
vector-backend      docker-backend:latest    Up 2 minutes    0.0.0.0:5000->80/tcp
vector-frontend     docker-frontend:latest   Up 2 minutes    0.0.0.0:3000->80/tcp
vector-postgres     postgres:15              Up 2 minutes    0.0.0.0:5432->5432/tcp
vector-redis        redis:7-alpine           Up 2 minutes    0.0.0.0:6379->6379/tcp
```

All services should show "Up" status.

### Step 7: View Logs

```powershell
# View all logs
docker compose logs

# Follow logs in real-time
docker compose logs -f

# View logs for specific service
docker compose logs postgres
docker compose logs redis
docker compose logs backend
docker compose logs frontend
```

### Step 8: Test Individual Services

#### Test PostgreSQL

```powershell
# Connect to PostgreSQL
docker exec -it vector-postgres psql -U postgres -d vector_db

# Run a test query
SELECT version();

# Exit PostgreSQL
\q
```

Or test from outside:
```powershell
docker exec vector-postgres psql -U postgres -d vector_db -c "SELECT version();"
```

**Expected Output:**
```
PostgreSQL 15.x on x86_64-pc-linux-gnu...
```

#### Test Redis

```powershell
# Test Redis connection
docker exec vector-redis redis-cli ping
```

**Expected Output:**
```
PONG
```

Test Redis commands:
```powershell
docker exec -it vector-redis redis-cli
> SET test "Hello Docker"
> GET test
> EXIT
```

#### Test Backend API

```powershell
# Health check endpoint
Invoke-WebRequest -Uri http://localhost:5000 -UseBasicParsing

# Expected response:
# {"message":"Vector API is running","version":"1.0.0"}

# Swagger UI (open in browser)
Start-Process http://localhost:5000/swagger
```

**Note:** Backend should return a health check response. See `docker/BACKEND_TESTING.md` for detailed testing guide.

#### Test Frontend

1. Open browser: http://localhost:3000
2. Should see the React application (or a welcome page)

### Step 9: Check Service Health

```powershell
# Check container health
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

All containers should show "healthy" or "Up" status.

### Step 10: Test Service Communication

Services should be able to communicate using service names:

```powershell
# Test if backend can reach PostgreSQL
docker exec vector-backend ping -c 2 postgres

# Test if backend can reach Redis
docker exec vector-backend ping -c 2 redis
```

## Common Issues and Solutions

### Issue: "Cannot connect to Docker daemon"

**Solution:**
- Make sure Docker Desktop is running
- Restart Docker Desktop
- Check if Docker Desktop is fully started (not just starting)

### Issue: "Port already in use"

**Solution:**
- Check what's using the port:
  ```powershell
  netstat -ano | findstr :5432
  netstat -ano | findstr :6379
  netstat -ano | findstr :5000
  netstat -ano | findstr :3000
  ```
- Stop the conflicting service or change ports in `docker-compose.yml`

### Issue: "Backend build fails"

**Solution:**
- Make sure backend project exists: `backend/Vector.Api/`
- Check if .NET SDK is needed (for building, but Docker should handle this)
- View build logs: `docker compose logs backend`

### Issue: "Frontend build fails"

**Solution:**
- Make sure frontend project exists: `frontend/`
- Check if `frontend/package.json` exists
- View build logs: `docker compose logs frontend`

### Issue: "Services keep restarting"

**Solution:**
- Check logs: `docker compose logs`
- Check health checks: `docker ps`
- Verify environment variables in `docker-compose.yml`

## Stopping Services

### Stop All Services

```powershell
docker compose down
```

### Stop and Remove Volumes (Clean Slate)

```powershell
docker compose down -v
```

**Warning:** This will delete all database data!

### Stop Specific Service

```powershell
docker compose stop postgres
docker compose start postgres
```

## Rebuilding Services

If you make changes to code:

```powershell
# Rebuild and restart
docker compose up -d --build

# Rebuild specific service
docker compose up -d --build backend
```

## Viewing Resource Usage

```powershell
# Container stats
docker stats

# Disk usage
docker system df

# Clean up unused resources
docker system prune
```

## Complete Test Checklist

- [ ] Docker Desktop is running
- [ ] `docker compose config` validates successfully
- [ ] `docker compose up -d` starts all services
- [ ] All services show "Up" status
- [ ] PostgreSQL responds to queries
- [ ] Redis responds to ping
- [ ] Backend is accessible on port 5000
- [ ] Frontend is accessible on port 3000
- [ ] Services can communicate with each other
- [ ] Logs show no errors

## Next Steps After Testing

Once Docker is working locally:

1. ✅ Configuration is validated
2. ✅ Services can start and communicate
3. ⏳ Ready for development
4. ⏳ Can test database migrations
5. ⏳ Can test API endpoints
6. ⏳ Can test frontend-backend integration

## Useful Commands Reference

```powershell
# Start services
docker compose up -d

# Stop services
docker compose down

# View logs
docker compose logs -f

# Check status
docker compose ps

# Rebuild
docker compose up -d --build

# Execute command in container
docker exec -it vector-postgres psql -U postgres -d vector_db

# View container logs
docker logs vector-postgres

# Remove everything (including volumes)
docker compose down -v
```

