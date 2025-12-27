# PowerShell script to show current AWS costs
# Usage: .\show-costs.ps1
# This script displays current AWS costs for the Vector project

param(
    [Parameter(Mandatory=$false)]
    [string]$AwsRegion = "us-east-1",
    
    [Parameter(Mandatory=$false)]
    [int]$Days = 7
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "AWS Cost Report - Vector Project" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check if AWS CLI is installed
$awsCheck = aws --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: AWS CLI is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

# Check if Cost Explorer is available (requires permissions)
Write-Host "Fetching cost data for the last $Days days..." -ForegroundColor Yellow
Write-Host ""

# Get current date and start date
$endDate = Get-Date -Format "yyyy-MM-dd"
$startDate = (Get-Date).AddDays(-$Days).ToString("yyyy-MM-dd")

# Try to get cost data using AWS Cost Explorer API
Write-Host "Attempting to fetch cost data from AWS Cost Explorer..." -ForegroundColor Gray
Write-Host "Note: This requires Cost Explorer API permissions" -ForegroundColor Gray
Write-Host ""

# Get cost and usage data
$costData = aws ce get-cost-and-usage `
    --time-period Start=$startDate,End=$endDate `
    --granularity DAILY `
    --metrics "BlendedCost" "UnblendedCost" `
    --group-by Type=DIMENSION,Key=SERVICE `
    --region us-east-1 `
    2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "Cost Data Retrieved Successfully!" -ForegroundColor Green
    Write-Host ""
    
    # Parse and display cost data
    $costJson = $costData | ConvertFrom-Json
    
    Write-Host "Daily Costs (Last $Days days):" -ForegroundColor Cyan
    Write-Host "================================" -ForegroundColor Cyan
    Write-Host ""
    
    $totalCost = 0
    $serviceCosts = @{}
    
    foreach ($result in $costJson.ResultsByTime) {
        $date = $result.TimePeriod.Start
        Write-Host "Date: $date" -ForegroundColor Yellow
        
        foreach ($group in $result.Groups) {
            $service = $group.Keys[0]
            $amount = [double]$group.Metrics.BlendedCost.Amount
            
            if ($amount -gt 0) {
                if (-not $serviceCosts.ContainsKey($service)) {
                    $serviceCosts[$service] = 0
                }
                $serviceCosts[$service] += $amount
                $totalCost += $amount
                
                Write-Host "  $service : `$$([math]::Round($amount, 2))" -ForegroundColor White
            }
        }
        Write-Host ""
    }
    
    Write-Host "================================" -ForegroundColor Cyan
    Write-Host "Total Cost (Last $Days days): `$$([math]::Round($totalCost, 2))" -ForegroundColor Green
    Write-Host "Estimated Monthly Cost: `$$([math]::Round($totalCost * (30 / $Days), 2))" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "Cost by Service (Last $Days days):" -ForegroundColor Cyan
    Write-Host "================================" -ForegroundColor Cyan
    $serviceCosts.GetEnumerator() | Sort-Object -Property Value -Descending | ForEach-Object {
        Write-Host "$($_.Key): `$$([math]::Round($_.Value, 2))" -ForegroundColor White
    }
    
} else {
    Write-Host "⚠ Could not fetch cost data from Cost Explorer API" -ForegroundColor Yellow
    Write-Host "This might be due to:" -ForegroundColor Yellow
    Write-Host "  1. Missing Cost Explorer API permissions" -ForegroundColor White
    Write-Host "  2. Cost Explorer not enabled (takes 24 hours to activate)" -ForegroundColor White
    Write-Host "  3. No cost data available yet" -ForegroundColor White
    Write-Host ""
    
    # Alternative: Show resource usage that contributes to costs
    Write-Host "Showing current resource usage (cost indicators):" -ForegroundColor Cyan
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Check running EC2 instances
    Write-Host "EC2 Instances:" -ForegroundColor Yellow
    $instances = aws ec2 describe-instances --filters "Name=instance-state-name,Values=running" --region $AwsRegion --query "Reservations[*].Instances[*].{InstanceId:InstanceId,Type:InstanceType,Name:Tags[?Key=='Name'].Value|[0]}" --output json 2>&1 | ConvertFrom-Json
    if ($instances.Count -gt 0) {
        foreach ($instance in $instances) {
            $name = if ($instance.Name) { $instance.Name } else { "Unnamed" }
            Write-Host "  - $name ($($instance.InstanceId)): $($instance.Type)" -ForegroundColor White
        }
    } else {
        Write-Host "  No running instances" -ForegroundColor Gray
    }
    Write-Host ""
    
    # Check RDS instances
    Write-Host "RDS Instances:" -ForegroundColor Yellow
    $rdsInstances = aws rds describe-db-instances --region $AwsRegion --query "DBInstances[*].{Identifier:DBInstanceIdentifier,Class:DBInstanceClass,Status:DBInstanceStatus}" --output json 2>&1 | ConvertFrom-Json
    if ($rdsInstances.Count -gt 0) {
        foreach ($rds in $rdsInstances) {
            $status = $rds.Status
            $costNote = if ($status -eq "stopped") { " (STOPPED - no cost)" } else { " (RUNNING - incurring cost)" }
            Write-Host "  - $($rds.Identifier): $($rds.Class) - Status: $status$costNote" -ForegroundColor White
        }
    } else {
        Write-Host "  No RDS instances" -ForegroundColor Gray
    }
    Write-Host ""
    
    # Check ElastiCache clusters
    Write-Host "ElastiCache Clusters:" -ForegroundColor Yellow
    $redisClusters = aws elasticache describe-replication-groups --region $AwsRegion --query "ReplicationGroups[*].{Id:ReplicationGroupId,Status:Status}" --output json 2>&1 | ConvertFrom-Json
    if ($redisClusters.Count -gt 0) {
        foreach ($redis in $redisClusters) {
            Write-Host "  - $($redis.Id): Status: $($redis.Status)" -ForegroundColor White
        }
    } else {
        Write-Host "  No ElastiCache clusters" -ForegroundColor Gray
    }
    Write-Host ""
    
    # Check ECS services
    Write-Host "ECS Services (Running Tasks):" -ForegroundColor Yellow
    $clusters = @("dev-vector-cluster", "staging-vector-cluster")
    foreach ($cluster in $clusters) {
        $services = aws ecs list-services --cluster $cluster --region $AwsRegion --output json 2>&1 | ConvertFrom-Json
        if ($services.ServiceArns.Count -gt 0) {
            Write-Host "  Cluster: $cluster" -ForegroundColor Cyan
            foreach ($serviceArn in $services.ServiceArns) {
                $serviceName = $serviceArn.Split('/')[-1]
                $serviceDetail = aws ecs describe-services --cluster $cluster --services $serviceName --region $AwsRegion --query "services[0].{Desired:desiredCount,Running:runningCount}" --output json 2>&1 | ConvertFrom-Json
                if ($serviceDetail.Running -gt 0) {
                    Write-Host "    - $serviceName : $($serviceDetail.Running) running tasks" -ForegroundColor White
                }
            }
        }
    }
    Write-Host ""
    
    Write-Host "Note: For detailed cost information, enable AWS Cost Explorer in the AWS Console" -ForegroundColor Yellow
    Write-Host "      Go to: AWS Console > Billing > Cost Explorer" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Cost Report Complete" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

