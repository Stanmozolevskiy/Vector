# Staging Environment Setup Guide

Complete guide for setting up and deploying the staging environment on AWS with GitHub Actions CI/CD.

## Prerequisites

- ✅ AWS CLI configured with appropriate credentials
- ✅ Terraform >= 1.0 installed
- ✅ GitHub repository access
- ✅ AWS account with appropriate permissions
- ✅ GitHub Secrets configured (see below)

## Step 1: Configure GitHub Secrets

Go to **GitHub Repository → Settings → Secrets and variables → Actions** and ensure these secrets exist:

### Required Secrets

| Secret Name | Description | Example |
|------------|-------------|---------|
| `AWS_ACCESS_KEY_ID` | AWS access key for deployments | `AKIAIOSFODNN7EXAMPLE` |
| `AWS_SECRET_ACCESS_KEY` | AWS secret access key | `wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY` |
| `STAGING_API_URL` | Backend API URL for frontend build | `http://staging-vector-alb-xxxxx.us-east-1.elb.amazonaws.com/api` |
| `JWT_SECRET` | JWT signing secret (same as dev) | `your-super-secret-key` |
| `JWT_ISSUER` | JWT issuer (same as dev) | `Vector` |
| `JWT_AUDIENCE` | JWT audience (same as dev) | `Vector` |
| `SENDGRID_API_KEY` | SendGrid API key for emails | `SG.xxxxxxxxxxxxx` |
| `SENDGRID_FROM_EMAIL` | SendGrid sender email | `noreply@vector.com` |
| `SENDGRID_FROM_NAME` | SendGrid sender name | `Vector` |
| `DATABASE_PASSWORD` | PostgreSQL database password | `SecurePassword123!` |

**Note:** `STAGING_API_URL` will be set after infrastructure deployment (Step 2).

## Step 2: Deploy Staging Infrastructure

### Option A: Using PowerShell Script (Recommended)

```powershell
cd infrastructure/terraform
.\deploy-staging.ps1 `
  -DbPassword "YourSecurePassword123!" `
  -BastionSshKey "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQC..." `
  -SendGridApiKey "SG.xxxxxxxxxxxxx" `
  -SendGridFromEmail "noreply@vector.com" `
  -SendGridFromName "Vector"
```

### Option B: Manual Terraform Deployment

```powershell
cd infrastructure/terraform

# Initialize Terraform
terraform init

# Create staging workspace
terraform workspace new staging
terraform workspace select staging

# Plan deployment
terraform plan `
  -var="environment=staging" `
  -var="vpc_cidr=10.1.0.0/16" `
  -var="db_instance_class=db.t3.small" `
  -var="redis_node_type=cache.t3.small" `
  -var="db_password=YourSecurePassword123!" `
  -var="bastion_ssh_public_key=ssh-rsa AAAAB3..." `
  -var="sendgrid_api_key=SG.xxx" `
  -var="sendgrid_from_email=noreply@vector.com" `
  -var="sendgrid_from_name=Vector"

# Apply configuration
terraform apply `
  -var="environment=staging" `
  -var="vpc_cidr=10.1.0.0/16" `
  -var="db_instance_class=db.t3.small" `
  -var="redis_node_type=cache.t3.small" `
  -var="db_password=YourSecurePassword123!" `
  -var="bastion_ssh_public_key=ssh-rsa AAAAB3..." `
  -var="sendgrid_api_key=SG.xxx" `
  -var="sendgrid_from_email=noreply@vector.com" `
  -var="sendgrid_from_name=Vector"
```

### Infrastructure Created

After deployment, the following resources will be created:

- **VPC**: `10.1.0.0/16` with public/private subnets
- **RDS PostgreSQL**: `staging-postgres` (db.t3.small, Multi-AZ)
- **ElastiCache Redis**: `staging-redis` (cache.t3.small)
- **S3 Bucket**: `staging-vector-user-uploads`
- **ECR Repositories**: `vector-backend`, `vector-frontend`
- **ECS Cluster**: `staging-vector-cluster`
- **Application Load Balancer**: `staging-vector-alb`
- **ECS Services**: 
  - `staging-vector-backend-service`
  - `staging-vector-frontend-service`
- **Bastion Host**: For secure database access

## Step 3: Get Infrastructure Outputs

After deployment, get the ALB DNS name:

```powershell
cd infrastructure/terraform
terraform output alb_dns_name
```

**Example output:**
```
alb_dns_name = "staging-vector-alb-1234567890.us-east-1.elb.amazonaws.com"
```

## Step 4: Update GitHub Secrets

1. Go to **GitHub Repository → Settings → Secrets and variables → Actions**
2. Add/Update `STAGING_API_URL`:
   - Value: `http://<alb-dns-name>/api`
   - Example: `http://staging-vector-alb-1234567890.us-east-1.elb.amazonaws.com/api`

## Step 5: Configure GitHub Environment (Optional)

1. Go to **GitHub Repository → Settings → Environments**
2. Click **New environment**
3. Name: `staging`
4. (Optional) Add protection rules:
   - **Required reviewers**: Add team members who must approve deployments
   - **Deployment branches**: Restrict to `staging` branch only
   - **Wait timer**: Add delay before deployment (optional)

## Step 6: Create Staging Branch

The staging branch should already exist. If not:

```bash
git checkout -b staging
git push -u origin staging
```

## Step 7: Deploy Code to Staging

### Option A: Merge from Develop (Recommended)

```bash
# Switch to staging branch
git checkout staging

# Merge latest changes from develop
git merge develop

# Push to trigger deployment
git push origin staging
```

### Option B: Direct Push to Staging

```bash
# Make changes and commit
git checkout staging
# ... make changes ...
git add .
git commit -m "Your commit message"
git push origin staging
```

### What Happens Automatically

When you push to `staging`, GitHub Actions will:

1. **Build and Test**
   - Run backend unit tests
   - Run frontend linting
   - Build Docker images

2. **Deploy to AWS**
   - Push Docker images to ECR
   - Update ECS services
   - Wait for services to stabilize

3. **Database Migrations**
   - Run automatically when backend container starts
   - Configured in `Program.cs`

## Step 8: Verify Deployment

### Check ECS Services

```bash
aws ecs describe-services \
  --cluster staging-vector-cluster \
  --services staging-vector-backend-service staging-vector-frontend-service \
  --region us-east-1
```

### Check Application Health

- **Backend Health**: `http://<alb-dns-name>/api/health`
- **Frontend**: `http://<alb-dns-name>`
- **Swagger API Docs**: `http://<alb-dns-name>/swagger`

### Check ECS Task Logs

```bash
# Get task ARN
TASK_ARN=$(aws ecs list-tasks \
  --cluster staging-vector-cluster \
  --service-name staging-vector-backend-service \
  --region us-east-1 \
  --query 'taskArns[0]' \
  --output text)

# Get logs
aws logs tail /ecs/staging-vector-backend \
  --follow \
  --region us-east-1
```

## Step 9: Database Access (If Needed)

### Via Bastion Host

```bash
# Start SSM session to bastion
aws ssm start-session \
  --target <bastion-instance-id> \
  --region us-east-1

# From bastion, connect to RDS
psql -h staging-postgres.xxxxx.us-east-1.rds.amazonaws.com \
  -U postgres \
  -d vector_db

# Check tables
\dt

# Check migrations
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
```

## Environment Comparison

| Component | Dev | Staging | Production |
|-----------|-----|---------|------------|
| **VPC CIDR** | 10.0.0.0/16 | 10.1.0.0/16 | 10.2.0.0/16 |
| **RDS Instance** | db.t3.micro | db.t3.small | db.t3.medium+ |
| **Redis Node** | cache.t3.micro | cache.t3.small | cache.t3.medium+ |
| **Backup Retention** | 1 day | 7 days | 7+ days |
| **Multi-AZ** | No | Yes | Yes |
| **Auto Failover** | No | Yes | Yes |
| **ECS Cluster** | dev-vector-cluster | staging-vector-cluster | prod-vector-cluster |

## Troubleshooting

### ECS Service Not Starting

1. **Check CloudWatch Logs:**
   ```bash
   aws logs tail /ecs/staging-vector-backend --follow --region us-east-1
   ```

2. **Check Security Groups:**
   - Verify ECS security group allows outbound to RDS/Redis
   - Verify RDS security group allows inbound from ECS security group
   - Verify Redis security group allows inbound from ECS security group

3. **Check Task Definition:**
   - Verify environment variables are set correctly
   - Check container health check configuration

### Database Connection Issues

1. **Verify RDS Security Group:**
   - Must allow inbound PostgreSQL (5432) from ECS security group
   - RDS must be in private subnet

2. **Check Connection String:**
   - Verify `ConnectionStrings__DefaultConnection` in ECS task definition
   - Format: `Host=<rds-endpoint>;Port=5432;Database=vector_db;Username=postgres;Password=<password>`

3. **Test Connection from Bastion:**
   ```bash
   psql -h <rds-endpoint> -U postgres -d vector_db
   ```

### Frontend Can't Connect to Backend

1. **Verify VITE_API_URL:**
   - Check GitHub Secret `STAGING_API_URL`
   - Must match ALB DNS name
   - Format: `http://<alb-dns-name>/api`

2. **Check ALB Target Group:**
   ```bash
   aws elbv2 describe-target-health \
     --target-group-arn <target-group-arn> \
     --region us-east-1
   ```

3. **Check CORS Configuration:**
   - Verify backend CORS allows frontend origin
   - Check `Program.cs` CORS configuration

### GitHub Actions Deployment Fails

1. **Check Workflow Logs:**
   - Go to **Actions** tab in GitHub
   - Click on failed workflow run
   - Review error messages

2. **Verify GitHub Secrets:**
   - Ensure all required secrets are set
   - Check secret names match workflow expectations

3. **Check AWS Permissions:**
   - Verify AWS credentials have necessary permissions
   - Required: ECR, ECS, CloudWatch Logs, IAM (for task execution role)

## Monitoring

### CloudWatch Alarms (Recommended)

Set up alarms for:
- ECS service CPU utilization > 80%
- ECS service memory utilization > 80%
- RDS CPU utilization > 80%
- RDS connection count > 80% of max
- ALB target response time > 1 second
- ALB 5xx error rate > 1%

### Log Aggregation

- Backend logs: `/ecs/staging-vector-backend`
- Frontend logs: `/ecs/staging-vector-frontend`
- ALB access logs: Configure in ALB settings

## Cost Optimization

Staging environment costs:
- **RDS**: ~$50-100/month (db.t3.small, Multi-AZ)
- **Redis**: ~$15-30/month (cache.t3.small)
- **ECS**: ~$30-50/month (2 services, minimal traffic)
- **ALB**: ~$20/month (base cost)
- **Data Transfer**: Variable based on usage
- **S3**: Minimal (storage only)

**Total Estimated**: ~$115-200/month

## Security Best Practices

1. **Database Password:**
   - Use strong, unique password for staging
   - Rotate passwords regularly
   - Store in GitHub Secrets (not in code)

2. **Bastion Access:**
   - Restrict SSH access to specific IPs
   - Use AWS Systems Manager Session Manager (recommended)
   - Rotate SSH keys regularly

3. **Secrets Management:**
   - Never commit secrets to git
   - Use GitHub Secrets for CI/CD
   - Use AWS Secrets Manager for production (future)

4. **Network Security:**
   - RDS and Redis in private subnets
   - ECS tasks in private subnets
   - ALB in public subnets
   - Security groups with least privilege

## Next Steps

1. ✅ Set up CloudWatch alarms
2. ✅ Configure log retention policies
3. ✅ Set up staging database backups
4. ✅ Create staging user accounts for testing
5. ✅ Configure staging-specific environment variables
6. ✅ Set up staging monitoring dashboard

## Support

For issues or questions:
1. Check CloudWatch logs
2. Review GitHub Actions workflow logs
3. Verify infrastructure with `terraform plan`
4. Check AWS Service Health Dashboard

---

**Last Updated**: December 2025
**Status**: ✅ Ready for Deployment

