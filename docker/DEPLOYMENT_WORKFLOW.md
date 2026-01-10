# Automatic Deployment Workflow

This document describes the automatic deployment workflow for local Docker development.

## Quick Deploy Commands

### Deploy Frontend Only
```powershell
cd docker
.\deploy-frontend.ps1
```

### Deploy Backend Only
```powershell
cd docker
.\deploy-backend.ps1
```

### Deploy All Services
```powershell
cd docker
.\deploy-all.ps1
```

### Deploy Specific Service
```powershell
cd docker
.\deploy-all.ps1 -service frontend
.\deploy-all.ps1 -service backend
```

## Automated Deployment

When code changes are made, the deployment process automatically:

1. **Rebuilds the container** without cache (to avoid stale assets)
2. **Restarts the container** with new code
3. **Verifies** the container is running
4. **Shows status** and URLs

## Deployment Rules

- **Frontend**: Always rebuild without cache (`--no-cache`) per deployment rules
- **Backend**: Rebuild without cache to ensure clean builds
- **Verification**: All scripts verify container status before completing
- **Error Handling**: Scripts exit with error code if deployment fails

## Typical Workflow

1. Make code changes in `frontend/src/` or `backend/Vector.Api/`
2. Run the appropriate deployment script:
   - Frontend changes: `.\deploy-frontend.ps1`
   - Backend changes: `.\deploy-backend.ps1`
   - Both: `.\deploy-all.ps1`
3. Wait for deployment to complete (usually 30-60 seconds)
4. Test the changes at:
   - Frontend: http://localhost:3000
   - Backend API: http://localhost:5000/api

## Troubleshooting

If deployment fails:
- Check Docker Desktop is running
- Verify no containers are using conflicting ports
- Check logs: `docker-compose logs frontend` or `docker-compose logs backend`
- Ensure all dependencies are installed locally

## Notes

- Deployment scripts are located in `docker/` directory
- All scripts navigate to the docker directory automatically
- Scripts follow the deployment rules (no cache rebuilds)
- Frontend rebuild takes ~20-30 seconds, Backend takes ~30-60 seconds
