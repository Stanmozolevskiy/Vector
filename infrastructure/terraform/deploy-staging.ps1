# PowerShell script to deploy staging environment infrastructure
# Usage: .\deploy-staging.ps1

param(
    [Parameter(Mandatory=$true)]
    [string]$DbPassword,
    
    [Parameter(Mandatory=$false)]
    [string]$AwsRegion = "us-east-1",
    
    [Parameter(Mandatory=$false)]
    [string]$VpcCidr = "10.1.0.0/16",
    
    [Parameter(Mandatory=$false)]
    [string]$BastionSshKey = "",
    
    [Parameter(Mandatory=$false)]
    [string]$SendGridApiKey = "",
    
    [Parameter(Mandatory=$false)]
    [string]$SendGridFromEmail = "",
    
    [Parameter(Mandatory=$false)]
    [string]$SendGridFromName = "Vector"
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Staging Environment Deployment" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check if terraform is installed
$terraformVersion = terraform version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Terraform is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

Write-Host "Terraform version:" -ForegroundColor Green
Write-Host $terraformVersion -ForegroundColor Gray
Write-Host ""

# Navigate to terraform directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Initialize terraform
Write-Host "Initializing Terraform..." -ForegroundColor Yellow
terraform init
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Terraform initialization failed" -ForegroundColor Red
    exit 1
}

# Create or select staging workspace
Write-Host "Setting up staging workspace..." -ForegroundColor Yellow
terraform workspace list | Out-String | Select-String -Pattern "staging" | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Creating staging workspace..." -ForegroundColor Yellow
    terraform workspace new staging
}
terraform workspace select staging
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to select staging workspace" -ForegroundColor Red
    exit 1
}

Write-Host "Current workspace: $(terraform workspace show)" -ForegroundColor Green
Write-Host ""

# Build terraform variables
$tfVars = @{
    environment = "staging"
    aws_region = $AwsRegion
    vpc_cidr = $VpcCidr
    db_password = $DbPassword
    db_instance_class = "db.t3.small"
    redis_node_type = "cache.t3.small"
}

if ($BastionSshKey) {
    $tfVars["bastion_ssh_public_key"] = $BastionSshKey
}

if ($SendGridApiKey) {
    $tfVars["sendgrid_api_key"] = $SendGridApiKey
    $tfVars["sendgrid_from_email"] = $SendGridFromEmail
    $tfVars["sendgrid_from_name"] = $SendGridFromName
}

# Build terraform apply command
$applyArgs = @()
foreach ($key in $tfVars.Keys) {
    $value = $tfVars[$key]
    if ($key -eq "db_password" -or $key -eq "sendgrid_api_key") {
        # Use single quotes for sensitive values to prevent PowerShell variable expansion
        $applyArgs += "-var=`"$key='$value'`""
    } else {
        $applyArgs += "-var=`"$key=$value`""
    }
}

# Run terraform plan
Write-Host "Running Terraform plan..." -ForegroundColor Yellow
$planCmd = "terraform plan " + ($applyArgs -join " ")
Invoke-Expression $planCmd
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Terraform plan failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Review the plan above. Do you want to proceed with deployment? (Y/N)" -ForegroundColor Yellow
$confirmation = Read-Host

if ($confirmation -ne "Y" -and $confirmation -ne "y") {
    Write-Host "Deployment cancelled." -ForegroundColor Yellow
    exit 0
}

# Run terraform apply
Write-Host ""
Write-Host "Applying Terraform configuration..." -ForegroundColor Yellow
$applyCmd = "terraform apply -auto-approve " + ($applyArgs -join " ")
Invoke-Expression $applyCmd
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Terraform apply failed" -ForegroundColor Red
    exit 1
}

# Get outputs
Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Getting infrastructure outputs..." -ForegroundColor Yellow
terraform output

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Get ALB DNS name from outputs above" -ForegroundColor White
Write-Host "2. Update GitHub Secret: STAGING_API_URL = http://<alb-dns-name>/api" -ForegroundColor White
Write-Host "3. Push code to staging branch to trigger deployment" -ForegroundColor White
Write-Host "4. Verify deployment at: http://<alb-dns-name>" -ForegroundColor White
Write-Host ""

