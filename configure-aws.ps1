# AWS Configuration Script
# This script helps configure AWS credentials without making any infrastructure changes

param(
    [string]$AccessKeyId = "",
    [string]$SecretAccessKey = "",
    [string]$Region = "us-east-1",
    [string]$OutputFormat = "json"
)

Write-Host "AWS Configuration Script" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host ""

# Check if AWS CLI is installed
try {
    $awsVersion = aws --version 2>&1
    Write-Host "✅ AWS CLI found: $awsVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ AWS CLI not found. Please install it first:" -ForegroundColor Red
    Write-Host "   Download from: https://awscli.amazonaws.com/AWSCLIV2.msi" -ForegroundColor Yellow
    Write-Host "   Or use: choco install awscli" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# If credentials not provided as parameters, prompt for them
if ([string]::IsNullOrEmpty($AccessKeyId)) {
    Write-Host "Enter your AWS credentials:" -ForegroundColor Yellow
    $AccessKeyId = Read-Host "AWS Access Key ID"
}

if ([string]::IsNullOrEmpty($SecretAccessKey)) {
    $SecretAccessKey = Read-Host "AWS Secret Access Key" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecretAccessKey)
    $SecretAccessKey = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
}

if ([string]::IsNullOrEmpty($Region)) {
    $Region = Read-Host "Default region name [us-east-1]"
    if ([string]::IsNullOrEmpty($Region)) {
        $Region = "us-east-1"
    }
}

if ([string]::IsNullOrEmpty($OutputFormat)) {
    $OutputFormat = Read-Host "Default output format [json]"
    if ([string]::IsNullOrEmpty($OutputFormat)) {
        $OutputFormat = "json"
    }
}

Write-Host ""
Write-Host "Configuring AWS credentials..." -ForegroundColor Cyan

# Configure AWS CLI
$env:AWS_ACCESS_KEY_ID = $AccessKeyId
$env:AWS_SECRET_ACCESS_KEY = $SecretAccessKey
$env:AWS_DEFAULT_REGION = $Region

# Use AWS configure command
$configureInput = @"
$AccessKeyId
$SecretAccessKey
$Region
$OutputFormat
"@

$configureInput | aws configure

Write-Host ""
Write-Host "Verifying AWS connection..." -ForegroundColor Cyan

# Test connection
try {
    $identity = aws sts get-caller-identity 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ AWS credentials configured successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Account Information:" -ForegroundColor Cyan
        $identity | ConvertFrom-Json | Format-List
        Write-Host ""
        Write-Host "✅ Ready to use Terraform!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "   1. Edit infrastructure/terraform/terraform.tfvars" -ForegroundColor White
        Write-Host "   2. cd infrastructure/terraform" -ForegroundColor White
        Write-Host "   3. terraform plan   # Review what will be created" -ForegroundColor White
        Write-Host "   4. terraform apply  # Create infrastructure (when ready)" -ForegroundColor White
    } else {
        Write-Host "❌ Failed to verify AWS credentials" -ForegroundColor Red
        Write-Host $identity -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error verifying credentials: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "Note: Credentials are stored in:" -ForegroundColor Cyan
Write-Host "   $env:USERPROFILE\.aws\credentials" -ForegroundColor Yellow
Write-Host "   $env:USERPROFILE\.aws\config" -ForegroundColor Yellow

