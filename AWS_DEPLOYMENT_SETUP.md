# AWS Deployment Setup - Complete ✅

## Summary

AWS deployment infrastructure has been created using ECS Fargate with Application Load Balancer. The CI/CD pipeline is configured to automatically build and deploy Docker images to ECS.

**Date:** November 29, 2024

---

## ✅ Completed Infrastructure

### 1. ECS (Elastic Container Service) Module
- ✅ ECS Cluster created
- ✅ Task Execution Role with proper permissions
- ✅ Task Role for application AWS service access
- ✅ Security groups for ECS tasks
- ✅ CloudWatch log group for container logs
- ✅ Task definition for backend API
- ✅ ECS Service with load balancer integration

### 2. Application Load Balancer (ALB) Module
- ✅ Application Load Balancer created
- ✅ Target group for backend API
- ✅ HTTP listener (dev) and HTTPS listener (staging/prod)
- ✅ Security group for ALB
- ✅ Health checks configured

### 3. ECR (Elastic Container Registry) Module
- ✅ ECR repositories for backend and frontend
- ✅ Lifecycle policies (keep last 10 images)
- ✅ Image scanning enabled (staging/prod)

### 4. CI/CD Pipeline Updates
- ✅ ECR login and image push
- ✅ ECS service update for deployments
- ✅ Database migration automation

---

## Infrastructure Architecture

```
Internet
   ↓
Application Load Balancer (ALB)
   ↓
ECS Service (Fargate)
   ↓
Backend Container (Port 80)
   ↓
RDS PostgreSQL (Port 5432)
Redis ElastiCache (Port 6379)
S3 Bucket (User uploads)
```

---

## Deployment Process

### Step 1: Deploy Infrastructure with Terraform

```powershell
cd infrastructure/terraform
terraform init
terraform plan -var="environment=dev" -var="db_password=YOUR_PASSWORD"
terraform apply -var="environment=dev" -var="db_password=YOUR_PASSWORD"
```

This will create:
- ECS cluster
- ALB
- ECR repositories
- ECS task definition
- ECS service

### Step 2: Get ECR Repository URL

```powershell
terraform output
# Note the ECR repository URL for backend
```

Or:
```powershell
aws ecr describe-repositories --repository-names vector-backend --region us-east-1
```

### Step 3: Update ECS Task Definition

The task definition needs the database connection string. You have two options:

**Option A: Use Environment Variables (Current)**
- Update task definition to include connection string as environment variable
- Less secure but simpler for dev

**Option B: Use AWS Secrets Manager (Recommended for Production)**
- Store connection strings in Secrets Manager
- Reference secrets in task definition
- More secure

### Step 4: CI/CD Pipeline

When you push to `develop` branch:
1. Pipeline builds Docker image
2. Pushes to ECR
3. Updates ECS service (forces new deployment)
4. Runs database migrations
5. New containers start with new image

---

## Current Status

### ✅ Infrastructure Created
- ECS cluster: `dev-vector-cluster`
- ALB: `dev-vector-alb`
- ECR repositories: `vector-backend`, `vector-frontend`
- Task definition: `dev-vector-backend`
- ECS service: `dev-vector-backend-service`

### ⏳ Pending Configuration

1. **Task Definition Environment Variables**
   - Need to add database connection string
   - Need to add Redis connection string
   - Can be done via Terraform variables or Secrets Manager

2. **First Deployment**
   - Need to push initial Docker image to ECR
   - ECS service will pull and deploy

3. **Health Checks**
   - ALB health checks configured
   - Container health check configured
   - Verify endpoints are accessible

---

## Next Steps

### Immediate

1. **Deploy Infrastructure:**
   ```powershell
   cd infrastructure/terraform
   terraform apply -var="environment=dev" -var="db_password=YOUR_PASSWORD"
   ```

2. **Update Task Definition with Connection Strings:**
   - Option 1: Add to Terraform variables
   - Option 2: Use AWS Secrets Manager
   - Option 3: Update manually in AWS Console (for testing)

3. **Test Pipeline:**
   - Push to `develop` branch
   - Watch GitHub Actions
   - Verify image is pushed to ECR
   - Check ECS service deployment

### Future Enhancements

1. **Secrets Management**
   - Migrate to AWS Secrets Manager
   - Update task definition to use secrets

2. **Auto Scaling**
   - Configure ECS auto scaling
   - Set up CloudWatch alarms

3. **SSL/TLS Certificates**
   - Request ACM certificate
   - Configure HTTPS listener

4. **Frontend Deployment**
   - Set up S3 + CloudFront for frontend
   - Or deploy frontend to ECS

---

## Accessing Your Application

After deployment, get the ALB DNS name:

```powershell
cd infrastructure/terraform
terraform output alb_dns_name
```

Then access:
- **API:** `http://<alb-dns-name>/`
- **Swagger:** `http://<alb-dns-name>/swagger` (dev only)

---

## Troubleshooting

### ECS Service Not Starting

**Check:**
- Task definition is correct
- Container image exists in ECR
- Security groups allow traffic
- Subnets have internet access (via NAT)

### Cannot Connect to Database

**Check:**
- RDS security group allows ECS security group
- Connection string is correct
- Database is in same VPC

### ALB Health Checks Failing

**Check:**
- Container is listening on port 80
- Health check path is correct (`/`)
- Security groups allow ALB → ECS traffic

---

## Cost Estimate

**ECS Fargate (Dev):**
- 1 task × 0.25 vCPU × 0.5 GB = ~$10/month
- ALB = ~$20/month
- Data transfer = variable

**Total Additional Cost:** ~$30-40/month for dev environment

---

## Files Created

1. `infrastructure/terraform/modules/ecs/` - ECS infrastructure
2. `infrastructure/terraform/modules/alb/` - Load balancer
3. `infrastructure/terraform/modules/ecr/` - Container registry
4. Updated `.github/workflows/backend.yml` - Deployment automation

---

**Status:** Infrastructure ready. Deploy with `terraform apply` and test the pipeline!

