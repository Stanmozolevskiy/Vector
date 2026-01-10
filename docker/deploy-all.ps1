# Deploy All Services to Local Docker
# Usage: .\deploy-all.ps1 [frontend|backend|all]

param(
    [string]$service = "all"
)

Write-Host "Deploying to Local Docker..." -ForegroundColor Cyan

# Navigate to docker directory
$dockerDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $dockerDir

if ($service -eq "frontend" -or $service -eq "all") {
    Write-Host ""
    Write-Host "Building Frontend..." -ForegroundColor Yellow
    docker-compose build --no-cache frontend
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Frontend build failed!" -ForegroundColor Red
        exit 1
    }
}

if ($service -eq "backend" -or $service -eq "all") {
    Write-Host ""
    Write-Host "Building Backend..." -ForegroundColor Yellow
    docker-compose build --no-cache backend
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Backend build failed!" -ForegroundColor Red
        exit 1
    }
}

# Restart services
Write-Host ""
Write-Host "Restarting services..." -ForegroundColor Yellow

if ($service -eq "frontend") {
    docker-compose up -d frontend
} elseif ($service -eq "backend") {
    docker-compose up -d backend
} else {
    docker-compose up -d
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "Deployment failed!" -ForegroundColor Red
    exit 1
}

# Wait for containers to be ready
Start-Sleep -Seconds 3

# Verify containers are running
Write-Host ""
Write-Host "Verifying container status..." -ForegroundColor Yellow

if ($service -eq "frontend" -or $service -eq "all") {
    $frontendStatus = docker ps --filter "name=vector-frontend" --format "{{.Status}}"
    if ($frontendStatus) {
        Write-Host "Frontend: $frontendStatus" -ForegroundColor Green
        Write-Host "URL: http://localhost:3000" -ForegroundColor Cyan
    } else {
        Write-Host "Frontend is not running!" -ForegroundColor Red
    }
}

if ($service -eq "backend" -or $service -eq "all") {
    $backendStatus = docker ps --filter "name=vector-backend" --format "{{.Status}}"
    if ($backendStatus) {
        Write-Host "Backend: $backendStatus" -ForegroundColor Green
        Write-Host "API URL: http://localhost:5000/api" -ForegroundColor Cyan
    } else {
        Write-Host "Backend is not running!" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Deployment complete!" -ForegroundColor Green
