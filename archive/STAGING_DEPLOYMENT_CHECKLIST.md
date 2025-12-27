# Staging Environment Deployment Checklist

Use this checklist to ensure all steps are completed for staging environment setup.

## Pre-Deployment

- [ ] **AWS Account Access**
  - [ ] AWS CLI configured with credentials
  - [ ] AWS account has necessary permissions (EC2, RDS, ECS, ECR, S3, IAM)
  - [ ] Terraform >= 1.0 installed and in PATH

- [ ] **GitHub Configuration**
  - [ ] Repository access confirmed
  - [ ] GitHub Actions enabled
  - [ ] All required secrets configured (see below)

- [ ] **Required GitHub Secrets**
  - [ ] `AWS_ACCESS_KEY_ID`
  - [ ] `AWS_SECRET_ACCESS_KEY`
  - [ ] `JWT_SECRET`
  - [ ] `JWT_ISSUER`
  - [ ] `JWT_AUDIENCE`
  - [ ] `SENDGRID_API_KEY`
  - [ ] `SENDGRID_FROM_EMAIL`
  - [ ] `SENDGRID_FROM_NAME`
  - [ ] `DATABASE_PASSWORD` (will be used in Terraform)
  - [ ] `STAGING_API_URL` (will be set after infrastructure deployment)

## Infrastructure Deployment

- [ ] **Terraform Setup**
  - [ ] Navigate to `infrastructure/terraform`
  - [ ] Run `terraform init`
  - [ ] Create staging workspace: `terraform workspace new staging`
  - [ ] Select staging workspace: `terraform workspace select staging`

- [ ] **Deploy Infrastructure**
  - [ ] Run `deploy-staging.ps1` script OR
  - [ ] Run `terraform plan` with staging variables
  - [ ] Review plan output
  - [ ] Run `terraform apply` with staging variables
  - [ ] Wait for deployment to complete (~15-20 minutes)

- [ ] **Verify Infrastructure**
  - [ ] VPC created: `10.1.0.0/16`
  - [ ] RDS instance created: `staging-postgres`
  - [ ] Redis cluster created: `staging-redis`
  - [ ] S3 bucket created: `staging-vector-user-uploads`
  - [ ] ECR repositories exist: `vector-backend`, `vector-frontend`
  - [ ] ECS cluster created: `staging-vector-cluster`
  - [ ] ALB created and healthy
  - [ ] ECS services created (may be initializing)

- [ ] **Get Infrastructure Outputs**
  - [ ] Run `terraform output alb_dns_name`
  - [ ] Copy ALB DNS name
  - [ ] Update GitHub Secret `STAGING_API_URL`: `http://<alb-dns-name>/api`

## GitHub Configuration

- [ ] **Staging Branch**
  - [ ] Staging branch exists: `git branch -a | grep staging`
  - [ ] If not, create: `git checkout -b staging && git push -u origin staging`

- [ ] **GitHub Environment (Optional)**
  - [ ] Go to Settings → Environments
  - [ ] Create `staging` environment
  - [ ] (Optional) Add protection rules
  - [ ] (Optional) Add required reviewers

## Code Deployment

- [ ] **Merge Code to Staging**
  - [ ] Switch to staging: `git checkout staging`
  - [ ] Merge from develop: `git merge develop`
  - [ ] Push to trigger deployment: `git push origin staging`

- [ ] **Monitor Deployment**
  - [ ] Go to GitHub Actions tab
  - [ ] Watch backend workflow: `Backend CI/CD`
  - [ ] Watch frontend workflow: `Frontend CI/CD`
  - [ ] Verify both workflows complete successfully

## Post-Deployment Verification

- [ ] **ECS Services**
  - [ ] Backend service running: `staging-vector-backend-service`
  - [ ] Frontend service running: `staging-vector-frontend-service`
  - [ ] Both services show "RUNNING" status
  - [ ] Task count matches desired count (usually 1)

- [ ] **Application Health**
  - [ ] Backend health check: `http://<alb-dns-name>/api/health` returns 200
  - [ ] Frontend loads: `http://<alb-dns-name>` shows application
  - [ ] Swagger accessible: `http://<alb-dns-name>/swagger` loads API docs

- [ ] **Database**
  - [ ] Database migrations ran successfully (check ECS logs)
  - [ ] Tables exist in database (connect via bastion if needed)
  - [ ] Can connect to database from ECS tasks

- [ ] **Functionality Tests**
  - [ ] User registration works
  - [ ] User login works
  - [ ] Email verification works (check SendGrid)
  - [ ] Profile page loads
  - [ ] Dashboard loads
  - [ ] API endpoints respond correctly

## Monitoring Setup

- [ ] **CloudWatch Logs**
  - [ ] Backend logs accessible: `/ecs/staging-vector-backend`
  - [ ] Frontend logs accessible: `/ecs/staging-vector-frontend`
  - [ ] Log retention configured (7 days recommended)

- [ ] **CloudWatch Alarms (Recommended)**
  - [ ] ECS CPU utilization alarm
  - [ ] ECS memory utilization alarm
  - [ ] RDS CPU utilization alarm
  - [ ] RDS connection count alarm
  - [ ] ALB response time alarm
  - [ ] ALB 5xx error rate alarm

## Documentation

- [ ] **Update Documentation**
  - [ ] `STAGING_SETUP_GUIDE.md` reviewed
  - [ ] `STAGE1_IMPLEMENTATION.md` updated with staging status
  - [ ] Team notified of staging environment availability
  - [ ] Staging URLs documented

## Security Review

- [ ] **Security Checklist**
  - [ ] Database password is strong and unique
  - [ ] Secrets stored in GitHub Secrets (not in code)
  - [ ] Security groups follow least privilege
  - [ ] RDS and Redis in private subnets
  - [ ] Bastion access restricted (if applicable)
  - [ ] CORS configured correctly

## Final Verification

- [ ] **End-to-End Test**
  - [ ] Create test user account
  - [ ] Verify email received
  - [ ] Complete email verification
  - [ ] Login with verified account
  - [ ] Access profile page
  - [ ] Access dashboard
  - [ ] Test subscription features (if applicable)

- [ ] **Performance Check**
  - [ ] Page load times acceptable (< 3 seconds)
  - [ ] API response times acceptable (< 500ms)
  - [ ] No console errors in browser
  - [ ] No 5xx errors in ALB logs

## Sign-Off

- [ ] **Deployment Approved By:**
  - [ ] DevOps Engineer: _________________ Date: _______
  - [ ] Backend Developer: _________________ Date: _______
  - [ ] Frontend Developer: _________________ Date: _______

- [ ] **Staging Environment Ready:**
  - [ ] All checklist items completed
  - [ ] All tests passing
  - [ ] Documentation updated
  - [ ] Team notified

---

**Deployment Date**: _______________
**Deployed By**: _______________
**Staging URL**: http://<alb-dns-name>
**Staging API URL**: http://<alb-dns-name>/api

---

## Quick Reference

### Staging URLs
- Frontend: `http://<alb-dns-name>`
- Backend API: `http://<alb-dns-name>/api`
- Swagger: `http://<alb-dns-name>/swagger`
- Health Check: `http://<alb-dns-name>/api/health`

### AWS Resources
- ECS Cluster: `staging-vector-cluster`
- Backend Service: `staging-vector-backend-service`
- Frontend Service: `staging-vector-frontend-service`
- RDS: `staging-postgres`
- Redis: `staging-redis`
- ALB: `staging-vector-alb`

### Useful Commands

```bash
# Check ECS services
aws ecs describe-services \
  --cluster staging-vector-cluster \
  --services staging-vector-backend-service staging-vector-frontend-service \
  --region us-east-1

# View backend logs
aws logs tail /ecs/staging-vector-backend --follow --region us-east-1

# Get ALB DNS
terraform output alb_dns_name
```

