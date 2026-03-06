# AWS Database Connection Information

## Database Type
**AWS RDS PostgreSQL** (NOT Docker container)

## Connection Details

### Basic Information
- **Host/Endpoint**: `dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com`
- **Port**: `5432`
- **Database Name**: `vector_db`
- **Username**: `postgres`
- **Password**: Set when you ran `terraform apply` (check your `terraform.tfvars` file or the command you used)
- **SSL Mode**: **Require** (RDS requires SSL)

### Connection String Format
```
Host=dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com;Port=5432;Database=vector_db;Username=postgres;Password=<your-password>;SSL Mode=Require;
```

## Important: Database Access

⚠️ **The database is in a PRIVATE subnet** - you **cannot** connect directly from your local machine without additional configuration.

### Current Security Configuration
- Database is in private subnet (not publicly accessible)
- Security group allows access from VPC CIDR: `10.0.0.0/16`
- Only resources within the VPC can connect directly

## How to Connect

### Option 1: Temporarily Allow Your IP (Quick Method for Development)

1. **Get your public IP:**
   ```powershell
   (Invoke-WebRequest -Uri "https://api.ipify.org").Content
   ```

2. **Get the RDS security group ID:**
   ```powershell
   aws ec2 describe-security-groups --filters "Name=group-name,Values=dev-rds-sg" --region us-east-1 --query "SecurityGroups[0].GroupId" --output text
   ```

3. **Add your IP to the security group:**
   ```powershell
   $sgId = aws ec2 describe-security-groups --filters "Name=group-name,Values=dev-rds-sg" --region us-east-1 --query "SecurityGroups[0].GroupId" --output text
   $myIP = (Invoke-WebRequest -Uri "https://api.ipify.org").Content
   aws ec2 authorize-security-group-ingress --group-id $sgId --protocol tcp --port 5432 --cidr "$myIP/32" --region us-east-1
   ```

4. **Connect using pgAdmin:**
   - Host: `dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com`
   - Port: `5432`
   - Database: `vector_db`
   - Username: `postgres`
   - Password: `<your-terraform-password>`
   - SSL Mode: **Require**

5. **Remove your IP when done:**
   ```powershell
   aws ec2 revoke-security-group-ingress --group-id $sgId --protocol tcp --port 5432 --cidr "$myIP/32" --region us-east-1
   ```

### Option 2: Use AWS RDS Query Editor (If Available)

1. Go to AWS Console → RDS → Databases
2. Select `dev-postgres`
3. Click "Query Editor" (if available in your region)
4. Enter credentials and connect

### Option 3: Create a Bastion Host

1. Create an EC2 instance in a public subnet of the same VPC
2. Update RDS security group to allow access from bastion's security group
3. SSH into bastion and connect to RDS from there

### Option 4: Port Forwarding via AWS Systems Manager

If you have an EC2 instance with SSM agent in the VPC:

```powershell
aws ssm start-session --target <instance-id> \
  --document-name AWS-StartPortForwardingSession \
  --parameters '{"portNumber":["5432"],"localPortNumber":["5433"]}'
```

Then connect to `localhost:5433` from your local machine.

## Finding Your Password

The database password was set when you ran `terraform apply`. Check:

1. **terraform.tfvars file** (if it exists):
   ```powershell
   cd infrastructure/terraform
   cat terraform.tfvars
   ```

2. **The command you used** when running terraform apply:
   ```powershell
   terraform apply -var="db_password=YOUR_PASSWORD"
   ```

3. **If you forgot the password**, you can reset it:
   ```powershell
   aws rds modify-db-instance --db-instance-identifier dev-postgres --master-user-password NEW_PASSWORD --apply-immediately --region us-east-1
   ```

## pgAdmin Setup

1. Open pgAdmin 4
2. Right-click "Servers" → "Create" → "Server"
3. **General Tab:**
   - Name: `Vector AWS Dev Database`
4. **Connection Tab:**
   - Host: `dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com`
   - Port: `5432`
   - Database: `vector_db`
   - Username: `postgres`
   - Password: `<your-password>`
   - Save password: ✅
5. **SSL Tab:**
   - SSL mode: **Require**
6. Click "Save"

## Quick Reference

```powershell
# Get database endpoint
cd infrastructure/terraform
terraform output database_endpoint

# Get security group ID
aws ec2 describe-security-groups --filters "Name=group-name,Values=dev-rds-sg" --region us-east-1 --query "SecurityGroups[0].GroupId" --output text

# Get your public IP
(Invoke-WebRequest -Uri "https://api.ipify.org").Content

# Test connection (after adding your IP to security group)
psql -h dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com -p 5432 -U postgres -d vector_db
```

## Summary

- **Type**: AWS RDS PostgreSQL (managed service, not Docker)
- **Endpoint**: `dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com`
- **Port**: `5432`
- **Database**: `vector_db`
- **Username**: `postgres`
- **Password**: Set during terraform apply
- **Access**: Private subnet - requires security group configuration to connect from outside VPC

