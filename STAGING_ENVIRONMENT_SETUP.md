# Staging Environment Setup Guide

This guide covers setting up and deploying to the staging environment on AWS.

## Prerequisites

- AWS CLI configured with appropriate credentials
- Terraform >= 1.0 installed
- GitHub repository with staging branch
- GitHub Secrets configured (AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY)

## Step 1: Create Staging Branch

```bash
git checkout -b staging
git push origin staging
```

## Step 2: Configure GitHub Environment

1. Go to GitHub repository → Settings → Environments
2. Create new environment: `staging`
3. Add protection rules if needed (optional):
   - Required reviewers
   - Deployment branches (only staging branch)

## Step 3: Deploy Staging Infrastructure

### 3.1 Configure Terraform Variables

Create `infrastructure/terraform/terraform.tfvars.staging`:

```hcl
aws_region      = "us-east-1"
environment     = "staging"
vpc_cidr        = "10.1.0.0/16"  # Different CIDR from dev (10.0.0.0/16)
db_instance_class = "db.t3.small"  # Slightly larger for staging
redis_node_type = "cache.t3.small"
db_password     = "YOUR_SECURE_PASSWORD_HERE"  # Use a strong password
bastion_ssh_public_key = "YOUR_SSH_PUBLIC_KEY"
sendgrid_api_key = "YOUR_SENDGRID_API_KEY"
sendgrid_from_email = "your-email@example.com"
sendgrid_from_name = "Vector"
```

### 3.2 Deploy Infrastructure

```bash
cd infrastructure/terraform
terraform init
terraform workspace new staging  # Create staging workspace
terraform workspace select staging
terraform plan -var-file=terraform.tfvars.staging
terraform apply -var-file=terraform.tfvars.staging
```

**Note:** This will create:
- VPC with public/private subnets (10.1.0.0/16)
- RDS PostgreSQL instance (staging-postgres)
- ElastiCache Redis cluster (staging-redis)
- S3 bucket (staging-vector-user-uploads)
- ECR repositories (vector-backend, vector-frontend)
- ECS cluster (staging-vector-cluster)
- Application Load Balancer
- ECS services for backend and frontend

### 3.3 Get Infrastructure Outputs

After deployment, get the ALB DNS name:

```bash
terraform output alb_dns_name
```

Update GitHub Secrets:
- `STAGING_API_URL`: `http://<alb-dns-name>/api`

## Step 4: Configure GitHub Secrets for Staging

Add/update the following secrets in GitHub (Settings → Secrets and variables → Actions):

- `AWS_ACCESS_KEY_ID` (already exists)
- `AWS_SECRET_ACCESS_KEY` (already exists)
- `STAGING_API_URL`: Backend API URL for frontend build

## Step 5: Deploy Code to Staging

### Option A: Automatic Deployment (Recommended)

1. Merge changes from `develop` to `staging`:
   ```bash
   git checkout staging
   git merge develop
   git push origin staging
   ```

2. GitHub Actions will automatically:
   - Build and test backend/frontend
   - Build Docker images
   - Push to ECR
   - Deploy to ECS

### Option B: Manual Deployment

If you need to deploy manually:

```bash
# Backend
cd backend/Vector.Api
dotnet publish -c Release

# Build and push Docker image
cd ../..
docker build -t vector-backend:staging -f docker/Dockerfile.backend .
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <ecr-registry>
docker tag vector-backend:staging <ecr-registry>/vector-backend:staging
docker push <ecr-registry>/vector-backend:staging

# Update ECS service
aws ecs update-service \
  --cluster staging-vector-cluster \
  --service staging-vector-backend-service \
  --force-new-deployment \
  --region us-east-1
```

## Step 6: Verify Deployment

1. **Check ECS Services:**
   ```bash
   aws ecs describe-services \
     --cluster staging-vector-cluster \
     --services staging-vector-backend-service staging-vector-frontend-service \
     --region us-east-1
   ```

2. **Check Application Health:**
   - Backend: `http://<alb-dns-name>/api/health`
   - Frontend: `http://<alb-dns-name>`
   - Swagger: `http://<alb-dns-name>/swagger`

3. **Check Database Migrations:**
   - Review ECS task logs to ensure migrations ran successfully
   - Connect to RDS and verify tables exist

## Step 7: Database Migrations

Database migrations run automatically when the backend container starts (configured in `Program.cs`).

To verify migrations:
```bash
# Connect via bastion host
aws ssm start-session --target <bastion-instance-id> --region us-east-1

# From bastion, connect to RDS
psql -h staging-postgres.xxxxx.us-east-1.rds.amazonaws.com -U postgres -d vector_db

# Check migrations
\dt
```

## Troubleshooting

### ECS Service Not Starting
- Check CloudWatch logs for errors
- Verify security group rules allow ECS → RDS/Redis
- Check task definition environment variables

### Database Connection Issues
- Verify RDS security group allows ECS security group
- Check connection string in ECS task definition
- Ensure RDS is in private subnet

### Frontend Can't Connect to Backend
- Verify `VITE_API_URL` in frontend build
- Check ALB target group health
- Ensure CORS is configured correctly

## Environment Differences

| Feature | Dev | Staging | Production |
|---------|-----|---------|------------|
| VPC CIDR | 10.0.0.0/16 | 10.1.0.0/16 | 10.2.0.0/16 |
| RDS Instance | db.t3.micro | db.t3.small | db.t3.medium+ |
| Redis Node | cache.t3.micro | cache.t3.small | cache.t3.medium+ |
| Backup Retention | 1 day | 7 days | 7+ days |
| Multi-AZ | No | Yes | Yes |
| Auto Failover | No | Yes | Yes |

## Next Steps

1. Set up monitoring and alerting for staging
2. Configure staging-specific environment variables
3. Set up staging database backups
4. Create staging user accounts for testing

