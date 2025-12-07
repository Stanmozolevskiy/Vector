# AWS Database Connection Guide

Complete guide to connect to PostgreSQL and Redis on AWS via bastion host.

## Prerequisites

✅ SSH key: `~/.ssh/dev-bastion-key` (or `$env:USERPROFILE\.ssh\dev-bastion-key` on Windows)  
✅ Docker installed (for Redis connection)  
✅ pgAdmin 4 installed (optional, for PostgreSQL GUI)  
✅ Bastion host running and accessible

## Quick Start

### Step 1: Get Connection Information

```powershell
cd infrastructure/terraform

# Get all connection info
$BASTION_IP = terraform output -raw bastion_public_ip
$DB_ENDPOINT = terraform output -raw database_endpoint
$REDIS_ENDPOINT = aws elasticache describe-replication-groups --replication-group-id dev-redis --query 'ReplicationGroups[0].NodeGroups[0].PrimaryEndpoint.Address' --output text

Write-Host "Bastion IP: $BASTION_IP"
Write-Host "PostgreSQL: $DB_ENDPOINT"
Write-Host "Redis: $REDIS_ENDPOINT"
```

### Step 2: Create SSH Tunnels

You'll need **TWO PowerShell windows** (one for each tunnel):

#### Window 1: PostgreSQL Tunnel

```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180
```

**Keep this window open!** This tunnel forwards `localhost:5433` → `PostgreSQL:5432`

#### Window 2: Redis Tunnel

```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 6380:dev-redis.fmc307.ng.0001.use1.cache.amazonaws.com:6379 ec2-user@13.216.193.180
```

**Keep this window open!** This tunnel forwards `localhost:6380` → `Redis:6379`

---

## PostgreSQL Connection

### Method 1: Using pgAdmin 4 (Recommended)

1. **Keep PostgreSQL tunnel running** (Window 1)

2. **Open pgAdmin 4**

3. **Add New Server:**
   - Right-click "Servers" → Create → Server

4. **General Tab:**
   - Name: `Vector Dev (AWS)`

5. **Connection Tab:**
   - **Host:** `localhost` (not the RDS endpoint!)
   - **Port:** `5433` (tunnel port, not 5432!)
   - **Maintenance database:** `vector_db`
   - **Username:** `postgres`
   - **Password:** (your RDS password from terraform.tfvars)
   - ✅ Save password (optional)

6. **SSL Tab:**
   - SSL mode: `Prefer` (or `Disable` for dev)

7. **Click "Save"**

You should now see the database and tables!

### Method 2: Using psql Command Line

```powershell
# With PostgreSQL tunnel running (Window 1)
psql -h localhost -p 5433 -U postgres -d vector_db
```

Enter your password when prompted.

### Method 3: Direct from Bastion

```powershell
# SSH into bastion
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key ec2-user@13.216.193.180

# Install PostgreSQL client (if needed)
sudo yum install postgresql15 -y

# Connect to database
psql -h dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com -U postgres -d vector_db
```

---

## Redis Connection

### Method 1: Using Docker (Recommended)

1. **Keep Redis tunnel running** (Window 2)

2. **Connect using Docker:**

```powershell
docker run -it --rm redis:7-alpine redis-cli -h host.docker.internal -p 6380
```

**Note:** `host.docker.internal` allows Docker containers to access services on your host machine (localhost).

3. **Test connection:**
```bash
PING
# Should return: PONG
```

### Method 2: Using redis-cli (if installed locally)

```powershell
# With Redis tunnel running (Window 2)
redis-cli -h localhost -p 6380
```

### Method 3: Direct from Bastion

```powershell
# SSH into bastion
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key ec2-user@13.216.193.180

# Install Redis client (if needed)
sudo yum install redis -y

# Connect to Redis
redis-cli -h dev-redis.fmc307.ng.0001.use1.cache.amazonaws.com -p 6379
```

---

## Complete Setup Example

### Terminal Setup

You'll have **3 terminal windows**:

**Window 1: PostgreSQL Tunnel**
```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180
```

**Window 2: Redis Tunnel**
```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 6380:dev-redis.fmc307.ng.0001.use1.cache.amazonaws.com:6379 ec2-user@13.216.193.180
```

**Window 3: Connect to Services**
```powershell
# PostgreSQL
psql -h localhost -p 5433 -U postgres -d vector_db

# Redis (using Docker)
docker run -it --rm redis:7-alpine redis-cli -h host.docker.internal -p 6380
```

---

## Common Commands

### PostgreSQL Commands

```sql
-- List all databases
\l

-- Connect to vector_db
\c vector_db

-- List all tables
\dt

-- Describe a table
\d users

-- Run a query
SELECT * FROM "Users" LIMIT 10;

-- View migration history
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";

-- Exit
\q
```

### Redis Commands

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

---

## Troubleshooting

### Issue: SSH Connection Timeout

**Solution:**
1. Check your IP is in security group
2. Verify bastion instance is running
3. Try different network (mobile hotspot)

See `SSH_CONNECTION_FIX.md` for detailed troubleshooting.

### Issue: PostgreSQL Connection Refused

**Causes:**
- PostgreSQL tunnel not running
- Wrong port (should be 5433, not 5432)
- Wrong host (should be localhost, not RDS endpoint)

**Solution:**
```powershell
# Verify tunnel is running
netstat -an | findstr 5433
# Should show: TCP  127.0.0.1:5433  ...  LISTENING

# Check tunnel window is still open
# Restart tunnel if needed
```

### Issue: Redis Connection Refused (Docker)

**Causes:**
- Redis tunnel not running
- Wrong port (should be 6380)
- Docker can't reach host.docker.internal

**Solution:**
```powershell
# Verify tunnel is running
netstat -an | findstr 6380
# Should show: TCP  127.0.0.1:6380  ...  LISTENING

# Try alternative Docker command
docker run -it --rm --network host redis:7-alpine redis-cli -h 127.0.0.1 -p 6380
```

### Issue: "host.docker.internal" Not Resolved

**Solution (Windows):**
```powershell
# Use IP address instead
docker run -it --rm redis:7-alpine redis-cli -h 172.17.0.1 -p 6380

# Or use host network mode
docker run -it --rm --network host redis:7-alpine redis-cli -h localhost -p 6380
```

**Solution (macOS/Linux):**
```bash
# Use host network mode
docker run -it --rm --network host redis:7-alpine redis-cli -h localhost -p 6380
```

### Issue: Port Already in Use

**Solution:**
Use different local ports:
```powershell
# PostgreSQL on 5434 instead of 5433
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5434:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180

# Redis on 6381 instead of 6380
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 6381:dev-redis.fmc307.ng.0001.use1.cache.amazonaws.com:6379 ec2-user@13.216.193.180
```

---

## Quick Reference

### Get Connection Info

```powershell
cd infrastructure/terraform

# PostgreSQL
terraform output database_endpoint
terraform output database_port

# Redis
aws elasticache describe-replication-groups --replication-group-id dev-redis --query 'ReplicationGroups[0].NodeGroups[0].PrimaryEndpoint.Address' --output text

# Bastion
terraform output bastion_public_ip
```

### SSH Tunnel Commands

**PostgreSQL:**
```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180
```

**Redis:**
```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 6380:dev-redis.fmc307.ng.0001.use1.cache.amazonaws.com:6379 ec2-user@13.216.193.180
```

### Connection Strings

**PostgreSQL (pgAdmin):**
- Host: `localhost`
- Port: `5433`
- Database: `vector_db`
- Username: `postgres`
- Password: (from terraform.tfvars)

**Redis (Docker):**
```powershell
docker run -it --rm redis:7-alpine redis-cli -h host.docker.internal -p 6380
```

---

## Security Notes

⚠️ **Important:**
- Never expose databases publicly
- Always use SSH tunnels
- Keep tunnel windows open while using databases
- Close tunnels when done
- Use strong passwords
- Rotate credentials regularly

---

## Cost Optimization

### Stop Bastion When Not Needed

```powershell
# Stop bastion (saves ~$7.50/month)
cd infrastructure/terraform
$INSTANCE_ID = terraform output -raw bastion_instance_id
aws ec2 stop-instances --instance-ids $INSTANCE_ID

# Start when needed
aws ec2 start-instances --instance-ids $INSTANCE_ID
aws ec2 wait instance-running --instance-ids $INSTANCE_ID
```

---

**Last Updated:** December 7, 2025  
**Environment:** Dev (us-east-1)

