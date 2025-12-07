# Connect to AWS Redis via Bastion Host

## Prerequisites

✅ SSH connection to bastion is working  
✅ PostgreSQL tunnel is already set up (port 5433)

## Step 1: Get Redis Connection Information

```powershell
cd infrastructure/terraform

# Get Redis endpoint and port
terraform output redis_endpoint
terraform output redis_port

# Get bastion IP (if needed)
terraform output bastion_public_ip
```

**Example Output:**
```
redis_endpoint = "dev-redis.xxxxx.0001.use1.cache.amazonaws.com"
redis_port = 6379
bastion_public_ip = "13.216.193.180"
```

## Step 2: Create SSH Tunnel for Redis

**Open a NEW PowerShell window** (keep your PostgreSQL tunnel running in the other window):

```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 6380:dev-redis.xxxxx.0001.use1.cache.amazonaws.com:6379 ec2-user@13.216.193.180
```

**Important:**
- Use port `6380` locally (to avoid conflict with PostgreSQL on 5433)
- Redis port is `6379` on AWS
- Keep this PowerShell window open while using Redis

## Step 3: Connect to Redis

### Option 1: Using redis-cli (if installed locally)

```powershell
# Connect to Redis through tunnel
redis-cli -h localhost -p 6380

# Test connection
PING
# Should return: PONG
```

### Option 2: Using Docker (if redis-cli not installed)

```powershell
# Run redis-cli in Docker container
docker run -it --rm redis:7-alpine redis-cli -h host.docker.internal -p 6380

# Test connection
PING
# Should return: PONG
```

### Option 3: Direct from Bastion (Alternative)

If you prefer to connect directly from the bastion:

```powershell
# 1. SSH into bastion (in a new terminal)
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key ec2-user@13.216.193.180

# 2. Install Redis client (if not installed)
sudo yum install redis -y

# 3. Connect to Redis
redis-cli -h dev-redis.xxxxx.0001.use1.cache.amazonaws.com -p 6379
```

## Common Redis Commands

Once connected:

```bash
# Test connection
PING
# Returns: PONG

# List all keys
KEYS *

# List keys by pattern
KEYS "rt:*"      # Refresh tokens
KEYS "session:*" # User sessions
KEYS "bl:*"      # Blacklisted tokens
KEYS "rl:*"      # Rate limit counters

# Get a key value
GET "rt:user-id-here"

# Get key TTL (Time To Live)
TTL "rt:user-id-here"

# Delete a key
DEL "rt:user-id-here"

# Monitor all commands in real-time
MONITOR

# Get database info
INFO

# Get memory usage
INFO memory

# Exit
exit
```

## Complete Setup: Both PostgreSQL and Redis

You'll need **TWO PowerShell windows** running:

### Window 1: PostgreSQL Tunnel
```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180
```

### Window 2: Redis Tunnel
```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 6380:dev-redis.xxxxx.0001.use1.cache.amazonaws.com:6379 ec2-user@13.216.193.180
```

### Window 3: Connect to Services

**PostgreSQL (pgAdmin):**
- Host: `localhost`
- Port: `5433`

**Redis (redis-cli):**
```powershell
redis-cli -h localhost -p 6380
```

## Troubleshooting

### Issue: "Connection refused" when connecting to Redis

**Causes:**
1. SSH tunnel not running
2. Wrong Redis endpoint
3. Security group not allowing bastion → Redis

**Solution:**
```powershell
# 1. Verify tunnel is running
netstat -an | findstr 6380
# Should show: TCP  127.0.0.1:6380  ...  LISTENING

# 2. Verify Redis endpoint
cd infrastructure/terraform
terraform output redis_endpoint

# 3. Check security group (bastion should have access to Redis)
# This is configured in Terraform automatically
```

### Issue: "Could not connect to Redis"

**Solution:**
1. Make sure SSH tunnel window is still open
2. Verify Redis endpoint is correct
3. Try connecting directly from bastion to test

### Issue: Port 6380 already in use

**Solution:**
Use a different local port:
```powershell
# Use port 6381 instead
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 6381:dev-redis.xxxxx.0001.use1.cache.amazonaws.com:6379 ec2-user@13.216.193.180

# Then connect with:
redis-cli -h localhost -p 6381
```

## Quick Reference

### Get Connection Info
```powershell
cd infrastructure/terraform
terraform output redis_endpoint
terraform output redis_port
terraform output bastion_public_ip
```

### Create Redis Tunnel
```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 6380:<REDIS_ENDPOINT>:6379 ec2-user@<BASTION_IP>
```

### Connect to Redis
```powershell
# Local redis-cli
redis-cli -h localhost -p 6380

# Or Docker
docker run -it --rm redis:7-alpine redis-cli -h host.docker.internal -p 6380
```

### Redis Key Patterns in Vector App
- `rt:{userId}` - Refresh tokens
- `session:{userId}` - User sessions
- `bl:{token}` - Blacklisted tokens
- `rl:{endpoint}:{userId}` - Rate limit counters

---

**Last Updated:** December 7, 2025

