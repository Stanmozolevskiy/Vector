# GitHub Actions Troubleshooting Guide

## Issue: Workflows Not Triggering Automatically

### Problem
GitHub Actions workflows for backend and frontend are not triggering automatically on push to `staging` branch.

### Root Cause
The workflows have **path filters** that only trigger when specific paths change:

**Backend Workflow Triggers On:**
- `backend/**`
- `.github/workflows/backend.yml`
- `docker/Dockerfile.backend`
- `.deployment-trigger`

**Frontend Workflow Triggers On:**
- `frontend/**`
- `.github/workflows/frontend.yml`
- `docker/Dockerfile.frontend`
- `.deployment-trigger`

**Recent commits changed:**
- Documentation files (`*.md`)
- Infrastructure files (`infrastructure/terraform/**`)
- GitHub secrets documents

These paths don't match the filters, so workflows didn't trigger.

---

## Solutions

### Option 1: Remove Path Filters (Recommended for Staging/Production)

Remove path filters so workflows trigger on **any** push to the branch. This ensures deployments happen even when only documentation or infrastructure changes.

**Pros:**
- ✅ Always triggers on push
- ✅ No manual intervention needed
- ✅ Ensures deployments stay in sync

**Cons:**
- ⚠️ May trigger unnecessary builds (but tests will fail fast if nothing changed)

### Option 2: Add More Paths to Filters

Add additional paths to the filters to include infrastructure and documentation changes.

**Example:**
```yaml
paths:
  - 'backend/**'
  - 'frontend/**'
  - '.github/workflows/**'
  - 'docker/**'
  - 'infrastructure/**'
  - '*.md'
  - '.deployment-trigger'
```

**Pros:**
- ✅ More control over when workflows run
- ✅ Can still filter out irrelevant changes

**Cons:**
- ⚠️ Need to maintain path list
- ⚠️ May miss some important changes

### Option 3: Use `.deployment-trigger` File

Create or touch a `.deployment-trigger` file to force deployment:

```bash
touch .deployment-trigger
git add .deployment-trigger
git commit -m "Trigger deployment"
git push
```

**Pros:**
- ✅ Explicit control
- ✅ No workflow changes needed

**Cons:**
- ⚠️ Manual step required
- ⚠️ Easy to forget

### Option 4: Manual Trigger via GitHub UI

Use `workflow_dispatch` to manually trigger workflows from GitHub Actions UI.

**Steps:**
1. Go to GitHub → Actions
2. Select "Backend CI/CD" or "Frontend CI/CD"
3. Click "Run workflow"
4. Select branch: `staging`
5. Click "Run workflow"

**Pros:**
- ✅ Full control
- ✅ No code changes needed

**Cons:**
- ⚠️ Manual process
- ⚠️ Not automated

---

## Recommended Fix: Remove Path Filters for Staging/Production

For staging and production environments, it's recommended to **remove path filters** to ensure deployments always happen when code is pushed.

### Implementation

**For Backend Workflow:**
```yaml
on:
  push:
    branches:
      - develop
      - staging
      - main
    # Remove paths filter for staging/main to ensure deployments
    paths-ignore:
      - '*.md'
      - 'docs/**'
      - '.gitignore'
      - 'README.md'
  workflow_dispatch:
```

**For Frontend Workflow:**
```yaml
on:
  push:
    branches:
      - develop
      - staging
      - main
    # Remove paths filter for staging/main to ensure deployments
    paths-ignore:
      - '*.md'
      - 'docs/**'
      - '.gitignore'
      - 'README.md'
  workflow_dispatch:
```

**Alternative: Use `paths-ignore` instead of `paths`**
This will trigger on all changes EXCEPT documentation:

```yaml
on:
  push:
    branches:
      - develop
      - staging
      - main
    paths-ignore:
      - '*.md'
      - 'docs/**'
      - '.gitignore'
  workflow_dispatch:
```

---

## Current Workflow Status

### Backend Workflow
- ✅ Triggers on: `backend/**`, `.github/workflows/backend.yml`, `docker/Dockerfile.backend`, `.deployment-trigger`
- ❌ Does NOT trigger on: Documentation, infrastructure, or other file changes

### Frontend Workflow
- ✅ Triggers on: `frontend/**`, `.github/workflows/frontend.yml`, `docker/Dockerfile.frontend`, `.deployment-trigger`
- ❌ Does NOT trigger on: Documentation, infrastructure, or other file changes

---

## Immediate Action to Trigger Deployment

### Option A: Create Deployment Trigger File
```bash
touch .deployment-trigger
git add .deployment-trigger
git commit -m "Trigger staging deployment"
git push origin staging
```

### Option B: Manual Trigger via GitHub UI
1. Go to: https://github.com/Stanmozolevskiy/Vector/actions
2. Select "Backend CI/CD" workflow
3. Click "Run workflow" → Select `staging` branch → Run
4. Repeat for "Frontend CI/CD" workflow

### Option C: Make a Small Change to Trigger Paths
```bash
# Touch a file in the backend directory to trigger backend workflow
echo "" >> backend/Vector.Api/.deployment-trigger
git add backend/Vector.Api/.deployment-trigger
git commit -m "Trigger backend deployment"
git push origin staging

# Touch a file in the frontend directory to trigger frontend workflow
echo "" >> frontend/.deployment-trigger
git add frontend/.deployment-trigger
git commit -m "Trigger frontend deployment"
git push origin staging
```

---

## Long-Term Solution

Update workflows to use `paths-ignore` instead of `paths` for staging and production branches, or remove path filters entirely for these branches to ensure deployments always happen.

---

**Last Updated:** December 9, 2025

