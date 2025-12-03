# Bastion Host Setup Guide

## Overview

A bastion host (jump box) provides secure access to private resources (RDS PostgreSQL, Redis) in your AWS VPC. This guide will help you set up and use the bastion host.

## Prerequisites

1. SSH key pair generated on your local machine
2. AWS CLI configured
3. Terraform installed
4. Access to AWS Console

## Step 1: Generate SSH Key Pair (if you don't have one)

### On Windows (PowerShell)
```powershell
# Generate SSH key
ssh-keygen -t rsa -b 4096 -f $env:USERPROFILE\.ssh\dev-bastion-key -C "bastion-host-key"

# View public key (you'll need this for Terraform)
Get-Content $env:USERPROFILE\.ssh\dev-bastion-key.pub
```

### On macOS/Linux
```bash
# Generate SSH key
ssh-keygen -t rsa -b 4096 -f ~/.ssh/dev-bastion-key -C "bastion-host-key"

# View public key
cat ~/.ssh/dev-bastion-key.pub
```

**Important:** Keep your private key secure! Never share or commit it.

## Step 2: Get Your Public IP Address

```powershell
# Windows PowerShell
(Invoke-WebRequest -Uri "https://api.ipify.org").Content

# Or visit: https://whatismyipaddress.com
```

## Step 3: Update terraform.tfvars

Add the following to your `infrastructure/terraform/terraform.tfvars`:

```hcl
# Bastion Host Configuration
bastion_ssh_public_key = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAACAQD... your-public-key-here"
bastion_allowed_ssh_cidr_blocks = ["YOUR_IP_ADDRESS/32"]  # Replace with your IP
bastion_instance_type = "t3.micro"
```

**Security Note:** Replace `YOUR_IP_ADDRESS/32` with your actual public IP address. Using `/32` allows only your specific IP to access the bastion.

## Step 4: Apply Terraform Configuration

```powershell
cd infrastructure/terraform
terraform plan
terraform apply
```

This will create:
- EC2 instance (bastion host) in public subnet
- Security group allowing SSH from your IP
- Security group rules allowing bastion → RDS
- Security group rules allowing bastion → Redis
- Elastic IP (static public IP)
- IAM role for SSM Session Manager

## Step 5: Get Bastion Connection Info

After `terraform apply` completes:

```powershell
terraform output bastion_public_ip
terraform output bastion_ssh_command
terraform output db_connection_via_bastion
```

## Connecting to PostgreSQL via Bastion

### Method 1: SSH Tunnel + pgAdmin (Recommended)

#### Step 1: Create SSH Tunnel
```powershell
# Windows PowerShell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:dev-postgres.xxxxx.us-east-1.rds.amazonaws.com:5432 ec2-user@BASTION_PUBLIC_IP
```

```bash
# macOS/Linux
ssh -i ~/.ssh/dev-bastion-key -L 5433:dev-postgres.xxxxx.us-east-1.rds.amazonaws.com:5432 ec2-user@BASTION_PUBLIC_IP
```

**Leave this terminal open** - it keeps the tunnel running.

#### Step 2: Configure pgAdmin

1. Open pgAdmin 4
2. Right-click "Servers" → Create → Server
3. **General Tab:**
   - Name: `Vector Dev (via Bastion)`
4. **Connection Tab:**
   - Host: `localhost`
   - Port: `5433` (local tunnel port)
   - Maintenance database: `vector_db`
   - Username: `postgres` (or your RDS username)
   - Password: Your RDS password
5. Click "Save"

Now you can access the database through pgAdmin!

### Method 2: Direct SSH + psql

#### Step 1: SSH into Bastion
```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key ec2-user@BASTION_PUBLIC_IP
```

#### Step 2: Connect to PostgreSQL from Bastion
```bash
# Inside the bastion host
psql -h dev-postgres.xxxxx.us-east-1.rds.amazonaws.com -U postgres -d vector_db
```

Enter your RDS password when prompted.

### Method 3: AWS Systems Manager Session Manager (No SSH Key Required)

#### Prerequisites
Install Session Manager plugin:
```powershell
# Windows (using Chocolatey)
choco install session-manager-plugin

# Or download from: https://docs.aws.amazon.com/systems-manager/latest/userguide/session-manager-working-with-install-plugin.html
```

#### Connect
```powershell
# Get instance ID
$INSTANCE_ID = terraform output -raw bastion_instance_id

# Start session
aws ssm start-session --target $INSTANCE_ID --region us-east-1
```

Then connect to PostgreSQL:
```bash
psql -h dev-postgres.xxxxx.us-east-1.rds.amazonaws.com -U postgres -d vector_db
```

## Security Best Practices

### 1. Restrict SSH Access
Always use your specific IP address, not `0.0.0.0/0`:
```hcl
bastion_allowed_ssh_cidr_blocks = ["203.0.113.45/32"]  # Your IP only
```

### 2. Use Session Manager Instead of SSH
- No need to manage SSH keys
- Audit trail in CloudTrail
- Easier to manage access

### 3. Stop Bastion When Not Needed
```powershell
# Stop bastion instance to save costs
aws ec2 stop-instances --instance-ids <bastion-instance-id> --region us-east-1

# Start when needed
aws ec2 start-instances --instance-ids <bastion-instance-id> --region us-east-1
```

### 4. Rotate SSH Keys Regularly
Generate new keys every 90 days and update Terraform.

## Troubleshooting

### SSH Connection Refused
**Cause:** Security group not allowing your IP

**Solution:**
1. Check your current public IP: `curl ifconfig.me`
2. Update `terraform.tfvars` with your new IP
3. Run `terraform apply`

### SSH Connection Timeout
**Cause:** Bastion instance not running or network issues

**Solution:**
```powershell
# Check instance status
aws ec2 describe-instances --instance-ids <bastion-id> --region us-east-1 --query 'Reservations[0].Instances[0].State.Name'

# Start if stopped
aws ec2 start-instances --instance-ids <bastion-id> --region us-east-1
```

### Database Connection Failed from Bastion
**Cause:** Security group rules not allowing bastion → RDS

**Solution:**
- Verify Terraform created the security group rule
- Check: `terraform state list | grep bastion_to_rds`

### SSH Key Permission Denied
**Cause:** SSH key file permissions too open

**Solution:**
```powershell
# Windows PowerShell
icacls $env:USERPROFILE\.ssh\dev-bastion-key /inheritance:r /grant:r "$env:USERNAME:R"
```

```bash
# macOS/Linux
chmod 400 ~/.ssh/dev-bastion-key
```

## Cost Information

### Bastion Host Costs
- **t3.micro instance:** ~$7.50/month (if running 24/7)
- **Elastic IP:** Free when attached to running instance
- **Data transfer:** Minimal (most data stays within VPC)

### Cost Optimization
- Stop bastion when not in use: `aws ec2 stop-instances`
- Use t3.nano for even lower cost: ~$3.80/month
- Use Session Manager instead of Elastic IP

## pgAdmin Configuration Example

### Connection Settings
```
Host: localhost
Port: 5433
Database: vector_db
Username: postgres
Password: [Your RDS Password]
```

### SSH Tunnel (Advanced Tab)
If pgAdmin supports SSH tunneling directly:
```
SSH Tunnel:
  Host: [Bastion Public IP]
  Port: 22
  Username: ec2-user
  Authentication: Identity file
  Identity file: C:\Users\[YourUser]\.ssh\dev-bastion-key
```

## Quick Reference

### Get Bastion Info
```powershell
cd infrastructure/terraform
terraform output bastion_public_ip
terraform output bastion_ssh_command
```

### Create SSH Tunnel
```powershell
ssh -i ~/.ssh/dev-bastion-key -L 5433:RDS_ENDPOINT:5432 ec2-user@BASTION_IP
```

### Connect pgAdmin
- Host: `localhost`
- Port: `5433`
- Database: `vector_db`

### Stop/Start Bastion
```powershell
# Stop
aws ec2 stop-instances --instance-ids i-xxxxx --region us-east-1

# Start
aws ec2 start-instances --instance-ids i-xxxxx --region us-east-1

# Check status
aws ec2 describe-instances --instance-ids i-xxxxx --region us-east-1 --query 'Reservations[0].Instances[0].State.Name'
```

