# Database Connection Guide - AWS RDS PostgreSQL

This guide explains how to connect to the PostgreSQL database on AWS RDS from your local machine using pgAdmin or other PostgreSQL clients.

## Prerequisites

- pgAdmin 4 installed (or another PostgreSQL client)
- AWS CLI configured with appropriate credentials
- Access to the AWS account where RDS is deployed

## Important: Database Security

The RDS database is in a **private subnet** for security. This means:
- ❌ You **cannot** connect directly from your local machine
- ✅ You need to use one of the methods below to access it

## Method 1: Get Database Connection Information

First, get the database endpoint and credentials:

```powershell
# Navigate to terraform directory
cd infrastructure/terraform

# Get database endpoint (sensitive output)
terraform output database_endpoint

# Get database port
terraform output database_port

# Database name: vector_db (from variables)
# Username: postgres (from variables)
# Password: The password you set when running terraform apply
```

## Method 2: Connect via AWS Systems Manager Session Manager (Recommended)

This is the most secure method. You'll need an EC2 instance in the same VPC.

### Option A: Use AWS RDS Query Editor (If Available)

1. Go to AWS Console → RDS → Databases
2. Select your database: `dev-postgres`
3. Click "Query Editor" (if available in your region)
4. Enter credentials and connect

### Option B: Create a Bastion Host (EC2 Instance)

1. **Create an EC2 instance in a public subnet:**
   ```powershell
   # This would be done via Terraform or AWS Console
   # Instance should be in the same VPC as RDS
   ```

2. **Update RDS Security Group to allow access from bastion:**
   ```powershell
   # Get your bastion's security group ID
   # Add ingress rule to RDS security group allowing port 5432 from bastion SG
   ```

3. **SSH into bastion and connect:**
   ```bash
   # SSH into bastion
   ssh -i your-key.pem ec2-user@bastion-ip
   
   # Install PostgreSQL client
   sudo yum install postgresql15 -y
   
   # Connect to RDS
   psql -h <rds-endpoint> -U postgres -d vector_db
   ```

## Method 3: Port Forwarding via AWS Systems Manager (SSM)

If you have an EC2 instance with SSM agent:

```powershell
# Install AWS Session Manager Plugin
# Then use port forwarding:
aws ssm start-session --target <instance-id> \
  --document-name AWS-StartPortForwardingSession \
  --parameters '{"portNumber":["5432"],"localPortNumber":["5433"]}'

# In another terminal, connect via localhost:5433
psql -h localhost -p 5433 -U postgres -d vector_db
```

## Method 4: Temporarily Allow Your IP (For Development Only)

⚠️ **Warning:** This is less secure. Only use for development/testing.

1. **Get your public IP:**
   ```powershell
   # Windows PowerShell
   (Invoke-WebRequest -Uri "https://api.ipify.org").Content
   ```

2. **Add your IP to RDS Security Group:**
   ```powershell
   # Get RDS security group ID
   aws ec2 describe-security-groups --filters "Name=group-name,Values=dev-rds-sg" --query "SecurityGroups[0].GroupId" --output text
   
   # Add ingress rule (replace YOUR_IP with your public IP)
   aws ec2 authorize-security-group-ingress \
     --group-id <sg-id> \
     --protocol tcp \
     --port 5432 \
     --cidr YOUR_IP/32
   ```

3. **Connect using pgAdmin:**
   - Host: `<database-endpoint>` (from terraform output)
   - Port: `5432`
   - Database: `vector_db`
   - Username: `postgres`
   - Password: `<your-db-password>`
   - SSL Mode: **Require** (RDS requires SSL)

4. **Remove your IP when done:**
   ```powershell
   aws ec2 revoke-security-group-ingress \
     --group-id <sg-id> \
     --protocol tcp \
     --port 5432 \
     --cidr YOUR_IP/32
   ```

## pgAdmin Connection Steps

1. **Open pgAdmin 4**

2. **Right-click "Servers" → "Create" → "Server"**

3. **General Tab:**
   - Name: `Vector Dev Database`

4. **Connection Tab:**
   - Host name/address: `<database-endpoint>` (without :5432)
   - Port: `5432`
   - Maintenance database: `vector_db`
   - Username: `postgres`
   - Password: `<your-db-password>`
   - Save password: ✅ (optional)

5. **SSL Tab:**
   - SSL mode: **Require**
   - Client certificate: (leave empty)
   - Client certificate key: (leave empty)
   - Root certificate: (leave empty)

6. **Click "Save"**

## Connection String Format

For reference, the connection string format is:
```
Host=<endpoint>;Port=5432;Database=vector_db;Username=postgres;Password=<password>;SSL Mode=Require;
```

## Troubleshooting

### "Connection Timeout"
- Database is in a private subnet - you need to use one of the methods above
- Check security group rules allow your connection method

### "SSL Required"
- Make sure SSL Mode is set to "Require" in pgAdmin
- RDS requires SSL connections

### "Authentication Failed"
- Verify username is `postgres`
- Verify password is correct (the one you set in terraform)
- Check if password has special characters that need escaping

### "Database Does Not Exist"
- Database name should be `vector_db`
- Verify database was created successfully

## Security Best Practices

1. ✅ Use Method 1 (SSM/Bastion) for production
2. ✅ Remove temporary IP access when done
3. ✅ Use strong passwords
4. ✅ Enable SSL (required by RDS)
5. ✅ Don't commit passwords to git
6. ✅ Use AWS Secrets Manager for production credentials

## Quick Reference

```powershell
# Get database endpoint
cd infrastructure/terraform
terraform output database_endpoint

# Get database port (should be 5432)
terraform output database_port

# Connection details:
# Host: <endpoint-from-output>
# Port: 5432
# Database: vector_db
# Username: postgres
# Password: <your-terraform-password>
# SSL: Required
```

