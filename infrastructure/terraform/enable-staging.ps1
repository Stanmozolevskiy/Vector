# PowerShell script to re-enable staging environment (scale up resources)
# Usage: .\enable-staging.ps1
# This script scales up resources that were previously disabled

param(
    [Parameter(Mandatory=$false)]
    [string]$AwsRegion = "us-east-1",
    
    [Parameter(Mandatory=$false)]
    [switch]$AutoApprove = $false
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Re-enable Staging Environment" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This will scale up staging resources:" -ForegroundColor Cyan
Write-Host "  - ECS Services: Scale to 1 task each" -ForegroundColor White
Write-Host "  - RDS: Start database instance" -ForegroundColor White
Write-Host "  - ElastiCache: Recreate cluster (via Terraform)" -ForegroundColor White
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
$awsVersion = aws --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: AWS CLI is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

Write-Host "AWS CLI version:" -ForegroundColor Green
Write-Host $awsVersion -ForegroundColor Gray
Write-Host ""

$clusterName = "staging-vector-cluster"
$backendServiceName = "staging-vector-backend-service"
$frontendServiceName = "staging-vector-frontend-service"
$rdsIdentifier = "staging-postgres"
$redisReplicationGroupId = "staging-redis"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Scaling Up Staging Resources" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Start RDS Instance (must be done first as services depend on it)
Write-Host "1. Starting RDS database instance..." -ForegroundColor Yellow
Write-Host "   This may take 5-10 minutes..." -ForegroundColor Gray

$rdsStartResult = aws rds start-db-instance --db-instance-identifier $rdsIdentifier --region $AwsRegion 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✓ RDS instance start initiated" -ForegroundColor Green
    Write-Host "   Waiting for RDS to be available..." -ForegroundColor Gray
    
    # Wait for RDS to be available (with timeout)
    $maxWait = 600  # 10 minutes
    $elapsed = 0
    $interval = 30  # Check every 30 seconds
    
    while ($elapsed -lt $maxWait) {
        Start-Sleep -Seconds $interval
        $elapsed += $interval
        
        $dbStatus = aws rds describe-db-instances --db-instance-identifier $rdsIdentifier --region $AwsRegion --query 'DBInstances[0].DBInstanceStatus' --output text 2>&1
        
        if ($dbStatus -eq "available") {
            Write-Host "   ✓ RDS instance is now available" -ForegroundColor Green
            break
        } elseif ($LASTEXITCODE -ne 0) {
            Write-Host "   ⚠ Could not check RDS status. Continuing anyway..." -ForegroundColor Yellow
            break
        } else {
            Write-Host "   RDS status: $dbStatus (waiting...)" -ForegroundColor Gray
        }
    }
} else {
    # Check if already running
    $dbStatus = aws rds describe-db-instances --db-instance-identifier $rdsIdentifier --region $AwsRegion --query 'DBInstances[0].DBInstanceStatus' --output text 2>&1
    
    if ($dbStatus -eq "available") {
        Write-Host "   ✓ RDS instance is already running" -ForegroundColor Green
    } elseif ($LASTEXITCODE -ne 0) {
        Write-Host "   ⚠ RDS instance may not exist. You may need to run terraform apply first." -ForegroundColor Yellow
    } else {
        Write-Host "   ⚠ RDS instance status: $dbStatus" -ForegroundColor Yellow
    }
}

Write-Host ""

# 2. Recreate ElastiCache Cluster (if needed)
Write-Host "2. Checking ElastiCache cluster..." -ForegroundColor Yellow

$redisCheckResult = aws elasticache describe-replication-groups --replication-group-id $redisReplicationGroupId --region $AwsRegion 2>&1 | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "   ElastiCache cluster does not exist." -ForegroundColor Yellow
    Write-Host "   You need to recreate it using Terraform:" -ForegroundColor Yellow
    Write-Host "     cd infrastructure/terraform" -ForegroundColor Gray
    Write-Host "     terraform workspace select staging" -ForegroundColor Gray
    Write-Host "     terraform apply -target=module.redis -var='environment=staging' ..." -ForegroundColor Gray
    Write-Host "   Or run full terraform apply to recreate all resources." -ForegroundColor Gray
} else {
    Write-Host "   ✓ ElastiCache cluster exists" -ForegroundColor Green
}

Write-Host ""

# 3. Scale ECS Services to 1
Write-Host "3. Scaling ECS services to 1 task each..." -ForegroundColor Yellow

# Backend service
Write-Host "   Scaling backend service to 1..." -ForegroundColor Gray
$backendResult = aws ecs update-service --cluster $clusterName --service $backendServiceName --desired-count 1 --region $AwsRegion 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✓ Backend service scaled to 1" -ForegroundColor Green
} else {
    Write-Host "   ⚠ Backend service may not exist. You may need to run terraform apply first." -ForegroundColor Yellow
}

# Frontend service
Write-Host "   Scaling frontend service to 1..." -ForegroundColor Gray
$frontendResult = aws ecs update-service --cluster $clusterName --service $frontendServiceName --desired-count 1 --region $AwsRegion 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✓ Frontend service scaled to 1" -ForegroundColor Green
} else {
    Write-Host "   ⚠ Frontend service may not exist. You may need to run terraform apply first." -ForegroundColor Yellow
}

Write-Host ""

Write-Host "=========================================" -ForegroundColor Green
Write-Host "Staging Environment Re-enabled!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Staging resources have been scaled up." -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Wait for ECS services to stabilize (2-5 minutes)" -ForegroundColor White
Write-Host "  2. Verify services are running:" -ForegroundColor White
Write-Host "     aws ecs describe-services --cluster $clusterName --services $backendServiceName $frontendServiceName --region $AwsRegion" -ForegroundColor Gray
Write-Host "  3. Get ALB DNS name:" -ForegroundColor White
Write-Host "     aws elbv2 describe-load-balancers --names staging-vector-alb --region $AwsRegion --query 'LoadBalancers[0].DNSName' --output text" -ForegroundColor Gray
Write-Host "  4. Test the staging environment" -ForegroundColor White
Write-Host ""
Write-Host "Note: If ElastiCache was recreated, services may need to reconnect." -ForegroundColor Yellow
Write-Host ""
