# AWS Connection Troubleshooting Guide

## Issue: SSH Connection Timeout

If you're getting `Connection timed out` when trying to SSH to the bastion host, follow these steps:

---

## Step 1: Verify Bastion Instance is Running

### Check Instance Status

```powershell
# Get bastion instance ID from Terraform
cd infrastructure/terraform
$BASTION_INSTANCE_ID = terraform output -raw bastion_instance_id

# Check instance status
aws ec2 describe-instances --instance-ids $BASTION_INSTANCE_ID --query 'Reservations[0].Instances[0].[State.Name,PublicIpAddress]' --output table
```

**Expected Output:**
```
-------------------
| DescribeInstances|
+------------------+
|  running         |
|  13.216.193.180  |
+------------------+
```

**If status is `stopped`:**
```powershell
# Start the instance
aws ec2 start-instances --instance-ids $BASTION_INSTANCE_ID

# Wait for it to start (takes 1-2 minutes)
aws ec2 wait instance-running --instance-ids $BASTION_INSTANCE_ID
```

**If status is `stopping` or `pending`:**
- Wait a few minutes and check again

---

## Step 2: Verify Your Public IP Address

### Get Your Current Public IP

```powershell
# Method 1: Using PowerShell
Invoke-RestMethod -Uri "https://api.ipify.org?format=json" | Select-Object -ExpandProperty ip

# Method 2: Using AWS CLI
aws ec2 describe-instances --query 'Reservations[0].Instances[0].PublicIpAddress' --output text

# Method 3: Check online
# Visit: https://whatismyipaddress.com/
```

**Save your IP address** - you'll need it for the next step.

---

## Step 3: Check Security Group Rules

### Get Bastion Security Group ID

```powershell
cd infrastructure/terraform
$BASTION_SG_ID = terraform output -raw bastion_security_group_id

# Or get it from instance
$BASTION_INSTANCE_ID = terraform output -raw bastion_instance_id
$BASTION_SG_ID = aws ec2 describe-instances --instance-ids $BASTION_INSTANCE_ID --query 'Reservations[0].Instances[0].SecurityGroups[0].GroupId' --output text
```

### Check Current Security Group Rules

```powershell
# View SSH rules
aws ec2 describe-security-groups --group-ids $BASTION_SG_ID --query 'SecurityGroups[0].IpPermissions[?FromPort==`22`]' --output json
```

### Add Your IP to Security Group (If Missing)

```powershell
# Replace YOUR_IP with your actual public IP (e.g., 203.0.113.0/32)
$YOUR_IP = "YOUR_IP/32"  # Example: "203.0.113.0/32"

# Add SSH rule for your IP
aws ec2 authorize-security-group-ingress `
    --group-id $BASTION_SG_ID `
    --protocol tcp `
    --port 22 `
    --cidr $YOUR_IP `
    --description "SSH access from my IP"
```

**Note:** The `/32` means only that specific IP address.

---

## Step 4: Verify Network Connectivity

### Test Basic Connectivity

```powershell
# Test if port 22 is reachable
Test-NetConnection -ComputerName 13.216.193.180 -Port 22

# Or using telnet (if available)
telnet 13.216.193.180 22
```

**If this fails:**
- Your network/firewall might be blocking outbound SSH
- Try from a different network (mobile hotspot, VPN, etc.)

### Check if Instance Has Public IP

```powershell
$BASTION_INSTANCE_ID = terraform output -raw bastion_instance_id
aws ec2 describe-instances --instance-ids $BASTION_INSTANCE_ID --query 'Reservations[0].Instances[0].[PublicIpAddress,PublicDnsName]' --output table
```

**If PublicIpAddress is empty:**
- Instance might be in a private subnet
- Check Terraform configuration

---

## Step 5: Verify SSH Key Permissions (Windows)

### Check Key File Exists

```powershell
Test-Path "$env:USERPROFILE\.ssh\dev-bastion-key"
```

### Fix Key Permissions (If Needed)

Windows SSH might require specific permissions. Try:

```powershell
# Remove inheritance and set permissions
icacls "$env:USERPROFILE\.ssh\dev-bastion-key" /inheritance:r
icacls "$env:USERPROFILE\.ssh\dev-bastion-key" /grant:r "$env:USERNAME:(R)"
```

---

## Step 6: Try Alternative Connection Methods

### Method 1: Use SSM Session Manager (No SSH Required)

```powershell
# Get instance ID
$BASTION_INSTANCE_ID = terraform output -raw bastion_instance_id

# Connect via SSM (requires AWS CLI and proper IAM permissions)
aws ssm start-session --target $BASTION_INSTANCE_ID --region us-east-1
```

**Advantages:**
- No SSH key needed
- No security group rules for SSH
- Works from anywhere
- Full audit trail

### Method 2: Use AWS Systems Manager Port Forwarding

```powershell
# Forward PostgreSQL port via SSM
aws ssm start-session `
    --target $BASTION_INSTANCE_ID `
    --document-name AWS-StartPortForwardingSession `
    --parameters '{"portNumber":["5432"],"localPortNumber":["5433"]}'
```

Then connect to `localhost:5433` from another terminal.

---

## Step 7: Complete Diagnostic Script

Run this PowerShell script to diagnose all issues at once:

```powershell
# Save as: diagnose-bastion.ps1

Write-Host "=== Bastion Connection Diagnostics ===" -ForegroundColor Cyan

# 1. Check Terraform outputs
Write-Host "`n1. Checking Terraform outputs..." -ForegroundColor Yellow
cd infrastructure/terraform
$BASTION_INSTANCE_ID = terraform output -raw bastion_instance_id
$BASTION_IP = terraform output -raw bastion_public_ip
$BASTION_SG_ID = terraform output -raw bastion_security_group_id

Write-Host "   Instance ID: $BASTION_INSTANCE_ID"
Write-Host "   Public IP: $BASTION_IP"
Write-Host "   Security Group: $BASTION_SG_ID"

# 2. Check instance status
Write-Host "`n2. Checking instance status..." -ForegroundColor Yellow
$INSTANCE_STATE = aws ec2 describe-instances --instance-ids $BASTION_INSTANCE_ID --query 'Reservations[0].Instances[0].State.Name' --output text
Write-Host "   State: $INSTANCE_STATE"

if ($INSTANCE_STATE -ne "running") {
    Write-Host "   ⚠️  Instance is not running! Start it with:" -ForegroundColor Red
    Write-Host "   aws ec2 start-instances --instance-ids $BASTION_INSTANCE_ID" -ForegroundColor Yellow
    exit
}

# 3. Get your public IP
Write-Host "`n3. Getting your public IP..." -ForegroundColor Yellow
$MY_IP = (Invoke-RestMethod -Uri "https://api.ipify.org?format=json").ip
Write-Host "   Your IP: $MY_IP"

# 4. Check security group rules
Write-Host "`n4. Checking security group rules..." -ForegroundColor Yellow
$SG_RULES = aws ec2 describe-security-groups --group-ids $BASTION_SG_ID --query 'SecurityGroups[0].IpPermissions[?FromPort==`22`].IpRanges[*].CidrIp' --output text
Write-Host "   Allowed IPs: $SG_RULES"

$IP_ALLOWED = $SG_RULES -match $MY_IP
if (-not $IP_ALLOWED) {
    Write-Host "   ⚠️  Your IP ($MY_IP) is not in the security group!" -ForegroundColor Red
    Write-Host "   Add it with:" -ForegroundColor Yellow
    Write-Host "   aws ec2 authorize-security-group-ingress --group-id $BASTION_SG_ID --protocol tcp --port 22 --cidr $MY_IP/32" -ForegroundColor Yellow
} else {
    Write-Host "   ✓ Your IP is allowed" -ForegroundColor Green
}

# 5. Test connectivity
Write-Host "`n5. Testing connectivity..." -ForegroundColor Yellow
$CONNECTION = Test-NetConnection -ComputerName $BASTION_IP -Port 22 -WarningAction SilentlyContinue
if ($CONNECTION.TcpTestSucceeded) {
    Write-Host "   ✓ Port 22 is reachable" -ForegroundColor Green
} else {
    Write-Host "   ✗ Port 22 is not reachable" -ForegroundColor Red
    Write-Host "   This could be due to:" -ForegroundColor Yellow
    Write-Host "   - Firewall blocking outbound SSH" -ForegroundColor Yellow
    Write-Host "   - Network restrictions" -ForegroundColor Yellow
    Write-Host "   - Security group not allowing your IP" -ForegroundColor Yellow
}

# 6. Check SSH key
Write-Host "`n6. Checking SSH key..." -ForegroundColor Yellow
$KEY_PATH = "$env:USERPROFILE\.ssh\dev-bastion-key"
if (Test-Path $KEY_PATH) {
    Write-Host "   ✓ SSH key exists: $KEY_PATH" -ForegroundColor Green
} else {
    Write-Host "   ✗ SSH key not found: $KEY_PATH" -ForegroundColor Red
}

Write-Host "`n=== Diagnostics Complete ===" -ForegroundColor Cyan
```

**Run it:**
```powershell
.\diagnose-bastion.ps1
```

---

## Quick Fixes

### Fix 1: Add Your IP to Security Group

```powershell
# Get your IP
$MY_IP = (Invoke-RestMethod -Uri "https://api.ipify.org?format=json").ip

# Get security group ID
cd infrastructure/terraform
$SG_ID = terraform output -raw bastion_security_group_id

# Add rule
aws ec2 authorize-security-group-ingress `
    --group-id $SG_ID `
    --protocol tcp `
    --port 22 `
    --cidr "$MY_IP/32" `
    --description "SSH from my current IP"
```

### Fix 2: Start Stopped Instance

```powershell
cd infrastructure/terraform
$INSTANCE_ID = terraform output -raw bastion_instance_id
aws ec2 start-instances --instance-ids $INSTANCE_ID
aws ec2 wait instance-running --instance-ids $INSTANCE_ID
```

### Fix 3: Use SSM Instead of SSH

```powershell
cd infrastructure/terraform
$INSTANCE_ID = terraform output -raw bastion_instance_id
aws ssm start-session --target $INSTANCE_ID --region us-east-1
```

---

## Common Error Messages

### "Connection timed out"
- **Cause:** Security group not allowing your IP, or instance stopped
- **Fix:** Add your IP to security group, or start instance

### "Permission denied (publickey)"
- **Cause:** Wrong SSH key or key permissions
- **Fix:** Check key path and permissions

### "Network is unreachable"
- **Cause:** Network/firewall blocking outbound SSH
- **Fix:** Try different network or use SSM

### "Host key verification failed"
- **Cause:** SSH key changed or wrong host
- **Fix:** Remove old key from `~/.ssh/known_hosts` or use `-o StrictHostKeyChecking=no`

---

## Windows-Specific SSH Command

For Windows PowerShell, use this format:

```powershell
# Correct format for Windows
ssh -i "$env:USERPROFILE\.ssh\dev-bastion-key" -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180

# Or with verbose output for debugging
ssh -v -i "$env:USERPROFILE\.ssh\dev-bastion-key" -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180
```

**Note:** The `-v` flag shows detailed connection information.

---

## Next Steps After Fixing

Once you can connect, test the PostgreSQL connection:

```powershell
# In one terminal: Keep SSH tunnel running
ssh -i "$env:USERPROFILE\.ssh\dev-bastion-key" -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 -N ec2-user@13.216.193.180

# In another terminal: Connect to PostgreSQL
psql -h localhost -p 5433 -U postgres -d vector_db
```

---

**Last Updated:** December 7, 2025

