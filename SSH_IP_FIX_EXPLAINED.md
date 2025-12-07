# SSH Connection Fix - IP and /32 CIDR Explained

## The Issue

Your SSH connection is timing out because:

1. **Your IP changed** - Old IP: `72.64.10.109`, Current IP: `45.137.78.28`
2. **Security group only allows the old IP** - Only `72.64.10.109/32` is allowed
3. **Your new IP needs to be added with `/32`**

## About `/32` CIDR Notation

The `/32` is **required** and means "single IP address only":

- `45.137.78.28/32` = Only IP `45.137.78.28` (secure, recommended)
- `45.137.78.28/24` = All IPs from `45.137.78.0` to `45.137.78.255` (less secure)
- `0.0.0.0/0` = All IPs from anywhere (not secure, don't use!)

**Why `/32`?**
- Security: Only your specific IP can connect
- Best practice: Restrict access to known IPs
- The old IP (`72.64.10.109/32`) also has `/32` - this is correct

## Why It Worked Before

Possible reasons it worked before:

1. **Your IP was `72.64.10.109`** when you last connected
2. **Security group had your IP** at that time
3. **Network/firewall wasn't blocking** outbound SSH
4. **Instance was running** and accessible

## Current Status

- ✅ **Instance is running** - `13.216.193.180`
- ✅ **Old IP in security group** - `72.64.10.109/32`
- ❌ **Your current IP NOT in security group** - `45.137.78.28/32` (needs to be added)
- ❌ **Port 22 not reachable** - Network/firewall issue (after IP is added)

## Fix Steps

### Step 1: Add Your IP with /32

```powershell
cd infrastructure/terraform

# Get your current IP
$MY_IP = (Invoke-RestMethod -Uri "https://api.ipify.org?format=json").ip
Write-Host "Your IP: $MY_IP"

# Get security group ID
$INSTANCE_ID = terraform output -raw bastion_instance_id
$SG_ID = aws ec2 describe-instances --instance-ids $INSTANCE_ID --query 'Reservations[0].Instances[0].SecurityGroups[0].GroupId' --output text

# Add your IP with /32
aws ec2 authorize-security-group-ingress `
    --group-id $SG_ID `
    --protocol tcp `
    --port 22 `
    --cidr "$MY_IP/32"

# Wait for propagation
Start-Sleep -Seconds 15
```

### Step 2: Test SSH Connection

```powershell
ssh -vv -i "$env:USERPROFILE\.ssh\dev-bastion-key" -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180
```

The `-vv` flag shows detailed connection information.

### Step 3: If Still Timing Out

If connection still times out after adding your IP:

1. **Check Windows Firewall:**
   ```powershell
   # Open Windows Firewall
   wf.msc
   ```
   - Check outbound rules for port 22
   - Temporarily disable firewall to test

2. **Try Different Network:**
   - Mobile hotspot
   - Different WiFi
   - Disconnect VPN if connected

3. **Check Corporate Firewall:**
   - If on corporate network, SSH (port 22) might be blocked
   - Try from home network or mobile hotspot

## Why You Didn't Restrict Port 22 Before

You're right - you didn't manually restrict port 22. The security group was created by Terraform with:

```hcl
bastion_allowed_ssh_cidr_blocks = ["YOUR_IP/32"]
```

This was set when the bastion was first deployed. The `/32` is automatically added by Terraform when you specify an IP in the `terraform.tfvars` file.

## What Changed

1. **Your IP changed** - This is normal (ISP assigns new IPs)
2. **Security group wasn't updated** - Still has old IP
3. **Network might be blocking** - Firewall/VPN/corporate network

## Quick Fix Script

Run this complete fix:

```powershell
cd infrastructure/terraform

# Get your IP
$MY_IP = (Invoke-RestMethod -Uri "https://api.ipify.org?format=json").ip
Write-Host "Your IP: $MY_IP/32" -ForegroundColor Green

# Get security group
$INSTANCE_ID = terraform output -raw bastion_instance_id
$SG_ID = aws ec2 describe-instances --instance-ids $INSTANCE_ID --query 'Reservations[0].Instances[0].SecurityGroups[0].GroupId' --output text

# Add IP with /32
Write-Host "Adding your IP to security group..." -ForegroundColor Yellow
aws ec2 authorize-security-group-ingress --group-id $SG_ID --protocol tcp --port 22 --cidr "$MY_IP/32" 2>&1 | Out-Null

# Wait for propagation
Write-Host "Waiting 15 seconds for propagation..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Verify
$RULES = aws ec2 describe-security-groups --group-ids $SG_ID --query 'SecurityGroups[0].IpPermissions[?FromPort==`22`].IpRanges[*].CidrIp' --output text
Write-Host "`nAllowed IPs:" -ForegroundColor Cyan
$RULES -split "`t" | ForEach-Object { if ($_) { Write-Host "  - $_" -ForegroundColor White } }

# Test connection
Write-Host "`n=== Try SSH Now ===" -ForegroundColor Green
Write-Host "ssh -i `"$env:USERPROFILE\.ssh\dev-bastion-key`" -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180" -ForegroundColor White
```

## Summary

- **`/32` is required** - It means "single IP address" (secure)
- **Old IP had `/32`** - This is correct
- **New IP needs `/32`** - Must be added the same way
- **It worked before** - Because your IP matched the security group
- **Network might block** - Even with correct IP, firewall can block SSH

---

**Last Updated:** December 7, 2025

