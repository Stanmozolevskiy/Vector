# GitHub Actions Workflows

## Manual Triggering

Both workflows now support manual triggering via `workflow_dispatch`.

### How to Manually Trigger

1. Go to: https://github.com/Stanmozolevskiy/Vector/actions
2. Select the workflow you want to run:
   - **Backend CI/CD**
   - **Frontend CI/CD**
3. Click **Run workflow** button (top right)
4. Select branch: `develop`
5. Click **Run workflow**

### Automatic Triggers

Workflows automatically trigger on push to `develop`, `staging`, or `main` when:
- **Backend:** Changes to `backend/**`, `.github/workflows/backend.yml`, `docker/Dockerfile.backend`, or `.deployment-trigger`
- **Frontend:** Changes to `frontend/**`, `.github/workflows/frontend.yml`, `docker/Dockerfile.frontend`, or `.deployment-trigger`

### Deployment Trigger File

The `.deployment-trigger` file can be updated to trigger both workflows:

```bash
echo "# Trigger deployment" >> .deployment-trigger
git add .deployment-trigger
git commit -m "Trigger deployment"
git push origin develop
```

