# Quick SSH Fix - No Additional Permissions Needed

## Your Working Command (From Before)

```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180
```

## What Was Wrong

1. **Your IP changed** - Security group had old IP (`72.64.10.109/32`), your current IP is `45.137.78.28`
   - ✅ **FIXED:** Your IP has been added to the security group

2. **Network/Firewall blocking** - Port 22 is not reachable from your network
   - This is a local network/firewall issue, not AWS

## Quick Fix Steps

### Step 1: Try SSH Anyway

Sometimes the connectivity test fails but SSH still works. Try your command:

```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180
```

**If it works:** You're done! Keep the terminal open and connect pgAdmin to `localhost:5433`

**If it fails with "Connection timed out":** Continue to Step 2

### Step 2: Check Windows Firewall

1. **Open Windows Defender Firewall:**
   - Press `Win + R`
   - Type: `wf.msc`
   - Press Enter

2. **Check Outbound Rules:**
   - Click "Outbound Rules" in left panel
   - Look for rules blocking port 22 or SSH
   - Temporarily disable any blocking rules

3. **Or create an exception:**
   - Click "New Rule" → "Port" → "TCP" → "22"
   - Allow the connection
   - Apply to all profiles

### Step 3: Try Different Network

If you're on a corporate network or VPN, it might be blocking SSH:

1. **Try mobile hotspot:**
   - Connect your computer to mobile hotspot
   - Get your new IP: `(Invoke-RestMethod -Uri "https://api.ipify.org?format=json").ip`
   - Add new IP to security group (if different)
   - Try SSH again

2. **Try different WiFi:**
   - Connect to a different network
   - Update security group with new IP if needed

### Step 4: Verify Security Group Has Your IP

```powershell
cd infrastructure/terraform

# Get your current IP
$MY_IP = (Invoke-RestMethod -Uri "https://api.ipify.org?format=json").ip
Write-Host "Your IP: $MY_IP"

# Get security group ID
$INSTANCE_ID = terraform output -raw bastion_instance_id
$SG_ID = aws ec2 describe-instances --instance-ids $INSTANCE_ID --query 'Reservations[0].Instances[0].SecurityGroups[0].GroupId' --output text

# Check if your IP is allowed
$RULES = aws ec2 describe-security-groups --group-ids $SG_ID --query 'SecurityGroups[0].IpPermissions[?FromPort==`22`].IpRanges[*].CidrIp' --output text
Write-Host "Allowed IPs: $RULES"

# If your IP is not in the list, add it:
if (-not ($RULES -match $MY_IP)) {
    Write-Host "Adding your IP to security group..." -ForegroundColor Yellow
    aws ec2 authorize-security-group-ingress --group-id $SG_ID --protocol tcp --port 22 --cidr "$MY_IP/32" --description "SSH from my current IP"
    Write-Host "✓ IP added. Wait 10 seconds..." -ForegroundColor Green
    Start-Sleep -Seconds 10
}
```

### Step 5: Fix SSH Key Permissions (If Needed)

```powershell
# Remove inheritance
icacls "$env:USERPROFILE\.ssh\dev-bastion-key" /inheritance:r

# Grant read-only to current user
icacls "$env:USERPROFILE\.ssh\dev-bastion-key" /grant:r "$env:USERNAME:(R)"
```

## Once Connected

1. **Keep the PowerShell window open** (don't close it - it's the tunnel)
2. **Open pgAdmin 4**
3. **Add new server:**
   - Host: `localhost`
   - Port: `5433`
   - Database: `vector_db`
   - Username: `postgres`
   - Password: (your RDS password)

## Troubleshooting

### "Connection timed out"
- **Cause:** Network/firewall blocking outbound SSH
- **Fix:** Try different network, check Windows Firewall, or use mobile hotspot

### "Permission denied (publickey)"
- **Cause:** SSH key permissions or wrong key
- **Fix:** Run Step 5 above

### "Connection refused"
- **Cause:** Security group doesn't have your IP
- **Fix:** Run Step 4 above

### "Host key verification failed"
- **Cause:** SSH key changed or wrong host
- **Fix:**
  ```powershell
  ssh-keygen -R 13.216.193.180
  ```

## Why This Happened

Your IP address changed since you last connected. The security group was configured with your old IP (`72.64.10.109/32`), but your current IP is `45.137.78.28`. 

**This is normal** - IP addresses can change when:
- You connect to a different network
- Your ISP assigns a new IP
- You restart your router
- You use a VPN

**Solution:** Always check your IP and update the security group when it changes.

---

**Last Updated:** December 7, 2025

