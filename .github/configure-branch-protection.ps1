# GitHub Branch Protection Configuration Script
# Run this script after GitHub CLI is installed and authenticated

Write-Host "Configuring Branch Protection Rules for Vector Repository" -ForegroundColor Cyan
Write-Host ""

# Check if GitHub CLI is available
try {
    $ghVersion = gh --version 2>&1
    Write-Host "✅ GitHub CLI found" -ForegroundColor Green
} catch {
    Write-Host "❌ GitHub CLI not found. Please install it first:" -ForegroundColor Red
    Write-Host "   winget install --id GitHub.cli" -ForegroundColor Yellow
    Write-Host "   Or download from: https://cli.github.com/" -ForegroundColor Yellow
    exit 1
}

# Check authentication
Write-Host "Checking GitHub authentication..." -ForegroundColor Cyan
$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Not authenticated. Please run:" -ForegroundColor Red
    Write-Host "   gh auth login" -ForegroundColor Yellow
    exit 1
}
Write-Host "✅ Authenticated" -ForegroundColor Green
Write-Host ""

# Repository
$repo = "Stanmozolevskiy/Vector"

Write-Host "Configuring branch protection for 'main' branch..." -ForegroundColor Cyan

# Main branch protection (strict)
$mainProtection = @{
    required_status_checks = @{
        strict = $true
        contexts = @()
    }
    enforce_admins = $true
    required_pull_request_reviews = @{
        required_approving_review_count = 1
        dismiss_stale_reviews = $true
        require_code_owner_reviews = $false
        require_last_push_approval = $false
    }
    restrictions = $null
    required_linear_history = $true
    allow_force_pushes = $false
    allow_deletions = $false
} | ConvertTo-Json -Depth 10

try {
    $mainProtection | gh api "repos/$repo/branches/main/protection" `
        --method PUT `
        --input - `
        --silent
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Main branch protection configured successfully" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed to configure main branch protection" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error configuring main branch: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "Configuring branch protection for 'develop' branch..." -ForegroundColor Cyan

# Develop branch protection (less strict)
$developProtection = @{
    required_status_checks = @{
        strict = $true
        contexts = @()
    }
    enforce_admins = $false
    required_pull_request_reviews = @{
        required_approving_review_count = 1
        dismiss_stale_reviews = $true
        require_code_owner_reviews = $false
        require_last_push_approval = $false
    }
    restrictions = $null
    required_linear_history = $false
    allow_force_pushes = $false
    allow_deletions = $false
} | ConvertTo-Json -Depth 10

try {
    $developProtection | gh api "repos/$repo/branches/develop/protection" `
        --method PUT `
        --input - `
        --silent
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Develop branch protection configured successfully" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed to configure develop branch protection" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error configuring develop branch: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "Branch protection configuration complete!" -ForegroundColor Green
Write-Host ""
Write-Host "To verify, visit:" -ForegroundColor Cyan
Write-Host "   https://github.com/Stanmozolevskiy/Vector/settings/branches" -ForegroundColor Yellow

