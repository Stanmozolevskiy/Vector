# Deploy Backend to Local Docker
# Usage: .\deploy-backend.ps1

Write-Host "Deploying Backend to Local Docker..." -ForegroundColor Cyan

# Navigate to docker directory
$dockerDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $dockerDir

# Rebuild backend without cache
Write-Host "Rebuilding backend container (no cache)..." -ForegroundColor Yellow
docker-compose build --no-cache backend

if ($LASTEXITCODE -ne 0) {
    Write-Host "Backend build failed!" -ForegroundColor Red
    exit 1
}

# Restart backend container
Write-Host "Restarting backend container..." -ForegroundColor Yellow
docker-compose up -d backend

if ($LASTEXITCODE -ne 0) {
    Write-Host "Backend deployment failed!" -ForegroundColor Red
    exit 1
}

# Wait for container to be ready
Start-Sleep -Seconds 5

# Verify container is running
Write-Host "Verifying container status..." -ForegroundColor Yellow
$status = docker ps --filter "name=vector-backend" --format "{{.Status}}"

if ($status) {
    Write-Host "Backend deployed successfully!" -ForegroundColor Green
    Write-Host "Container Status: $status" -ForegroundColor Gray
    Write-Host "API URL: http://localhost:5000/api" -ForegroundColor Cyan
    
    # Show recent logs
    Write-Host ""
    Write-Host "Recent backend logs:" -ForegroundColor Yellow
    docker-compose logs backend --tail 20
} else {
    Write-Host "Backend container is not running!" -ForegroundColor Red
    docker-compose logs backend --tail 50
    exit 1
}

Write-Host ""
Write-Host "Deployment complete!" -ForegroundColor Green
