# Branch Protection via GitHub CLI

## Option 1: Install GitHub CLI (Recommended)

GitHub CLI (`gh`) allows you to configure branch protection from the terminal.

### Install GitHub CLI

**Windows:**
1. Download: https://cli.github.com/
2. Or use Chocolatey: `choco install gh`
3. Or use Winget: `winget install --id GitHub.cli`

### Authenticate

```powershell
gh auth login
```

Follow the prompts to authenticate with GitHub.

### Configure Branch Protection

Once authenticated, you can use the GitHub API or `gh` commands:

```powershell
# For main branch
gh api repos/Stanmozolevskiy/Vector/branches/main/protection \
  --method PUT \
  --field required_status_checks='{"strict":true,"contexts":[]}' \
  --field enforce_admins=true \
  --field required_pull_request_reviews='{"required_approving_review_count":1,"dismiss_stale_reviews":true,"require_code_owner_reviews":false}' \
  --field restrictions=null

# For develop branch (less strict)
gh api repos/Stanmozolevskiy/Vector/branches/develop/protection \
  --method PUT \
  --field required_status_checks='{"strict":true,"contexts":[]}' \
  --field enforce_admins=false \
  --field required_pull_request_reviews='{"required_approving_review_count":1,"dismiss_stale_reviews":true,"require_code_owner_reviews":false}' \
  --field restrictions=null
```

## Option 2: Manual Setup (Current Method)

Since GitHub CLI is not installed, you need to set up branch protection manually:

1. Go to: https://github.com/Stanmozolevskiy/Vector/settings/branches
2. Click "Add rule"
3. Enter `main` in "Branch name pattern"
4. Enable:
   - ✅ Require a pull request before merging
   - ✅ Require approvals: 1
   - ✅ Require status checks to pass before merging
   - ✅ Require conversation resolution before merging
   - ✅ Include administrators
5. Click "Create"
6. Repeat for `develop` branch

## Option 3: Use GitHub API with curl

If you have a GitHub Personal Access Token:

```powershell
$token = "your-github-token"
$headers = @{
    "Authorization" = "token $token"
    "Accept" = "application/vnd.github.v3+json"
}

# Protect main branch
$body = @{
    required_status_checks = @{
        strict = $true
        contexts = @()
    }
    enforce_admins = $true
    required_pull_request_reviews = @{
        required_approving_review_count = 1
        dismiss_stale_reviews = $true
        require_code_owner_reviews = $false
    }
    restrictions = $null
} | ConvertTo-Json -Depth 10

Invoke-RestMethod -Uri "https://api.github.com/repos/Stanmozolevskiy/Vector/branches/main/protection" `
    -Method PUT `
    -Headers $headers `
    -Body $body `
    -ContentType "application/json"
```

**To create a Personal Access Token:**
1. Go to: https://github.com/settings/tokens
2. Click "Generate new token (classic)"
3. Select scopes: `repo`, `admin:repo_hook`
4. Copy the token and use it in the script above

## Recommendation

**For now:** Use Option 2 (Manual Setup) - it's the quickest and doesn't require additional tools.

**For automation:** Install GitHub CLI (Option 1) - it's the most convenient for future use.

