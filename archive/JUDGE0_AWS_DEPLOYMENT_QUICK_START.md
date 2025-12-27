# Judge0 AWS Deployment - Quick Start Guide

## Overview

Judge0 is deployed as an EC2 instance running Docker Compose. This provides the Linux cgroups support needed for code execution isolation.

## Architecture

```
Backend (ECS Fargate) → Judge0 (EC2 Instance) → Docker Containers
                                              ├─ PostgreSQL
                                              ├─ Redis  
                                              └─ RabbitMQ
```

## Deployment Steps

### 1. Update Terraform Variables

Add to `infrastructure/terraform/terraform.tfvars`:

```hcl
# Judge0 Configuration
judge0_instance_type      = "t3.medium"  # Recommended for code execution
judge0_use_elastic_ip     = false        # Set true for stable IP
judge0_db_password         = "your_secure_password_here"
judge0_rabbitmq_password   = "your_secure_password_here"
```

### 2. Deploy with Terraform

```powershell
cd infrastructure/terraform

# Initialize (if needed)
terraform init

# Plan deployment
terraform plan -var="environment=dev" `
  -var="db_password=YOUR_DB_PASSWORD" `
  -var="judge0_db_password=YOUR_JUDGE0_DB_PASSWORD" `
  -var="judge0_rabbitmq_password=YOUR_RABBITMQ_PASSWORD"

# Apply changes
terraform apply -var="environment=dev" `
  -var="db_password=YOUR_DB_PASSWORD" `
  -var="judge0_db_password=YOUR_JUDGE0_DB_PASSWORD" `
  -var="judge0_rabbitmq_password=YOUR_RABBITMQ_PASSWORD"
```

### 3. Get Judge0 Endpoint

```powershell
terraform output judge0_endpoint
# Example: http://10.0.1.50:2358
```

### 4. Verify Backend Configuration

The backend is automatically configured with `Judge0__BaseUrl` environment variable pointing to Judge0's private IP.

Check ECS task definition to verify:
```powershell
aws ecs describe-task-definition --task-definition dev-vector-backend --query 'taskDefinition.containerDefinitions[0].environment'
```

### 5. Wait for Judge0 to Start

Judge0 takes 2-3 minutes to install and start. Check CloudWatch logs:

```powershell
# View logs
aws logs tail /ec2/dev-vector-judge0 --follow

# Or check via SSH (via bastion)
ssh ubuntu@<judge0-private-ip>
sudo docker ps
sudo docker logs judge0
```

### 6. Test Code Execution

Test from backend API or directly:

```powershell
# Get Judge0 private IP
$judge0Ip = (terraform output -raw judge0_private_ip)

# Test via backend (after deployment)
# Or test directly (from bastion)
curl -X POST http://$judge0Ip:2358/submissions?base64_encoded=false&wait=true `
  -H "Content-Type: application/json" `
  -d '{"source_code":"print(\"Hello!\");","language_id":71,"stdin":""}'
```

## What Gets Created

### EC2 Instance
- **Type**: t3.medium (2 vCPU, 4GB RAM)
- **OS**: Ubuntu 22.04 LTS
- **Location**: Private subnet
- **Storage**: 30GB encrypted EBS volume

### Docker Services (on EC2)
- **PostgreSQL**: Judge0 database
- **Redis**: Caching and queues
- **RabbitMQ**: Message queue
- **Judge0 API**: Code execution service

### Security
- **Security Group**: Allows access from ECS tasks only
- **Private Subnet**: No public internet access
- **Encrypted Storage**: EBS volumes encrypted

### Networking
- **Private IP**: Accessible from ECS tasks
- **Port**: 2358 (Judge0 API)
- **No Public IP**: More secure

## Cost Estimate

### Dev Environment
- **EC2 t3.medium**: ~$30/month
- **EBS 30GB**: ~$3/month
- **Total**: ~$33/month

### Production
- **EC2 t3.large+**: ~$60-120/month
- **EBS 50GB+**: ~$5-10/month
- **Total**: ~$65-130/month

## Troubleshooting

### Judge0 Not Starting

1. **Check CloudWatch Logs**:
   ```powershell
   aws logs tail /ec2/dev-vector-judge0 --follow
   ```

2. **SSH to Instance** (via bastion):
   ```powershell
   # Get bastion IP
   terraform output bastion_public_ip
   
   # SSH to bastion, then to Judge0
   ssh ubuntu@<judge0-private-ip>
   
   # Check Docker
   sudo docker ps -a
   sudo docker logs judge0
   ```

3. **Check User Data Execution**:
   ```bash
   sudo cat /var/log/cloud-init-output.log
   ```

### Backend Cannot Connect

1. **Verify Security Group Rules**:
   - ECS security group should allow egress to Judge0
   - Judge0 security group should allow ingress from ECS

2. **Check Environment Variable**:
   ```powershell
   aws ecs describe-task-definition --task-definition dev-vector-backend `
     --query 'taskDefinition.containerDefinitions[0].environment[?name==`Judge0__BaseUrl`]'
   ```

3. **Test Connectivity**:
   ```powershell
   # From backend ECS task (via bastion)
   curl http://<judge0-private-ip>:2358/languages
   ```

## Maintenance

### Update Judge0

```bash
# SSH to Judge0 instance
cd /opt/judge0
sudo docker compose pull
sudo docker compose up -d
```

### Backup Database

```bash
# On Judge0 instance
sudo docker exec judge0-db pg_dump -U judge0 judge0 > backup.sql
```

## Next Steps

1. Deploy infrastructure with Terraform
2. Wait 2-3 minutes for Judge0 to start
3. Verify Judge0 is accessible
4. Test code execution from backend
5. Monitor CloudWatch logs

## Files Created

- `infrastructure/terraform/modules/judge0/` - Judge0 Terraform module
- `JUDGE0_AWS_DEPLOYMENT.md` - Detailed documentation
- `JUDGE0_AWS_DEPLOYMENT_QUICK_START.md` - This file

