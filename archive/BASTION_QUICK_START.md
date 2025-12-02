# Bastion Host - Quick Start Guide

## ✅ Bastion Successfully Deployed!

Your bastion host is now running on AWS and ready for use.

## Connection Details

- **Bastion Public IP:** `13.216.193.180`
- **Instance ID:** `i-08c32ba31f518fdac`
- **RDS Endpoint:** `dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432`
- **Database:** `vector_db`
- **Username:** `postgres`
- **Password:** `VectorDev2024!SecurePassword`

## Connect to PostgreSQL with pgAdmin 4

### Step 1: Create SSH Tunnel

Open PowerShell and run:

```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180
```

**IMPORTANT:**
- Replace `dev-postgres.c9gtkzkqzs8w.us-east-1.rds.amazonaws.com` with your actual RDS endpoint
- Get RDS endpoint: `cd infrastructure/terraform; terraform output -raw database_endpoint`
- **Keep this terminal window open** while using pgAdmin

### Step 2: Configure pgAdmin

1. **Open pgAdmin 4**
2. **Right-click "Servers" → Create → Server**

**General Tab:**
- Name: `Vector Dev (via Bastion)`

**Connection Tab:**
- Host: `localhost`
- Port: `5433`
- Maintenance database: `vector_db`
- Username: `postgres`
- Password: `VectorDev2024!SecurePassword`
- Save password: ✅

3. **Click "Save"**

### Step 3: Verify Tables

You should see these tables:
- ✅ `Users`
- ✅ `EmailVerifications`
- ✅ `PasswordResets` ← New table from migration
- ✅ `Subscriptions`
- ✅ `Payments`
- ✅ `__EFMigrationsHistory`

## Alternative: SSM Session Manager (No SSH Key Required)

```powershell
# Connect via AWS Systems Manager
aws ssm start-session --target i-08c32ba31f518fdac --region us-east-1

# Then from the session:
psql -h dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com -U postgres -d vector_db
```

## Useful Queries

### View Recent Users
```sql
SELECT 
    "Id", 
    "Email", 
    "FirstName", 
    "LastName", 
    "EmailVerified", 
    "CreatedAt"
FROM "Users"
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

### Check Password Reset Tokens
```sql
SELECT 
    pr."Token",
    pr."ExpiresAt",
    pr."IsUsed",
    u."Email",
    pr."CreatedAt"
FROM "PasswordResets" pr
JOIN "Users" u ON pr."UserId" = u."Id"
ORDER BY pr."CreatedAt" DESC;
```

### Verify Migration Applied
```sql
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
```

You should see:
- `20251129193049_InitialCreate`
- `20251202143821_AddPasswordResetTable` ← New migration

## Managing the Bastion

### Stop Bastion (Save ~$7.50/month)
```powershell
aws ec2 stop-instances --instance-ids i-08c32ba31f518fdac --region us-east-1
```

### Start Bastion (When Needed)
```powershell
aws ec2 start-instances --instance-ids i-08c32ba31f518fdac --region us-east-1
```

### Check Status
```powershell
aws ec2 describe-instances --instance-ids i-08c32ba31f518fdac --region us-east-1 --query 'Reservations[0].Instances[0].State.Name'
```

## Security Notes

- ✅ SSH access restricted to your IP: `72.64.10.109/32`
- ✅ SSH key authentication enabled
- ✅ IAM role configured for SSM Session Manager
- ✅ Encrypted EBS volume
- ✅ Deployed in public subnet (for SSH access)
- ✅ Security groups allow bastion → RDS and bastion → Redis

## Troubleshooting

### Connection Refused
Your IP may have changed. Check current IP:
```powershell
(Invoke-WebRequest -Uri "https://api.ipify.org").Content
```

If different, update `terraform.tfvars` and run `terraform apply`.

### Permission Denied
Fix SSH key permissions:
```powershell
icacls "$env:USERPROFILE\.ssh\dev-bastion-key" /inheritance:r /grant:r "$env:USERNAME:R"
```

### Can't Find RDS Endpoint
```powershell
cd infrastructure/terraform
terraform output -raw database_endpoint
```

## Full Documentation

- **Quick Setup:** `infrastructure/terraform/DEPLOY_BASTION_INSTRUCTIONS.md`
- **Complete Guide:** `infrastructure/terraform/BASTION_SETUP_GUIDE.md`
- **pgAdmin Setup:** `PGADMIN_CONNECTION_GUIDE.md`
- **SSH Keys:** `infrastructure/terraform/SSH_KEY_GENERATION.md`

