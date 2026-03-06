# Deploy Frontend to Local Docker
# Usage: .\deploy-frontend.ps1

Write-Host "Deploying Frontend to Local Docker..." -ForegroundColor Cyan

# Navigate to docker directory
$dockerDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $dockerDir

# Rebuild frontend without cache (required per deployment rules)
Write-Host "Rebuilding frontend container (no cache)..." -ForegroundColor Yellow
docker-compose build --no-cache frontend

if ($LASTEXITCODE -ne 0) {
    Write-Host "Frontend build failed!" -ForegroundColor Red
    exit 1
}

# Restart frontend container
Write-Host "Restarting frontend container..." -ForegroundColor Yellow
docker-compose up -d frontend

if ($LASTEXITCODE -ne 0) {
    Write-Host "Frontend deployment failed!" -ForegroundColor Red
    exit 1
}

# Wait for container to be ready
Start-Sleep -Seconds 3

# Verify container is running
Write-Host "Verifying container status..." -ForegroundColor Yellow
$status = docker ps --filter "name=vector-frontend" --format "{{.Status}}"

if ($status) {
    Write-Host "Frontend deployed successfully!" -ForegroundColor Green
    Write-Host "Container Status: $status" -ForegroundColor Gray
    Write-Host "URL: http://localhost:3000" -ForegroundColor Cyan
} else {
    Write-Host "Frontend container is not running!" -ForegroundColor Red
    docker-compose logs frontend --tail 50
    exit 1
}

Write-Host ""
Write-Host "Deployment complete!" -ForegroundColor Green
