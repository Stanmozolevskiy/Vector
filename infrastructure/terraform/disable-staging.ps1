# PowerShell script to temporarily disable staging environment (scale down to save costs)
# Usage: .\disable-staging.ps1
# This script scales down resources but does NOT destroy them, allowing easy re-enablement

param(
    [Parameter(Mandatory=$false)]
    [string]$AwsRegion = "us-east-1",
    
    [Parameter(Mandatory=$false)]
    [switch]$AutoApprove = $false
)

Write-Host "=========================================" -ForegroundColor Yellow
Write-Host "Temporarily Disable Staging Environment" -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "This will scale down staging resources to save costs:" -ForegroundColor Cyan
Write-Host "  - ECS Services: Scale to 0 tasks (no compute cost)" -ForegroundColor White
Write-Host "  - RDS: Stop database instance (saves ~$15/month)" -ForegroundColor White
Write-Host "  - ElastiCache: Delete cluster (saves ~$12/month)" -ForegroundColor White
Write-Host "  - ALB: Keep running (minimal cost, needed for re-enablement)" -ForegroundColor White
Write-Host "  - VPC, S3, ECR: Keep (minimal/no cost)" -ForegroundColor White
Write-Host ""
Write-Host "Note: Resources are NOT destroyed. Use enable-staging.ps1 to re-enable." -ForegroundColor Green
Write-Host ""

if (-not $AutoApprove) {
    Write-Host "Do you want to proceed? (Y/N)" -ForegroundColor Yellow
    $confirmation = Read-Host
    
    if ($confirmation -ne "Y" -and $confirmation -ne "y") {
        Write-Host "Operation cancelled." -ForegroundColor Yellow
        exit 0
    }
}

# Check if AWS CLI is installed
$awsCheck = aws --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: AWS CLI is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

Write-Host "AWS CLI version:" -ForegroundColor Green
Write-Host $awsCheck -ForegroundColor Gray
Write-Host ""

$clusterName = "staging-vector-cluster"
$backendServiceName = "staging-vector-backend-service"
$frontendServiceName = "staging-vector-frontend-service"
$rdsIdentifier = "staging-postgres"
$redisReplicationGroupId = "staging-redis"

Write-Host "=========================================" -ForegroundColor Yellow
Write-Host "Scaling Down Staging Resources" -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Yellow
Write-Host ""

# 1. Scale ECS Services to 0
Write-Host "1. Scaling ECS services to 0 tasks..." -ForegroundColor Yellow

Write-Host "   Scaling backend service to 0..." -ForegroundColor Gray
$null = aws ecs update-service --cluster $clusterName --service $backendServiceName --desired-count 0 --region $AwsRegion 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✓ Backend service scaled to 0" -ForegroundColor Green
} else {
    Write-Host "   ⚠ Backend service may not exist or already at 0" -ForegroundColor Yellow
}

Write-Host "   Scaling frontend service to 0..." -ForegroundColor Gray
$null = aws ecs update-service --cluster $clusterName --service $frontendServiceName --desired-count 0 --force-new-deployment --region $AwsRegion 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✓ Frontend service scaled to 0 (waiting for tasks to stop...)" -ForegroundColor Green
    # Wait for tasks to stop
    $maxWait = 300
    $elapsed = 0
    while ($elapsed -lt $maxWait) {
        Start-Sleep -Seconds 10
        $elapsed += 10
        $statusJson = aws ecs describe-services --cluster $clusterName --services $frontendServiceName --region $AwsRegion --query "services[0].{Running:runningCount,Desired:desiredCount}" --output json 2>&1
        if ($LASTEXITCODE -eq 0) {
            $status = $statusJson | ConvertFrom-Json
            if ($status.Running -eq 0 -and $status.Desired -eq 0) {
                Write-Host "   ✓ All frontend tasks stopped" -ForegroundColor Green
                break
            }
        }
        if ($elapsed -ge $maxWait) {
            Write-Host "   ⚠ Timeout waiting for tasks to stop" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "   ⚠ Frontend service may not exist or already at 0" -ForegroundColor Yellow
}

Write-Host ""

# 2. Stop RDS Instance
Write-Host "2. Stopping RDS database instance..." -ForegroundColor Yellow
Write-Host "   Note: RDS can be stopped for up to 7 days" -ForegroundColor Gray

$null = aws rds stop-db-instance --db-instance-identifier $rdsIdentifier --region $AwsRegion 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✓ RDS instance stop initiated" -ForegroundColor Green
} else {
    Write-Host "   ⚠ RDS instance may already be stopped or may not exist" -ForegroundColor Yellow
}

Write-Host ""

# 3. Delete ElastiCache Cluster
Write-Host "3. Deleting ElastiCache cluster..." -ForegroundColor Yellow
Write-Host "   Note: Cluster will be recreated when re-enabling staging" -ForegroundColor Gray

$null = aws elasticache delete-replication-group --replication-group-id $redisReplicationGroupId --region $AwsRegion 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✓ ElastiCache cluster deletion initiated" -ForegroundColor Green
} else {
    Write-Host "   ⚠ ElastiCache cluster may already be deleted or may not exist" -ForegroundColor Yellow
}

Write-Host ""

Write-Host "=========================================" -ForegroundColor Green
Write-Host "Staging Environment Disabled!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Staging resources have been scaled down to save costs." -ForegroundColor White
Write-Host ""
Write-Host "Estimated monthly savings:" -ForegroundColor Cyan
Write-Host "  - ECS compute: ~$30-50/month" -ForegroundColor White
Write-Host "  - RDS: ~$15/month (while stopped, max 7 days)" -ForegroundColor White
Write-Host "  - ElastiCache: ~$12/month" -ForegroundColor White
Write-Host "  - Total: ~$57-77/month" -ForegroundColor Green
Write-Host ""
Write-Host "To re-enable staging, run:" -ForegroundColor Yellow
Write-Host "  .\enable-staging.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "Note: RDS can only be stopped for 7 days. After that, it will auto-start." -ForegroundColor Yellow
Write-Host "      If you need longer, consider scaling to db.t3.micro instead." -ForegroundColor Yellow
Write-Host ""
