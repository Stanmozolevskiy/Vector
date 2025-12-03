# Script to Enable Database Connection from Your Local Machine
# This script adds your public IP to the RDS security group

$region = "us-east-1"
$securityGroupId = "sg-049bb66ef327d258c"
$port = 5432

Write-Host "`n=== Vector Database Connection Setup ===" -ForegroundColor Cyan
Write-Host "`nGetting your public IP address..." -ForegroundColor Yellow

try {
    $myIP = (Invoke-WebRequest -Uri "https://api.ipify.org" -UseBasicParsing).Content
    Write-Host "Your public IP: $myIP" -ForegroundColor Green
} catch {
    Write-Host "Failed to get your public IP. Please enter it manually:" -ForegroundColor Red
    $myIP = Read-Host "Enter your public IP address"
}

Write-Host "`nAdding your IP ($myIP/32) to RDS security group..." -ForegroundColor Yellow

try {
    aws ec2 authorize-security-group-ingress `
        --group-id $securityGroupId `
        --protocol tcp `
        --port $port `
        --cidr "$myIP/32" `
        --region $region `
        --output json | Out-Null
    
    Write-Host "✅ Successfully added your IP to security group!" -ForegroundColor Green
    Write-Host "`nYou can now connect to the database using:" -ForegroundColor Cyan
    Write-Host "  Host: dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com" -ForegroundColor White
    Write-Host "  Port: 5432" -ForegroundColor White
    Write-Host "  Database: vector_db" -ForegroundColor White
    Write-Host "  Username: postgres" -ForegroundColor White
    Write-Host "  Password: `$Memic1234" -ForegroundColor White
    Write-Host "  SSL Mode: Require" -ForegroundColor White
    Write-Host "`n⚠️  Remember to remove your IP when done:" -ForegroundColor Yellow
    Write-Host "  Run: .\remove-database-access.ps1" -ForegroundColor Gray
} catch {
    Write-Host "❌ Failed to add IP to security group. Error:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "`nYou may need to check:" -ForegroundColor Yellow
    Write-Host "  1. AWS CLI is configured correctly" -ForegroundColor Gray
    Write-Host "  2. Your AWS credentials have EC2 permissions" -ForegroundColor Gray
    Write-Host "  3. The IP wasn't already added" -ForegroundColor Gray
}

