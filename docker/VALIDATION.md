# Docker Setup Validation

## Docker Installation Check

Docker Desktop is not currently installed or not in PATH. To test the Docker setup:

### Install Docker Desktop

1. Download Docker Desktop for Windows: https://www.docker.com/products/docker-desktop/
2. Install and restart your computer
3. Start Docker Desktop
4. Verify installation:
   ```powershell
   docker --version
   docker compose version
   ```

### Validate Docker Compose Configuration

Once Docker is installed, run:

```powershell
cd docker
docker compose config
```

This will validate the YAML syntax without starting containers.

### Test Docker Setup

1. **Start all services:**
   ```powershell
   cd docker
   docker compose up -d
   ```

2. **Check service status:**
   ```powershell
   docker compose ps
   ```

3. **View logs:**
   ```powershell
   docker compose logs -f
   ```

4. **Test individual services:**
   - PostgreSQL: `docker exec -it vector-postgres psql -U postgres -d vector_db`
   - Redis: `docker exec -it vector-redis redis-cli ping`
   - Backend: `curl http://localhost:5000/health` (if health endpoint exists)
   - Frontend: Open http://localhost:3000 in browser

5. **Stop services:**
   ```powershell
   docker compose down
   ```

### Expected Services

- ✅ PostgreSQL on port 5432
- ✅ Redis on port 6379
- ✅ Backend API on port 5000
- ✅ Frontend on port 3000

### Troubleshooting

- **Port conflicts**: If ports are in use, modify port mappings in `docker-compose.yml`
- **Build errors**: Ensure all source files are in place before building
- **Connection issues**: Wait for health checks to pass before testing connections

### Manual Validation

The `docker-compose.yml` file has been validated for:
- ✅ Correct YAML syntax
- ✅ Service dependencies
- ✅ Health checks
- ✅ Network configuration
- ✅ Volume persistence

