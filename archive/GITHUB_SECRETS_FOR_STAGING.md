# GitHub Secrets for Staging Environment

## Required GitHub Secrets

Please add/update these secrets in **GitHub Repository → Settings → Secrets and variables → Actions**:

### ✅ Already Configured (Should Exist)
These secrets should already be configured from the dev environment:

| Secret Name | Current Status | Notes |
|------------|----------------|-------|
| `AWS_ACCESS_KEY_ID` | ✅ Should exist | AWS access key for deployments |
| `AWS_SECRET_ACCESS_KEY` | ✅ Should exist | AWS secret access key |
| `JWT_SECRET` | ✅ Should exist | JWT signing secret (same as dev) |
| `JWT_ISSUER` | ✅ Should exist | JWT issuer (same as dev) |
| `JWT_AUDIENCE` | ✅ Should exist | JWT audience (same as dev) |
| `SENDGRID_API_KEY` | ✅ Should exist | SendGrid API key for emails |
| `SENDGRID_FROM_EMAIL` | ✅ Should exist | SendGrid sender email |
| `SENDGRID_FROM_NAME` | ✅ Should exist | SendGrid sender name |

### ⚠️ REQUIRED: Add/Update This Secret

| Secret Name | Value | Description |
|------------|-------|-------------|
| **`STAGING_API_URL`** | `http://staging-vector-alb-2020798622.us-east-1.elb.amazonaws.com/api` | Backend API URL for frontend build |

**Action Required:** Add or update `STAGING_API_URL` in GitHub Secrets with the value above.

---

## Deployment Summary

### ✅ Infrastructure Deployed Successfully

**ALB DNS Name:** `staging-vector-alb-2020798622.us-east-1.elb.amazonaws.com`

**Resources Created:**
- ✅ VPC: `vpc-0b965811b0fc3f9b3` (10.1.0.0/16)
- ✅ ECS Cluster: `staging-vector-cluster`
- ✅ RDS PostgreSQL: `staging-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432`
- ✅ Redis: `staging-redis` (cache.t3.small, Multi-AZ)
- ✅ S3 Bucket: `staging-vector-user-uploads`
- ✅ ECR Repositories: `vector-backend`, `vector-frontend`
- ✅ ECS Services:
  - `staging-vector-backend-service` (2 tasks)
  - `staging-vector-frontend-service` (2 tasks)
- ✅ Bastion Host: `i-0e13c7f756f58745b` (Public IP: 44.199.200.136)

### Database Information

**Database Endpoint:** `staging-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432`  
**Database Name:** `vector_db`  
**Database Username:** `postgres`  
**Database Password:** `GDhf5jnbRuTzcyaqMUdJvC0O3oL7IKtB` (stored securely in Terraform state)

### Bastion Host Access

**SSH Key Location:** `C:\Users\stanm\.ssh\vector_staging_bastion`  
**Public IP:** `44.199.200.136`  
**SSH Command:** `ssh -i ~/.ssh/vector_staging_bastion ec2-user@44.199.200.136`  
**SSM Command:** `aws ssm start-session --target i-0e13c7f756f58745b --region us-east-1`

---

## Next Steps

1. **Add GitHub Secret:**
   - Go to GitHub → Settings → Secrets and variables → Actions
   - Add/Update: `STAGING_API_URL` = `http://staging-vector-alb-2020798622.us-east-1.elb.amazonaws.com/api`

2. **Deploy Code to Staging:**
   ```bash
   git checkout staging
   git merge develop
   git push origin staging
   ```
   This will trigger automatic deployment via GitHub Actions.

3. **Verify Deployment:**
   - Backend Health: `http://staging-vector-alb-2020798622.us-east-1.elb.amazonaws.com/api/health`
   - Frontend: `http://staging-vector-alb-2020798622.us-east-1.elb.amazonaws.com`
   - Swagger: `http://staging-vector-alb-2020798622.us-east-1.elb.amazonaws.com/swagger`

---

**Deployment Completed:** December 9, 2025  
**Status:** ✅ Infrastructure ready, waiting for GitHub Secret update

