# pgAdmin Troubleshooting - Common Issues

## Error: "password authentication failed for user postgres" on port 5432

### Problem
You're seeing this error in pgAdmin:
```
connection to server at "127.0.0.1", port 5432 failed: 
FATAL: password authentication failed for user "postgres"
```

### Root Cause
**pgAdmin is configured with the WRONG PORT.**

- Your SSH tunnel is on port **5433** (correct)
- pgAdmin is trying to connect to port **5432** (wrong - this is your local PostgreSQL if installed)

### Solution

#### Fix pgAdmin Connection Settings:

1. **In pgAdmin:**
   - Right-click your server → **Properties**
   - Go to **Connection** tab
   - Change **Port** from `5432` to `5433`
   - Click **Save**

2. **Try connecting again**

#### Correct Configuration:
```
Host: localhost
Port: 5433  ← MUST BE 5433!
Database: vector_db
Username: postgres
Password: VectorDev2024!SecurePassword
```

### Why This Happens

When you create an SSH tunnel:
```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:RDS_ENDPOINT:5432 ec2-user@BASTION_IP
```

The format is: `-L LOCAL_PORT:REMOTE_HOST:REMOTE_PORT`

- **LOCAL_PORT:** `5433` ← This is what pgAdmin should use
- **REMOTE_HOST:** RDS endpoint
- **REMOTE_PORT:** `5432` ← PostgreSQL's default port on RDS

So pgAdmin connects to `localhost:5433`, which tunnels to `RDS:5432`.

## Error: "Connection refused" or "No route to host"

### Cause
SSH tunnel is not running.

### Solution
1. Check if the tunnel terminal is still open and connected
2. Look for the Amazon Linux welcome message
3. If closed, reconnect:
   ```powershell
   ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180
   ```

## Error: "Address already in use" when creating tunnel

### Cause
Port 5433 is already in use (maybe an old tunnel is still running).

### Solution
```powershell
# Find and kill process using port 5433
Get-Process -Id (Get-NetTCPConnection -LocalPort 5433).OwningProcess | Stop-Process -Force

# Or use a different port
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5434:RDS_ENDPOINT:5432 ec2-user@BASTION_IP
# Then use port 5434 in pgAdmin
```

## Error: "Permission denied (publickey)"

### Cause
SSH key file permissions or wrong key path.

### Solution
```powershell
# Check key exists
Test-Path $env:USERPROFILE\.ssh\dev-bastion-key

# Set correct permissions (Windows)
icacls "$env:USERPROFILE\.ssh\dev-bastion-key" /inheritance:r /grant:r "$env:USERNAME`:R"

# Use full path if needed
ssh -i "$env:USERPROFILE\.ssh\dev-bastion-key" -L 5433:RDS_ENDPOINT:5432 ec2-user@BASTION_IP
```

## Error: "Connection timeout"

### Cause
1. Bastion instance not running
2. Your IP address changed
3. Security group blocking your IP

### Solution
```powershell
# 1. Check bastion status
aws ec2 describe-instances --instance-ids i-08c32ba31f518fdac --region us-east-1 --query 'Reservations[0].Instances[0].State.Name'

# 2. Check your current IP
(Invoke-WebRequest -Uri "https://api.ipify.org").Content

# 3. If IP changed, update terraform.tfvars and apply:
cd infrastructure/terraform
# Update bastion_allowed_ssh_cidr_blocks in terraform.tfvars
terraform apply
```

## Checklist: pgAdmin Won't Connect

- [ ] SSH tunnel is running and connected (check terminal)
- [ ] pgAdmin Host is `localhost` (NOT the RDS endpoint!)
- [ ] pgAdmin Port is `5433` (NOT 5432!)
- [ ] Database name is `vector_db`
- [ ] Username is `postgres`
- [ ] Password is correct (from terraform.tfvars)
- [ ] SSH tunnel terminal is still open and showing Amazon Linux prompt

## Quick Test: Verify Tunnel is Working

While the SSH tunnel is connected, test from another PowerShell window:

```powershell
# Install PostgreSQL client (if not installed)
# Then test:
psql -h localhost -p 5433 -U postgres -d vector_db

# If this works, the tunnel is fine and it's a pgAdmin configuration issue
```

Or use a simpler test:

```powershell
# Test if port 5433 is listening
netstat -an | findstr 5433

# Should show:
# TCP    127.0.0.1:5433     ...   LISTENING
# TCP    [::1]:5433         ...   LISTENING
```

If you see these lines, the tunnel is working correctly!

## Still Having Issues?

### Double-check pgAdmin Settings

**When editing the server in pgAdmin:**
1. General → Name: `Vector Dev (via Bastion)`
2. Connection:
   ```
   Host name/address: localhost
   Port: 5433
   Maintenance database: vector_db
   Username: postgres
   Password: VectorDev2024!SecurePassword
   Save password: ✓
   ```
3. SSL → Mode: `Prefer` or `Disable`

### Common Mistakes

❌ **Using RDS endpoint in pgAdmin:**
- Host: `dev-postgres.cahsciiy4v4q...` ← WRONG!
- Should be: `localhost` ✅

❌ **Using port 5432:**
- Port: `5432` ← WRONG!
- Should be: `5433` ✅

❌ **Tunnel not running:**
- Terminal closed ← WRONG!
- Terminal should show Amazon Linux prompt ✅

## Current Connection Details

**SSH Tunnel Command:**
```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180
```

**pgAdmin Configuration:**
- Host: `localhost`
- Port: `5433`
- Database: `vector_db`
- Username: `postgres`
- Password: `VectorDev2024!SecurePassword`

**Bastion IP:** `13.216.193.180`  
**RDS Endpoint:** `dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432`

