# Deploy Bastion Host - Step-by-Step Instructions

## Quick Setup (5 minutes)

Follow these steps to deploy the bastion host and connect to PostgreSQL with pgAdmin.

### Step 1: Generate SSH Key

**Windows PowerShell:**
```powershell
# Generate key
ssh-keygen -t rsa -b 4096 -f $env:USERPROFILE\.ssh\dev-bastion-key

# Press Enter for all prompts (or set a passphrase for extra security)

# View public key (copy this entire line)
Get-Content $env:USERPROFILE\.ssh\dev-bastion-key.pub
```

**Copy the output** - you'll need it for Step 2.

### Step 2: Get Your Public IP

```powershell
(Invoke-WebRequest -Uri "https://api.ipify.org").Content
```

**Copy the IP address** - you'll need it for Step 3.

### Step 3: Update terraform.tfvars

```powershell
cd C:\Users\stanm\source\repos\Vecotr\infrastructure\terraform

# If terraform.tfvars doesn't exist, create it
# Add these lines (replace with your actual values):
```

Add to `terraform.tfvars`:
```hcl
# Existing values...
aws_region = "us-east-1"
environment = "dev"
db_password = "your_existing_password"
# ... other existing values ...

# NEW: Bastion Host Configuration
bastion_ssh_public_key = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAACAQD... [PASTE YOUR PUBLIC KEY HERE]"
bastion_allowed_ssh_cidr_blocks = ["YOUR.IP.ADDRESS.HERE/32"]
bastion_instance_type = "t3.micro"
```

**Example:**
```hcl
bastion_ssh_public_key = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABgQC7X..."
bastion_allowed_ssh_cidr_blocks = ["203.0.113.45/32"]
bastion_instance_type = "t3.micro"
```

### Step 4: Deploy Bastion Host

```powershell
cd C:\Users\stanm\source\repos\Vecotr\infrastructure\terraform

# Review changes
terraform plan

# Deploy (type 'yes' when prompted)
terraform apply
```

**This will take ~2-3 minutes.**

### Step 5: Get Connection Info

```powershell
# Get bastion public IP
terraform output bastion_public_ip

# Get full connection instructions
terraform output db_connection_via_bastion
```

**Copy the bastion public IP** - you'll need it for Step 6.

### Step 6: Create SSH Tunnel

Open a **new PowerShell window** and run:

```powershell
# Replace BASTION_IP and RDS_ENDPOINT with your actual values
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:dev-postgres.c9gtkzkqzs8w.us-east-1.rds.amazonaws.com:5432 ec2-user@BASTION_IP
```

**Example:**
```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:dev-postgres.c9gtkzkqzs8w.us-east-1.rds.amazonaws.com:5432 ec2-user@54.123.45.67
```

**Leave this window open** - it keeps the tunnel active.

### Step 7: Configure pgAdmin 4

1. Open pgAdmin 4
2. Right-click "Servers" → Create → Server

**General Tab:**
- Name: `Vector Dev (via Bastion)`

**Connection Tab:**
- Host name/address: `localhost`
- Port: `5433`
- Maintenance database: `vector_db`
- Username: `postgres`
- Password: [Your db_password from terraform.tfvars]
- Save password: ✅ (optional)

3. Click **Save**

### Step 8: Verify Connection

You should now see:
- Server: `Vector Dev (via Bastion)`
- Database: `vector_db`
- Tables: `Users`, `EmailVerifications`, `PasswordResets`, etc.

## Troubleshooting

### Error: "Connection refused"

**Solution 1 - Check your IP:**
```powershell
# Your IP may have changed
(Invoke-WebRequest -Uri "https://api.ipify.org").Content

# Update terraform.tfvars with new IP
# Then: terraform apply
```

**Solution 2 - Check bastion is running:**
```powershell
aws ec2 describe-instances --filters "Name=tag:Name,Values=dev-bastion-host" --region us-east-1 --query 'Reservations[0].Instances[0].State.Name'

# If stopped, start it:
aws ec2 start-instances --instance-ids INSTANCE_ID --region us-east-1
```

### Error: "Permission denied (publickey)"

**Solution - Fix key permissions:**
```powershell
icacls "$env:USERPROFILE\.ssh\dev-bastion-key" /inheritance:r /grant:r "$env:USERNAME:R"
```

### Error: pgAdmin can't connect

**Checklist:**
- [ ] SSH tunnel is running (check the terminal)
- [ ] Using `localhost` as host in pgAdmin (not the RDS endpoint!)
- [ ] Using port `5433` in pgAdmin (the tunnel port!)
- [ ] Password is correct
- [ ] Database name is `vector_db`

## After You're Done

### Stop Bastion (Save Money)
```powershell
# Get instance ID
cd infrastructure/terraform
$INSTANCE_ID = terraform output -raw bastion_instance_id

# Stop instance
aws ec2 stop-instances --instance-ids $INSTANCE_ID --region us-east-1
```

**Cost Savings:** Stopping the instance saves ~$7.50/month while keeping configuration.

### Start Bastion (When Needed Again)
```powershell
aws ec2 start-instances --instance-ids $INSTANCE_ID --region us-east-1

# Wait ~30 seconds for it to boot
Start-Sleep -Seconds 30

# Get new public IP (if not using Elastic IP)
aws ec2 describe-instances --instance-ids $INSTANCE_ID --region us-east-1 --query 'Reservations[0].Instances[0].PublicIpAddress'
```

## Quick Reference Card

### Create Tunnel
```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:RDS_ENDPOINT:5432 ec2-user@BASTION_IP
```

### pgAdmin Connection
- Host: `localhost`
- Port: `5433`
- Database: `vector_db`
- Username: `postgres`

### Useful Commands
```powershell
# Get bastion IP
terraform output bastion_public_ip

# Stop bastion
aws ec2 stop-instances --instance-ids INSTANCE_ID

# Start bastion
aws ec2 start-instances --instance-ids INSTANCE_ID

# Check bastion status
aws ec2 describe-instances --instance-ids INSTANCE_ID --query 'Reservations[0].Instances[0].State.Name'
```

## Next Steps

After connecting to the database:
1. View the `Users` table to see registered users
2. Check `EmailVerifications` for verification tokens
3. Check `PasswordResets` for password reset tokens
4. Run queries to verify data
5. Test the application end-to-end

## Need Help?

See detailed guides:
- `infrastructure/terraform/BASTION_SETUP_GUIDE.md` - Full setup guide
- `infrastructure/terraform/SSH_KEY_GENERATION.md` - SSH key details
- `PGADMIN_CONNECTION_GUIDE.md` - pgAdmin configuration

