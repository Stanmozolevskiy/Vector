# Bastion Host Module

## Overview

This module creates a bastion host (jump box) for secure access to private resources in the VPC.

## Resources Created

- EC2 instance (Amazon Linux 2023) in public subnet
- Security group for bastion with SSH access
- Security group rules allowing bastion → RDS
- Security group rules allowing bastion → Redis
- IAM role for SSM Session Manager
- Elastic IP (optional)
- SSH key pair

## Usage

```hcl
module "bastion" {
  source = "./modules/bastion"

  environment              = "dev"
  vpc_id                   = module.vpc.vpc_id
  public_subnet_ids        = module.vpc.public_subnet_ids
  rds_security_group_id    = module.database.security_group_id
  redis_security_group_id  = module.redis.security_group_id
  ssh_public_key           = var.bastion_ssh_public_key
  allowed_ssh_cidr_blocks  = ["YOUR_IP/32"]
  instance_type            = "t3.micro"
  use_elastic_ip           = true
}
```

## Variables

| Variable | Type | Description | Default |
|----------|------|-------------|---------|
| `environment` | string | Environment name (dev, staging, prod) | - |
| `vpc_id` | string | VPC ID where bastion will be deployed | - |
| `public_subnet_ids` | list(string) | Public subnet IDs | - |
| `rds_security_group_id` | string | RDS security group ID | - |
| `redis_security_group_id` | string | Redis security group ID | - |
| `ssh_public_key` | string | SSH public key content | - |
| `allowed_ssh_cidr_blocks` | list(string) | CIDRs allowed to SSH | ["0.0.0.0/0"] |
| `instance_type` | string | EC2 instance type | "t3.micro" |
| `use_elastic_ip` | bool | Whether to use Elastic IP | true |

## Outputs

| Output | Description |
|--------|-------------|
| `bastion_public_ip` | Public IP of bastion host |
| `bastion_instance_id` | EC2 instance ID |
| `bastion_security_group_id` | Security group ID |
| `ssh_command` | SSH command to connect |
| `ssm_command` | SSM Session Manager command |

## Security Features

1. **SSH Access Control:**
   - Restricted to specific CIDR blocks
   - Uses SSH key authentication (no passwords)
   - Can use your specific IP address only

2. **SSM Session Manager:**
   - Alternative to SSH (no key required)
   - Full audit trail in CloudTrail
   - IAM-based access control

3. **Network Security:**
   - Deployed in public subnet for SSH access
   - Security group rules allow only necessary outbound traffic
   - No inbound traffic except SSH

4. **Instance Security:**
   - Amazon Linux 2023 (latest security patches)
   - Encrypted EBS volume
   - IAM role with minimal permissions

## Cost

### t3.micro (default)
- **On-Demand:** ~$0.0104/hour (~$7.50/month if running 24/7)
- **Elastic IP:** Free when attached to running instance
- **Data Transfer:** Minimal (most traffic within VPC)

### Cost Optimization
- Stop instance when not needed: `aws ec2 stop-instances`
- Use t3.nano for even lower cost: ~$3.80/month
- Use Session Manager (no Elastic IP needed)

## Access Methods

### 1. SSH with Key
```bash
ssh -i ~/.ssh/dev-bastion-key ec2-user@BASTION_PUBLIC_IP
```

### 2. SSH Tunnel (for pgAdmin)
```bash
ssh -i ~/.ssh/dev-bastion-key -L 5433:RDS_ENDPOINT:5432 ec2-user@BASTION_PUBLIC_IP
```

### 3. SSM Session Manager
```bash
aws ssm start-session --target INSTANCE_ID --region us-east-1
```

## Connecting to Database

### From Bastion (Direct)
```bash
# SSH into bastion first
ssh -i ~/.ssh/dev-bastion-key ec2-user@BASTION_IP

# Then connect to PostgreSQL
psql -h dev-postgres.abc123.us-east-1.rds.amazonaws.com -U postgres -d vector_db
```

### From Local Machine (via Tunnel)
```bash
# 1. Create tunnel (terminal 1)
ssh -i ~/.ssh/dev-bastion-key -L 5433:RDS_ENDPOINT:5432 ec2-user@BASTION_IP

# 2. Connect from local machine (terminal 2)
psql -h localhost -p 5433 -U postgres -d vector_db

# Or use pgAdmin with localhost:5433
```

## Maintenance

### Update SSH Keys
```bash
# 1. Generate new key
ssh-keygen -t rsa -b 4096 -f ~/.ssh/dev-bastion-key-new

# 2. Update terraform.tfvars with new public key
# 3. Apply changes
terraform apply

# 4. Old key is replaced
```

### Update Allowed IPs
```hcl
# terraform.tfvars
bastion_allowed_ssh_cidr_blocks = [
  "NEW_IP_ADDRESS/32"
]
```

Then: `terraform apply`

### Destroy Bastion
```bash
# Update main.tf (comment out bastion module)
# Then:
terraform apply
```

## Installed Software on Bastion

The bastion comes pre-installed with:
- **PostgreSQL 15 client** (`psql`)
- **Redis client** (`redis-cli`)
- **AWS CLI** (pre-installed on Amazon Linux)
- **SSM Agent** (for Session Manager)

### Connect to Redis
```bash
# From bastion
redis-cli -h dev-redis.abc123.0001.use1.cache.amazonaws.com -p 6379
```

