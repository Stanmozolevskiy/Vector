# Script to Remove Your IP from Database Security Group
# Run this when you're done connecting to the database

$region = "us-east-1"
$securityGroupId = "sg-049bb66ef327d258c"
$port = 5432

Write-Host "`n=== Remove Database Access ===" -ForegroundColor Cyan
Write-Host "`nGetting your public IP address..." -ForegroundColor Yellow

try {
    $myIP = (Invoke-WebRequest -Uri "https://api.ipify.org" -UseBasicParsing).Content
    Write-Host "Your public IP: $myIP" -ForegroundColor Green
} catch {
    Write-Host "Failed to get your public IP. Please enter it manually:" -ForegroundColor Red
    $myIP = Read-Host "Enter your public IP address"
}

Write-Host "`nRemoving your IP ($myIP/32) from RDS security group..." -ForegroundColor Yellow

try {
    aws ec2 revoke-security-group-ingress `
        --group-id $securityGroupId `
        --protocol tcp `
        --port $port `
        --cidr "$myIP/32" `
        --region $region `
        --output json | Out-Null
    
    Write-Host "✅ Successfully removed your IP from security group!" -ForegroundColor Green
    Write-Host "Database access from your IP has been revoked." -ForegroundColor Cyan
} catch {
    Write-Host "❌ Failed to remove IP from security group. Error:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "`nThe IP may not have been in the security group, or you may need to check AWS permissions." -ForegroundColor Yellow
}

