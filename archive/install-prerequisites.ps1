# Prerequisites Installation Script
# Run this script as Administrator for Chocolatey installations
# Or use manual installers (no admin required)

Write-Host "=== Prerequisites Installation Script ===" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "WARNING: Not running as Administrator. Chocolatey installations may fail." -ForegroundColor Yellow
    Write-Host "Consider using manual installers instead (see INSTALL_PREREQUISITES.md)" -ForegroundColor Yellow
    Write-Host ""
}

# Check if Chocolatey is installed
$chocoInstalled = Get-Command choco -ErrorAction SilentlyContinue

if ($chocoInstalled) {
    Write-Host "Chocolatey is installed. Using Chocolatey for installations." -ForegroundColor Green
    Write-Host ""
    
    # Install AWS CLI
    Write-Host "Installing AWS CLI..." -ForegroundColor Cyan
    try {
        choco install awscli -y
        Write-Host "AWS CLI installed successfully" -ForegroundColor Green
    } catch {
        Write-Host "AWS CLI installation failed. Try manual installer: https://awscli.amazonaws.com/AWSCLIV2.msi" -ForegroundColor Red
    }
    
    # Install Terraform
    Write-Host "`nInstalling Terraform..." -ForegroundColor Cyan
    try {
        choco install terraform -y
        Write-Host "Terraform installed successfully" -ForegroundColor Green
    } catch {
        Write-Host "Terraform installation failed. Try manual installer: https://developer.hashicorp.com/terraform/downloads" -ForegroundColor Red
    }
    
    # Install GitHub CLI (optional)
    Write-Host "`nInstalling GitHub CLI (optional)..." -ForegroundColor Cyan
    try {
        choco install gh -y
        Write-Host "GitHub CLI installed successfully" -ForegroundColor Green
    } catch {
        Write-Host "GitHub CLI installation failed. You can configure branch protection manually." -ForegroundColor Yellow
    }
} else {
    Write-Host "Chocolatey is not installed." -ForegroundColor Yellow
    Write-Host "Please install manually:" -ForegroundColor Yellow
    Write-Host "  - AWS CLI: https://awscli.amazonaws.com/AWSCLIV2.msi" -ForegroundColor Yellow
    Write-Host "  - Terraform: https://developer.hashicorp.com/terraform/downloads" -ForegroundColor Yellow
    Write-Host "  - GitHub CLI: https://cli.github.com/" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Or install Chocolatey first: https://chocolatey.org/install" -ForegroundColor Yellow
}

# Refresh environment variables
Write-Host "`nRefreshing environment variables..." -ForegroundColor Cyan
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

# Verify installations
Write-Host "`n=== Verifying Installations ===" -ForegroundColor Cyan
Write-Host ""

# Check AWS CLI
Write-Host "Checking AWS CLI..." -ForegroundColor Cyan
$aws = Get-Command aws -ErrorAction SilentlyContinue
if ($aws) {
    aws --version
} else {
    Write-Host "AWS CLI not found. Please install manually." -ForegroundColor Red
}

# Check Terraform
Write-Host "`nChecking Terraform..." -ForegroundColor Cyan
$terraform = Get-Command terraform -ErrorAction SilentlyContinue
if ($terraform) {
    terraform --version
} else {
    Write-Host "Terraform not found. Please install manually." -ForegroundColor Red
}

# Check GitHub CLI
Write-Host "`nChecking GitHub CLI..." -ForegroundColor Cyan
$gh = Get-Command gh -ErrorAction SilentlyContinue
if ($gh) {
    gh --version
} else {
    Write-Host "GitHub CLI not found (optional)." -ForegroundColor Yellow
}

Write-Host "`n=== Installation Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Configure AWS CLI: aws configure" -ForegroundColor White
Write-Host "2. Initialize Terraform: cd infrastructure/terraform && terraform init" -ForegroundColor White
Write-Host "3. Set up branch protection: See .github/BRANCH_PROTECTION_SETUP.md" -ForegroundColor White

