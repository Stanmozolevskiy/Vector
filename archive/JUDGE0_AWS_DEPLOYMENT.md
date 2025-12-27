# Judge0 AWS Deployment Guide

## Overview

This guide explains how to deploy Judge0 to AWS for code execution. Judge0 requires privileged containers and Linux cgroups, which don't work in Docker on Windows or ECS Fargate. The solution is to deploy Judge0 on an EC2 instance with Docker.

## Architecture

```
Backend (ECS Fargate)
    ↓
Judge0 (EC2 Instance) - Private Subnet
    ↓
PostgreSQL, Redis, RabbitMQ (Docker on EC2)
```

## Prerequisites

- AWS account with appropriate permissions
- Terraform installed
- AWS CLI configured
- Existing Vector infrastructure deployed

## Deployment Steps

### 1. Update Terraform Variables

Add Judge0 configuration to `infrastructure/terraform/terraform.tfvars`:

```hcl
# Judge0 Configuration
judge0_instance_type      = "t3.medium"  # Recommended: t3.medium (2 vCPU, 4GB RAM)
judge0_use_elastic_ip     = false        # Set to true for stable IP
judge0_db_password         = "your_secure_password_here"
judge0_rabbitmq_password   = "your_secure_password_here"
```

### 2. Deploy Infrastructure

```powershell
cd infrastructure/terraform

# Initialize Terraform (if not already done)
terraform init

# Plan the deployment
terraform plan -var="environment=dev" -var="db_password=YOUR_DB_PASSWORD" -var="judge0_db_password=YOUR_JUDGE0_DB_PASSWORD" -var="judge0_rabbitmq_password=YOUR_RABBITMQ_PASSWORD"

# Apply the changes
terraform apply -var="environment=dev" -var="db_password=YOUR_DB_PASSWORD" -var="judge0_db_password=YOUR_JUDGE0_DB_PASSWORD" -var="judge0_rabbitmq_password=YOUR_RABBITMQ_PASSWORD"
```

### 3. Verify Deployment

After deployment, check the outputs:

```powershell
terraform output judge0_endpoint
terraform output judge0_private_ip
terraform output judge0_instance_id
```

### 4. Wait for Judge0 to Start

Judge0 installation takes 2-3 minutes. Check CloudWatch logs:

```powershell
# Get log stream
aws logs describe-log-streams --log-group-name "/ec2/dev-vector-judge0" --order-by LastEventTime --descending --max-items 1

# View logs
aws logs tail /ec2/dev-vector-judge0 --follow
```

Or SSH into the instance:

```powershell
# Get bastion IP
terraform output bastion_public_ip

# SSH to bastion, then to Judge0 instance
ssh -i ~/.ssh/your-key.pem ubuntu@<bastion-ip>
ssh ubuntu@<judge0-private-ip>

# Check Judge0 status
sudo docker ps
sudo docker logs judge0
curl http://localhost:2358/languages
```

### 5. Update Backend Configuration

The backend is automatically configured via environment variable `Judge0__BaseUrl` set to the Judge0 private IP endpoint.

Verify in ECS task definition that the environment variable is set correctly.

## Configuration Details

### Judge0 Instance

- **Instance Type**: `t3.medium` (recommended)
  - 2 vCPU, 4GB RAM
  - Sufficient for code execution workloads
- **Subnet**: Private subnet (for security)
- **Security Group**: Allows access from ECS tasks only
- **Storage**: 30GB GP3 encrypted volume

### Judge0 Services (Docker Compose on EC2)

- **PostgreSQL**: Database for Judge0
- **Redis**: Caching and queue management
- **RabbitMQ**: Message queue for job processing
- **Judge0 API**: Main service on port 2358

### Network Configuration

- Judge0 is in a private subnet
- Accessible only from ECS tasks (backend)
- No public internet access (more secure)
- Can be accessed via bastion host for troubleshooting

## Cost Estimation

### Dev Environment
- **EC2 Instance (t3.medium)**: ~$30/month
- **EBS Volume (30GB)**: ~$3/month
- **Data Transfer**: Minimal (internal VPC)
- **Total**: ~$33/month

### Production Environment
- **EC2 Instance (t3.large or larger)**: ~$60-120/month
- **EBS Volume (50GB+)**: ~$5-10/month
- **Total**: ~$65-130/month

## Security Considerations

1. **Private Subnet**: Judge0 is not publicly accessible
2. **Security Groups**: Only ECS tasks can access Judge0
3. **Encrypted Storage**: EBS volumes are encrypted
4. **Strong Passwords**: Use secure passwords for Judge0 DB and RabbitMQ
5. **IAM Roles**: Minimal permissions for EC2 instance

## Troubleshooting

### Judge0 Not Starting

1. **Check CloudWatch Logs**:
   ```powershell
   aws logs tail /ec2/dev-vector-judge0 --follow
   ```

2. **SSH to Instance**:
   ```powershell
   # Via bastion
   ssh ubuntu@<judge0-private-ip>
   
   # Check Docker
   sudo docker ps -a
   sudo docker logs judge0
   ```

3. **Check User Data Script**:
   ```powershell
   # View user data execution log
   sudo cat /var/log/cloud-init-output.log
   ```

### Backend Cannot Connect to Judge0

1. **Verify Security Group Rules**:
   - ECS security group should have egress rule to Judge0 security group
   - Judge0 security group should allow ingress from ECS security group

2. **Check Network Connectivity**:
   ```powershell
   # From backend ECS task (via bastion)
   curl http://<judge0-private-ip>:2358/languages
   ```

3. **Verify Environment Variable**:
   - Check ECS task definition for `Judge0__BaseUrl`
   - Should be: `http://<judge0-private-ip>:2358`

### Code Execution Fails

1. **Check Judge0 Logs**:
   ```powershell
   sudo docker logs judge0 --tail 100
   ```

2. **Verify Isolate is Working**:
   ```powershell
   # Test direct API call
   curl -X POST http://localhost:2358/submissions?base64_encoded=false&wait=true \
     -H "Content-Type: application/json" \
     -d '{"source_code":"print(\"test\");","language_id":71}'
   ```

3. **Check Resource Limits**:
   - Ensure instance has enough CPU/memory
   - Consider upgrading to t3.large for production

## Scaling

### Horizontal Scaling

For high load, you can:
1. Deploy multiple Judge0 instances
2. Use an Application Load Balancer in front
3. Update backend to use ALB endpoint

### Vertical Scaling

For better performance:
1. Upgrade instance type (t3.large, t3.xlarge)
2. Increase EBS volume size if needed
3. Enable CloudWatch monitoring

## Maintenance

### Updating Judge0

1. SSH to instance
2. Pull latest image:
   ```bash
   cd /opt/judge0
   sudo docker compose pull
   sudo docker compose up -d
   ```

### Backup

Judge0 database is stored in Docker volume. To backup:
```bash
# On Judge0 instance
sudo docker exec judge0-db pg_dump -U judge0 judge0 > backup.sql
```

## Alternative: ECS EC2 Launch Type

If you prefer ECS management, you can:
1. Create ECS cluster with EC2 capacity
2. Deploy Judge0 as ECS task with EC2 launch type
3. Requires EC2 instances in the cluster (more complex)

The EC2 instance approach is simpler and recommended for Judge0.

## Next Steps

1. Deploy infrastructure with Terraform
2. Verify Judge0 is running
3. Test code execution from backend
4. Monitor CloudWatch logs
5. Set up alerts for Judge0 health

## References

- [Judge0 Documentation](https://judge0.com/docs)
- [Judge0 GitHub](https://github.com/judge0/judge0)
- [AWS EC2 Documentation](https://docs.aws.amazon.com/ec2/)

