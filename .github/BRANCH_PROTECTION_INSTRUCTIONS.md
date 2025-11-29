# Branch Protection Setup Instructions

## Step 1: Authenticate GitHub CLI

First, you need to authenticate with GitHub:

```powershell
gh auth login
```

Follow the prompts:
1. Choose "GitHub.com"
2. Choose "HTTPS" or "SSH" (HTTPS recommended)
3. Authenticate via web browser or token

## Step 2: Run the Configuration Script

Once authenticated, run the script:

```powershell
cd .github
.\configure-branch-protection.ps1
```

Or from the root directory:

```powershell
.\github\configure-branch-protection.ps1
```

## What the Script Does

The script will configure:

### Main Branch Protection:
- ✅ Require pull request before merging
- ✅ Require 1 approval
- ✅ Dismiss stale reviews when new commits are pushed
- ✅ Require branches to be up to date
- ✅ Require linear history
- ✅ Enforce on administrators
- ✅ Block force pushes
- ✅ Block deletions

### Develop Branch Protection:
- ✅ Require pull request before merging
- ✅ Require 1 approval
- ✅ Dismiss stale reviews when new commits are pushed
- ✅ Require branches to be up to date
- ❌ Do NOT enforce on administrators (more flexible for dev)
- ✅ Block force pushes
- ✅ Block deletions

## Manual Alternative

If you prefer to set it up manually:

1. Go to: https://github.com/Stanmozolevskiy/Vector/settings/branches
2. Click "Add rule"
3. Enter `main` in "Branch name pattern"
4. Enable the settings listed above
5. Click "Create"
6. Repeat for `develop` branch

## Verify Configuration

After running the script, verify:

1. Visit: https://github.com/Stanmozolevskiy/Vector/settings/branches
2. You should see protection rules for both `main` and `develop`
3. Try creating a test branch and pushing directly to `main` - it should be blocked

## Troubleshooting

### Error: "Not authenticated"
- Run `gh auth login` first

### Error: "Resource not found"
- Verify repository name: `Stanmozolevskiy/Vector`
- Check if you have admin access to the repository

### Error: "Permission denied"
- You need admin access to configure branch protection
- Check repository permissions in GitHub

