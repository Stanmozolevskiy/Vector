# GitHub Branch Protection Rules Setup Guide

This guide explains how to set up branch protection rules for the Vector repository.

## Branch Protection Rules for `main` Branch

### Required Settings

1. **Navigate to Repository Settings**
   - Go to: `https://github.com/Stanmozolevskiy/Vector/settings/branches`
   - Or: Repository → Settings → Branches

2. **Add Branch Protection Rule**
   - Click "Add rule" or "Add branch protection rule"
   - Branch name pattern: `main`

3. **Configure Protection Settings**

   **Required Settings:**
   - ✅ **Require a pull request before merging**
     - ✅ Require approvals: `1` (or more for your team)
     - ✅ Dismiss stale pull request approvals when new commits are pushed
     - ✅ Require review from Code Owners (if you have CODEOWNERS file)
   
   - ✅ **Require status checks to pass before merging**
     - ✅ Require branches to be up to date before merging
     - Add required status checks:
       - `Backend CI/CD` (from `.github/workflows/backend.yml`)
       - `Frontend CI/CD` (from `.github/workflows/frontend.yml`)
   
   - ✅ **Require conversation resolution before merging**
   
   - ✅ **Require linear history** (optional but recommended)
   
   - ✅ **Include administrators** (applies rules to admins too)

   **Optional but Recommended:**
   - ✅ **Do not allow bypassing the above settings**
   - ✅ **Restrict who can push to matching branches** (only specific teams/users)
   - ✅ **Require signed commits** (if you want to enforce GPG signing)

4. **Save the Rule**
   - Click "Create" or "Save changes"

## Branch Protection Rules for `develop` Branch

Repeat the same process for the `develop` branch with similar settings, but you may want:
- Fewer required approvals (1 instead of 2)
- Same status checks
- Allow force pushes (optional, for development flexibility)

## Status Checks Setup

For the status checks to work, you need to:

1. **Create GitHub Actions Workflows**
   - `.github/workflows/backend.yml` - Backend CI/CD
   - `.github/workflows/frontend.yml` - Frontend CI/CD

2. **Workflows must report status**
   - Use `actions/checkout@v4`
   - Jobs should have descriptive names
   - Status checks will appear automatically after first run

## Code Owners (Optional)

Create `.github/CODEOWNERS` file:

```
# Global owners
* @Stanmozolevskiy

# Backend
/backend/ @Stanmozolevskiy

# Frontend
/frontend/ @Stanmozolevskiy

# Infrastructure
/infrastructure/ @Stanmozolevskiy
```

## Verification

After setting up:
1. Try to push directly to `main` - should be blocked
2. Create a pull request - should require approval
3. Check that status checks run on PR

## Manual Setup Steps

Since this requires GitHub UI access, here are the exact steps:

1. Go to: https://github.com/Stanmozolevskiy/Vector/settings/branches
2. Click "Add rule"
3. Enter `main` in "Branch name pattern"
4. Check the following:
   - ✅ Require a pull request before merging
   - ✅ Require approvals: 1
   - ✅ Require status checks to pass before merging
   - ✅ Require conversation resolution before merging
   - ✅ Include administrators
5. Click "Create"

Repeat for `develop` branch with similar settings.

