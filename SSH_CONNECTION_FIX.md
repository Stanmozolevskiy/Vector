# Fix SSH Connection to Bastion (Without Adding Permissions)

## Your Previous Working Command

You were able to connect before using:

```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180
```

## Why It Might Not Work Now

### 1. Security Group Rules
Your IP address might not be in the security group anymore, or your IP changed.

**Fix:** We already added your IP (`45.137.78.28`) to the security group earlier today.

### 2. Windows Firewall / Network Restrictions
Your network or Windows Firewall might be blocking outbound SSH (port 22).

**Check:**
```powershell
Test-NetConnection -ComputerName 13.216.193.180 -Port 22
```

If this fails, it's a network/firewall issue, not AWS.

### 3. SSH Key Permissions
Windows SSH might require specific file permissions.

**Fix:**
```powershell
# Remove inheritance and set permissions
icacls "$env:USERPROFILE\.ssh\dev-bastion-key" /inheritance:r
icacls "$env:USERPROFILE\.ssh\dev-bastion-key" /grant:r "$env:USERNAME:(R)"
```

### 4. Bastion Instance Status
The instance might be stopped.

**Check:**
```powershell
cd infrastructure/terraform
$INSTANCE_ID = terraform output -raw bastion_instance_id
aws ec2 describe-instances --instance-ids $INSTANCE_ID --query 'Reservations[0].Instances[0].State.Name' --output text
```

Should return: `running`

## Step-by-Step Fix

### Step 1: Verify Security Group Has Your IP

```powershell
cd infrastructure/terraform
$INSTANCE_ID = terraform output -raw bastion_instance_id
$SG_ID = aws ec2 describe-instances --instance-ids $INSTANCE_ID --query 'Reservations[0].Instances[0].SecurityGroups[0].GroupId' --output text

# Get your current IP
$MY_IP = (Invoke-RestMethod -Uri "https://api.ipify.org?format=json").ip
Write-Host "Your IP: $MY_IP"

# Check if your IP is in the security group
aws ec2 describe-security-groups --group-ids $SG_ID --query "SecurityGroups[0].IpPermissions[?FromPort==\`22\`].IpRanges[?CidrIp==\`$MY_IP/32\`]" --output text

# If empty, add it:
aws ec2 authorize-security-group-ingress --group-id $SG_ID --protocol tcp --port 22 --cidr "$MY_IP/32" --description "SSH from my current IP"
```

### Step 2: Fix SSH Key Permissions (Windows)

```powershell
$keyPath = "$env:USERPROFILE\.ssh\dev-bastion-key"

# Remove inheritance
icacls $keyPath /inheritance:r

# Grant read-only to current user
icacls $keyPath /grant:r "$env:USERNAME:(R)"
```

### Step 3: Test Basic Connectivity

```powershell
# Test if port 22 is reachable
Test-NetConnection -ComputerName 13.216.193.180 -Port 22
```

**If this fails:**
- Your network/firewall is blocking SSH
- Try from a different network (mobile hotspot, VPN)
- Check Windows Firewall settings
- Contact your network administrator

### Step 4: Try SSH with Verbose Output

```powershell
ssh -v -i "$env:USERPROFILE\.ssh\dev-bastion-key" -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180
```

The `-v` flag shows detailed connection information. Look for:
- `Connection refused` = Security group issue
- `Connection timed out` = Network/firewall blocking
- `Permission denied` = SSH key issue

### Step 5: Alternative - Use SSH Config File

Create `$env:USERPROFILE\.ssh\config`:

```
Host bastion
    HostName 13.216.193.180
    User ec2-user
    IdentityFile ~/.ssh/dev-bastion-key
    LocalForward 5433 dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432
    ServerAliveInterval 60
    ServerAliveCountMax 3
```

Then connect with:
```powershell
ssh bastion
```

## Complete Diagnostic Script

Run this to check everything:

```powershell
Write-Host "=== SSH Connection Diagnostics ===" -ForegroundColor Cyan

# 1. Check SSH key
Write-Host "`n1. Checking SSH key..." -ForegroundColor Yellow
$keyPath = "$env:USERPROFILE\.ssh\dev-bastion-key"
if (Test-Path $keyPath) {
    Write-Host "   ✓ Key exists: $keyPath" -ForegroundColor Green
    $keyInfo = Get-Item $keyPath
    Write-Host "   Size: $($keyInfo.Length) bytes" -ForegroundColor Gray
    Write-Host "   Modified: $($keyInfo.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "   ✗ Key not found!" -ForegroundColor Red
    exit
}

# 2. Check instance status
Write-Host "`n2. Checking bastion instance..." -ForegroundColor Yellow
cd infrastructure/terraform
$INSTANCE_ID = terraform output -raw bastion_instance_id
$STATE = aws ec2 describe-instances --instance-ids $INSTANCE_ID --query 'Reservations[0].Instances[0].State.Name' --output text
if ($STATE -eq "running") {
    Write-Host "   ✓ Instance is running" -ForegroundColor Green
} else {
    Write-Host "   ✗ Instance is $STATE" -ForegroundColor Red
    Write-Host "   Start it with: aws ec2 start-instances --instance-ids $INSTANCE_ID" -ForegroundColor Yellow
}

# 3. Get your IP
Write-Host "`n3. Getting your public IP..." -ForegroundColor Yellow
$MY_IP = (Invoke-RestMethod -Uri "https://api.ipify.org?format=json").ip
Write-Host "   Your IP: $MY_IP" -ForegroundColor Green

# 4. Check security group
Write-Host "`n4. Checking security group..." -ForegroundColor Yellow
$SG_ID = aws ec2 describe-instances --instance-ids $INSTANCE_ID --query 'Reservations[0].Instances[0].SecurityGroups[0].GroupId' --output text
$SG_RULES = aws ec2 describe-security-groups --group-ids $SG_ID --query 'SecurityGroups[0].IpPermissions[?FromPort==`22`].IpRanges[*].CidrIp' --output text
Write-Host "   Allowed IPs: $SG_RULES" -ForegroundColor Gray

if ($SG_RULES -match $MY_IP) {
    Write-Host "   ✓ Your IP is allowed" -ForegroundColor Green
} else {
    Write-Host "   ✗ Your IP is NOT in security group!" -ForegroundColor Red
    Write-Host "   Add it with:" -ForegroundColor Yellow
    Write-Host "   aws ec2 authorize-security-group-ingress --group-id $SG_ID --protocol tcp --port 22 --cidr `"$MY_IP/32`"" -ForegroundColor White
}

# 5. Test connectivity
Write-Host "`n5. Testing connectivity..." -ForegroundColor Yellow
$BASTION_IP = terraform output -raw bastion_public_ip
$TEST = Test-NetConnection -ComputerName $BASTION_IP -Port 22 -WarningAction SilentlyContinue
if ($TEST.TcpTestSucceeded) {
    Write-Host "   ✓ Port 22 is reachable!" -ForegroundColor Green
    Write-Host "`n=== Ready to Connect ===" -ForegroundColor Green
    Write-Host "Run this command:" -ForegroundColor Cyan
    Write-Host "ssh -i `"$keyPath`" -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@$BASTION_IP" -ForegroundColor White
} else {
    Write-Host "   ✗ Port 22 is not reachable" -ForegroundColor Red
    Write-Host "   This is likely a network/firewall issue" -ForegroundColor Yellow
    Write-Host "   Try:" -ForegroundColor Yellow
    Write-Host "   - Different network (mobile hotspot)" -ForegroundColor Gray
    Write-Host "   - Check Windows Firewall" -ForegroundColor Gray
    Write-Host "   - Check corporate firewall/VPN" -ForegroundColor Gray
}

Write-Host "`n=== Diagnostics Complete ===" -ForegroundColor Cyan
```

## Common Issues and Solutions

### Issue: "Connection timed out"
**Cause:** Network/firewall blocking outbound SSH  
**Solution:** 
- Try different network
- Check Windows Firewall
- Use mobile hotspot to test

### Issue: "Permission denied (publickey)"
**Cause:** SSH key permissions or wrong key  
**Solution:**
```powershell
icacls "$env:USERPROFILE\.ssh\dev-bastion-key" /inheritance:r
icacls "$env:USERPROFILE\.ssh\dev-bastion-key" /grant:r "$env:USERNAME:(R)"
```

### Issue: "Connection refused"
**Cause:** Security group doesn't allow your IP  
**Solution:** Add your IP to security group (see Step 1)

### Issue: "Host key verification failed"
**Cause:** SSH key changed or wrong host  
**Solution:**
```powershell
# Remove old key from known_hosts
ssh-keygen -R 13.216.193.180
```

## Once Connected

After the SSH tunnel is established:

1. **Keep the PowerShell window open** (don't close it)
2. **Open pgAdmin 4**
3. **Add new server:**
   - Host: `localhost`
   - Port: `5433`
   - Database: `vector_db`
   - Username: `postgres`
   - Password: (your database password)

The tunnel forwards `localhost:5433` → `bastion` → `RDS:5432`

---

**Last Updated:** December 7, 2025

