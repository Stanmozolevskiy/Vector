# Manual AWS Configuration Script
# Use this if the automated script has issues with special characters

Write-Host "AWS Manual Configuration Script" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This script will help you configure AWS credentials step by step." -ForegroundColor Yellow
Write-Host ""

# Check if AWS CLI is installed
try {
    $awsVersion = aws --version 2>&1
    Write-Host "✅ AWS CLI found: $awsVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ AWS CLI not found. Please install it first:" -ForegroundColor Red
    Write-Host "   Download from: https://awscli.amazonaws.com/AWSCLIV2.msi" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Please run the following command manually:" -ForegroundColor Yellow
Write-Host ""
Write-Host "aws configure" -ForegroundColor Cyan
Write-Host ""
Write-Host "Then enter:" -ForegroundColor Yellow
Write-Host "  1. AWS Access Key ID: [your access key]" -ForegroundColor White
Write-Host "  2. AWS Secret Access Key: [your secret key]" -ForegroundColor White
Write-Host "  3. Default region name: us-east-1" -ForegroundColor White
Write-Host "  4. Default output format: json" -ForegroundColor White
Write-Host ""
Write-Host "After configuration, verify with:" -ForegroundColor Yellow
Write-Host "  aws sts get-caller-identity" -ForegroundColor Cyan
Write-Host ""

# Ask if user wants to proceed
$proceed = Read-Host "Have you configured AWS? (y/n)"
if ($proceed -eq "y" -or $proceed -eq "Y") {
    Write-Host ""
    Write-Host "Verifying AWS connection..." -ForegroundColor Cyan
    try {
        $identity = aws sts get-caller-identity 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ AWS credentials configured successfully!" -ForegroundColor Green
            Write-Host ""
            Write-Host "Account Information:" -ForegroundColor Cyan
            $identity | ConvertFrom-Json | Format-List
            Write-Host ""
            Write-Host "✅ Ready to use Terraform!" -ForegroundColor Green
        } else {
            Write-Host "❌ Failed to verify AWS credentials" -ForegroundColor Red
            Write-Host $identity -ForegroundColor Red
            Write-Host ""
            Write-Host "Common issues:" -ForegroundColor Yellow
            Write-Host "  - Secret key may have special characters that need escaping" -ForegroundColor White
            Write-Host "  - Check for extra spaces in credentials" -ForegroundColor White
            Write-Host "  - Verify credentials are correct in AWS Console" -ForegroundColor White
        }
    } catch {
        Write-Host "❌ Error verifying credentials: $_" -ForegroundColor Red
    }
}

