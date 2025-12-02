# pgAdmin 4 Connection Guide - AWS Dev Database via Bastion

## Overview

This guide explains how to connect to your AWS RDS PostgreSQL database using pgAdmin 4 through the bastion host (jump box).

## Prerequisites

1. ✅ Bastion host deployed to AWS (via Terraform)
2. ✅ SSH key pair generated
3. ✅ pgAdmin 4 installed on your local machine
4. ✅ SSH access to bastion from your IP

## Connection Method: SSH Tunnel

### Step 1: Get Connection Information

```powershell
cd infrastructure/terraform
terraform output bastion_public_ip
terraform output database_endpoint
terraform output db_connection_via_bastion
```

Save these values:
- **Bastion IP:** (e.g., `54.123.45.67`)
- **RDS Endpoint:** (e.g., `dev-postgres.abc123.us-east-1.rds.amazonaws.com`)
- **RDS Port:** `5432`

### Step 2: Create SSH Tunnel

Open a terminal and keep it running:

#### Windows PowerShell
```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:dev-postgres.abc123.us-east-1.rds.amazonaws.com:5432 ec2-user@54.123.45.67
```

#### macOS/Linux
```bash
ssh -i ~/.ssh/dev-bastion-key -L 5433:dev-postgres.abc123.us-east-1.rds.amazonaws.com:5432 ec2-user@54.123.45.67
```

**Important:**
- Replace `dev-postgres.abc123.us-east-1.rds.amazonaws.com` with your actual RDS endpoint
- Replace `54.123.45.67` with your actual bastion public IP
- Keep this terminal window open while using pgAdmin

### Step 3: Configure pgAdmin

1. **Open pgAdmin 4**

2. **Right-click "Servers" in the left panel → Create → Server**

3. **General Tab:**
   - **Name:** `Vector Dev (via Bastion)`

4. **Connection Tab:**
   - **Host name/address:** `localhost`
   - **Port:** `5433` (the local tunnel port, not 5432!)
   - **Maintenance database:** `vector_db`
   - **Username:** `postgres` (or your RDS username)
   - **Password:** Your RDS password (from terraform.tfvars `db_password`)
   - **Save password:** ✅ (optional, for convenience)

5. **SSL Tab:**
   - **SSL mode:** `Prefer` (or `Disable` for dev)

6. **Advanced Tab:**
   - Leave defaults

7. **Click "Save"**

### Step 4: Verify Connection

You should now see:
- `Vector Dev (via Bastion)` server in pgAdmin
- Database: `vector_db`
- Tables:
  - `Users`
  - `EmailVerifications`
  - `PasswordResets`
  - `Subscriptions`
  - `Payments`
  - `__EFMigrationsHistory`

## Viewing Data

### Query Users
```sql
SELECT 
    "Id", 
    "Email", 
    "FirstName", 
    "LastName", 
    "Role", 
    "EmailVerified", 
    "CreatedAt"
FROM "Users"
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

### Check Email Verifications
```sql
SELECT 
    ev."Id",
    ev."Token",
    ev."ExpiresAt",
    ev."IsUsed",
    u."Email",
    ev."CreatedAt"
FROM "EmailVerifications" ev
JOIN "Users" u ON ev."UserId" = u."Id"
ORDER BY ev."CreatedAt" DESC
LIMIT 10;
```

### Check Password Resets
```sql
SELECT 
    pr."Id",
    pr."Token",
    pr."ExpiresAt",
    pr."IsUsed",
    u."Email",
    pr."CreatedAt"
FROM "PasswordResets" pr
JOIN "Users" u ON pr."UserId" = u."Id"
ORDER BY pr."CreatedAt" DESC
LIMIT 10;
```

### View Migration History
```sql
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
```

## Troubleshooting

### Issue: SSH Connection Refused

**Possible Causes:**
1. Bastion instance not running
2. Your IP address changed
3. Security group not allowing your IP

**Solution:**
```powershell
# 1. Check bastion status
aws ec2 describe-instances --filters "Name=tag:Name,Values=dev-bastion-host" --region us-east-1 --query 'Reservations[0].Instances[0].State.Name'

# 2. Get your current IP
(Invoke-WebRequest -Uri "https://api.ipify.org").Content

# 3. Update terraform.tfvars and apply
cd infrastructure/terraform
terraform apply
```

### Issue: Permission Denied (publickey)

**Solution:**
```powershell
# Check key file exists
ls $env:USERPROFILE\.ssh\dev-bastion-key

# Set correct permissions (Windows)
icacls "$env:USERPROFILE\.ssh\dev-bastion-key" /inheritance:r /grant:r "$env:USERNAME:R"

# Use -v flag for verbose output
ssh -v -i $env:USERPROFILE\.ssh\dev-bastion-key ec2-user@BASTION_IP
```

### Issue: pgAdmin Can't Connect to localhost:5433

**Possible Causes:**
1. SSH tunnel not running
2. Wrong RDS endpoint in tunnel command
3. Wrong port in pgAdmin

**Solution:**
1. **Verify tunnel is running:**
   - Check the terminal where you ran the SSH command
   - Should show: `Welcome to Amazon Linux 2023`
   - Keep this terminal open

2. **Verify tunnel port:**
   ```powershell
   # Windows
   netstat -an | findstr 5433
   
   # Should show: TCP  127.0.0.1:5433  ...  LISTENING
   ```

3. **Verify pgAdmin settings:**
   - Host: `localhost` (not the RDS endpoint!)
   - Port: `5433` (the tunnel port, not 5432!)

### Issue: Database Password Incorrect

**Solution:**
```powershell
# Get the password from terraform.tfvars
cd infrastructure/terraform
Get-Content terraform.tfvars | Select-String "db_password"
```

Or check AWS Secrets Manager if you're storing it there.

### Issue: SSH Tunnel Drops Frequently

**Solution:**
Add keep-alive to SSH config:

**Windows:** Create/edit `C:\Users\YourUser\.ssh\config`:
```
Host vector-bastion
  HostName BASTION_PUBLIC_IP
  User ec2-user
  IdentityFile C:\Users\YourUser\.ssh\dev-bastion-key
  ServerAliveInterval 60
  ServerAliveCountMax 3
```

**macOS/Linux:** Create/edit `~/.ssh/config`:
```
Host vector-bastion
  HostName BASTION_PUBLIC_IP
  User ec2-user
  IdentityFile ~/.ssh/dev-bastion-key
  ServerAliveInterval 60
  ServerAliveCountMax 3
```

Then connect with: `ssh -L 5433:RDS_ENDPOINT:5432 vector-bastion`

## Alternative: AWS Systems Manager Session Manager

If you don't want to manage SSH keys:

### Step 1: Install Session Manager Plugin
```powershell
# Windows (Chocolatey)
choco install session-manager-plugin

# Or download from:
# https://docs.aws.amazon.com/systems-manager/latest/userguide/session-manager-working-with-install-plugin.html
```

### Step 2: Start Session
```powershell
# Get instance ID
cd infrastructure/terraform
$INSTANCE_ID = terraform output -raw bastion_instance_id

# Connect
aws ssm start-session --target $INSTANCE_ID --region us-east-1
```

### Step 3: From Session, Connect to PostgreSQL
```bash
psql -h dev-postgres.abc123.us-east-1.rds.amazonaws.com -U postgres -d vector_db
```

## Security Best Practices

1. **Always use SSH tunnel** - Never expose RDS publicly
2. **Restrict bastion access** - Use your specific IP (/32), not 0.0.0.0/0
3. **Stop bastion when not needed** - Saves money and improves security
4. **Use strong passwords** - For both SSH key passphrase and RDS
5. **Rotate credentials** - Change SSH keys and RDS passwords regularly

## Cost Management

### Stop Bastion When Not Needed
```powershell
# Stop instance (saves ~$7.50/month)
aws ec2 stop-instances --instance-ids INSTANCE_ID --region us-east-1

# Start when needed
aws ec2 start-instances --instance-ids INSTANCE_ID --region us-east-1

# Note: Elastic IP remains attached and costs nothing while instance is stopped
```

### Check Bastion Status
```powershell
aws ec2 describe-instances --instance-ids INSTANCE_ID --region us-east-1 --query 'Reservations[0].Instances[0].State.Name'
```

## Quick Reference

### Create Tunnel
```powershell
ssh -i ~/.ssh/dev-bastion-key -L 5433:RDS_ENDPOINT:5432 ec2-user@BASTION_IP
```

### pgAdmin Settings
- **Host:** `localhost`
- **Port:** `5433`
- **Database:** `vector_db`
- **Username:** `postgres`
- **Password:** [Your RDS password]

### Useful Queries
```sql
-- List all tables
SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';

-- Count users
SELECT COUNT(*) FROM "Users";

-- View recent registrations
SELECT "Email", "EmailVerified", "CreatedAt" FROM "Users" ORDER BY "CreatedAt" DESC LIMIT 10;
```

