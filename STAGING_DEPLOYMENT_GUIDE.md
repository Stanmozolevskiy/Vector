# Staging Deployment Guide

## Overview

To deploy to staging, you need to:
1. Create staging infrastructure (Terraform)
2. Add staging secrets to GitHub
3. Update CI/CD workflows for staging
4. Push to `staging` branch

---

## Step 1: Create Staging Infrastructure

### Deploy Staging Environment with Terraform

```powershell
cd infrastructure/terraform

# Apply Terraform with staging environment
terraform apply \
  -var="environment=staging" \
  -var="aws_region=us-east-1" \
  -var="vpc_cidr=10.1.0.0/16" \
  -var="db_instance_class=db.t3.small" \
  -var="redis_node_type=cache.t3.small" \
  -var="db_name=vector_db" \
  -var="db_username=postgres" \
  -var='db_password=YourStagingPassword123!' \
  -auto-approve
```

**Note:** 
- Use a different VPC CIDR (e.g., `10.1.0.0/16`) to avoid conflicts
- Use larger instance sizes for staging (closer to production)
- Use a strong, unique password for staging

### Get Staging Database Endpoint

```powershell
terraform output -raw database_endpoint
# Remove :5432 if present
```

### Get Staging ALB URL

```powershell
terraform output alb_dns_name
```

---

## Step 2: Add Staging Secrets to GitHub

Go to: https://github.com/Stanmozolevskiy/Vector/settings/secrets/actions

Add these secrets:

### Required Secrets

1. **`STAGING_DB_CONNECTION_STRING`**
   - Format: `Host=<staging-rds-endpoint>;Port=5432;Database=vector_db;Username=postgres;Password=<password>;SSL Mode=Require;`
   - Example: `Host=staging-postgres.xxxxx.us-east-1.rds.amazonaws.com;Port=5432;Database=vector_db;Username=postgres;Password=YourStagingPassword123!;SSL Mode=Require;`

2. **`STAGING_API_URL`** (for frontend builds)
   - Format: `http://<staging-alb-dns-name>/api`
   - Example: `http://staging-vector-alb-xxxxx.us-east-1.elb.amazonaws.com/api`

---

## Step 3: Update CI/CD Workflows

The workflows are already configured for staging, but need to be updated to:
- Skip migrations (like dev)
- Use correct ECS cluster/service names
- Use staging ECR repositories

### Update Backend Workflow

The staging deployment step needs to be updated to:
1. Skip migrations (run in container)
2. Use staging ECR repository
3. Update staging ECS service

### Update Frontend Workflow

The staging deployment step needs to:
1. Use staging ECR repository
2. Build with `STAGING_API_URL`
3. Update staging ECS service

---

## Step 4: Create Staging Branch and Deploy

```powershell
# Create staging branch from develop
git checkout develop
git pull origin develop
git checkout -b staging
git push origin staging
```

**Or merge develop into staging:**

```powershell
git checkout staging
git merge develop
git push origin staging
```

---

## Step 5: Monitor Deployment

1. Go to: https://github.com/Stanmozolevskiy/Vector/actions
2. Monitor both backend and frontend workflows
3. Verify deployments complete successfully

---

## Staging URLs

After deployment:
- **Frontend:** `http://<staging-alb-dns-name>/`
- **Backend API:** `http://<staging-alb-dns-name>/api`
- **Backend Health:** `http://<staging-alb-dns-name>/api/health`

---

## Differences: Dev vs Staging

| Aspect | Dev | Staging |
|--------|-----|---------|
| VPC CIDR | `10.0.0.0/16` | `10.1.0.0/16` |
| DB Instance | `db.t3.micro` | `db.t3.small` |
| Redis Node | `cache.t3.micro` | `cache.t3.small` |
| ECS CPU/Memory | 256/512 | 512/1024 |
| Backup Retention | 1 day | 7 days |
| Environment Name | `dev` | `staging` |

---

## Cost Considerations

Staging will cost more than dev:
- Larger database instance
- Larger Redis instance
- More ECS resources
- Additional NAT Gateway (if using separate VPC)

**Estimated Monthly Cost:** ~$50-100/month (depending on usage)

---

## Next Steps After Staging

1. Test staging environment thoroughly
2. Set up staging-specific monitoring
3. Configure staging database backups
4. Prepare for production deployment

---

**Note:** Staging infrastructure should mirror production as closely as possible for accurate testing.

