# AWS Connection Guide - PostgreSQL & Redis

Simple step-by-step guide to connect to your AWS PostgreSQL and Redis instances.

---

## Prerequisites

1. **AWS CLI installed and configured**
   ```bash
   aws --version
   aws configure
   ```

2. **Terraform outputs available** (if infrastructure was deployed with Terraform)
   ```bash
   cd infrastructure/terraform
   terraform output
   ```

3. **SSH key for bastion host** (if using bastion)
   - Key should be in `~/.ssh/` directory
   - Default name: `dev-bastion-key.pem` or `dev-bastion-key`

---

## Step 1: Get Connection Information

### Option A: Using Terraform Outputs (Recommended)

```bash
cd infrastructure/terraform
terraform output
```

This will show:
- `database_endpoint` - PostgreSQL RDS endpoint
- `database_port` - PostgreSQL port (usually 5432)
- `redis_endpoint` - Redis ElastiCache endpoint
- `redis_port` - Redis port (usually 6379)
- `bastion_public_ip` - Bastion host public IP
- `bastion_ssh_command` - Ready-to-use SSH command
- `db_connection_via_bastion` - Connection instructions

### Option B: Using AWS Console

1. **PostgreSQL (RDS):**
   - Go to: AWS Console → RDS → Databases
   - Find: `dev-postgres` (or your environment name)
   - Copy: **Endpoint** and **Port**

2. **Redis (ElastiCache):**
   - Go to: AWS Console → ElastiCache → Redis clusters
   - Find: `dev-redis` (or your environment name)
   - Copy: **Primary endpoint** and **Port**

3. **Bastion Host:**
   - Go to: AWS Console → EC2 → Instances
   - Find: `dev-bastion-host` (or your environment name)
   - Copy: **Public IPv4 address**

---

## Step 2: Find Bastion SSH Command

### Method 1: From Terraform Outputs

```bash
cd infrastructure/terraform
terraform output bastion_ssh_command
```

**Example output:**
```bash
ssh -i ~/.ssh/dev-bastion-key.pem ec2-user@54.123.45.67
```

### Method 2: From AWS Console

1. Go to: AWS Console → EC2 → Instances
2. Select your bastion instance
3. Click **Connect**
4. Choose **SSH client** tab
5. Copy the SSH command shown

### Method 3: Manual Construction

```bash
ssh -i ~/.ssh/dev-bastion-key.pem ec2-user@<BASTION_PUBLIC_IP>
```

Replace:
- `dev-bastion-key.pem` with your actual key name
- `<BASTION_PUBLIC_IP>` with bastion's public IP

---

## Step 3: Connect to PostgreSQL

### Method 1: Via Bastion (SSH Tunnel) - Recommended

**Step 3.1: Create SSH Tunnel**

```bash
# Get database endpoint from Terraform
cd infrastructure/terraform
DB_ENDPOINT=$(terraform output -raw database_endpoint | cut -d: -f1)

# Create SSH tunnel (runs in background)
ssh -i ~/.ssh/dev-bastion-key.pem \
    -L 5433:${DB_ENDPOINT}:5432 \
    -N \
    ec2-user@<BASTION_PUBLIC_IP>
```

**Step 3.2: Connect to PostgreSQL**

In a **new terminal**, connect using the tunnel:

```bash
# Using psql (if installed locally)
psql -h localhost -p 5433 -U postgres -d vector_db

# Or using connection string
psql "host=localhost port=5433 dbname=vector_db user=postgres"
```

**Password:** Enter the database password (from Terraform variables or AWS Secrets Manager)

### Method 2: Direct from Bastion

**Step 3.1: SSH into Bastion**

```bash
ssh -i ~/.ssh/dev-bastion-key.pem ec2-user@<BASTION_PUBLIC_IP>
```

**Step 3.2: Install PostgreSQL Client (if not installed)**

```bash
sudo yum install postgresql15 -y
```

**Step 3.3: Connect to Database**

```bash
# Get database endpoint
DB_ENDPOINT=$(aws rds describe-db-instances \
  --query 'DBInstances[?DBInstanceIdentifier==`dev-postgres`].Endpoint.Address' \
  --output text)

# Connect
psql -h ${DB_ENDPOINT} -U postgres -d vector_db
```

### Method 3: Using Connection String

```bash
# From your local machine (via tunnel)
psql "host=localhost port=5433 dbname=vector_db user=postgres password=YOUR_PASSWORD"
```

---

## Step 4: Connect to Redis

### Method 1: Via Bastion (SSH Tunnel) - Recommended

**Step 4.1: Create SSH Tunnel for Redis**

```bash
# Get Redis endpoint from Terraform
cd infrastructure/terraform
REDIS_ENDPOINT=$(terraform output -raw redis_endpoint | cut -d: -f1)

# Create SSH tunnel (runs in background)
ssh -i ~/.ssh/dev-bastion-key.pem \
    -L 6380:${REDIS_ENDPOINT}:6379 \
    -N \
    ec2-user@<BASTION_PUBLIC_IP>
```

**Step 4.2: Connect to Redis**

In a **new terminal**:

```bash
# Using redis-cli (if installed locally)
redis-cli -h localhost -p 6380

# Or if using Docker
docker run -it --rm redis:7-alpine redis-cli -h host.docker.internal -p 6380
```

### Method 2: Direct from Bastion

**Step 4.1: SSH into Bastion**

```bash
ssh -i ~/.ssh/dev-bastion-key.pem ec2-user@<BASTION_PUBLIC_IP>
```

**Step 4.2: Install Redis Client (if not installed)**

```bash
sudo yum install redis -y
# OR
sudo yum install gcc make -y
wget https://download.redis.io/redis-stable.tar.gz
tar xzf redis-stable.tar.gz
cd redis-stable
make
sudo make install
```

**Step 4.3: Connect to Redis**

```bash
# Get Redis endpoint
REDIS_ENDPOINT=$(aws elasticache describe-replication-groups \
  --replication-group-id dev-redis \
  --query 'ReplicationGroups[0].NodeGroups[0].PrimaryEndpoint.Address' \
  --output text)

# Connect
redis-cli -h ${REDIS_ENDPOINT} -p 6379
```

### Method 3: Using Docker from Local Machine

```bash
# With SSH tunnel running on port 6380
docker run -it --rm redis:7-alpine redis-cli -h host.docker.internal -p 6380
```

---

## Quick Reference Commands

### Get All Connection Info at Once

```bash
cd infrastructure/terraform

# PostgreSQL
echo "PostgreSQL Endpoint: $(terraform output -raw database_endpoint)"
echo "PostgreSQL Port: $(terraform output -raw database_port)"

# Redis
echo "Redis Endpoint: $(terraform output -raw redis_endpoint)"
echo "Redis Port: $(terraform output -raw redis_port)"

# Bastion
echo "Bastion IP: $(terraform output -raw bastion_public_ip)"
echo "SSH Command: $(terraform output -raw bastion_ssh_command)"
```

### One-Line SSH Tunnel Commands

**PostgreSQL:**
```bash
ssh -i ~/.ssh/dev-bastion-key.pem -L 5433:$(cd infrastructure/terraform && terraform output -raw database_endpoint | cut -d: -f1):5432 -N ec2-user@$(cd infrastructure/terraform && terraform output -raw bastion_public_ip)
```

**Redis:**
```bash
ssh -i ~/.ssh/dev-bastion-key.pem -L 6380:$(cd infrastructure/terraform && terraform output -raw redis_endpoint | cut -d: -f1):6379 -N ec2-user@$(cd infrastructure/terraform && terraform output -raw bastion_public_ip)
```

---

## Common PostgreSQL Commands

Once connected to PostgreSQL:

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
SELECT * FROM users LIMIT 10;

-- Exit
\q
```

---

## Common Redis Commands

Once connected to Redis:

```bash
# Test connection
PING
# Should return: PONG

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

# Exit
exit
```

---

## Troubleshooting

### Issue: "Permission denied (publickey)"

**Solution:**
```bash
# Check key permissions
chmod 400 ~/.ssh/dev-bastion-key.pem

# Verify key exists
ls -la ~/.ssh/dev-bastion-key.pem
```

### Issue: "Connection refused" or "Connection timeout"

**Solutions:**
1. **Check Security Groups:**
   - Bastion: Should allow SSH (port 22) from your IP
   - RDS: Should allow PostgreSQL (port 5432) from bastion security group
   - Redis: Should allow Redis (port 6379) from bastion security group

2. **Verify endpoints:**
   ```bash
   # Test bastion connectivity
   ping <BASTION_PUBLIC_IP>
   
   # Test from bastion to RDS
   ssh -i ~/.ssh/dev-bastion-key.pem ec2-user@<BASTION_IP>
   telnet <RDS_ENDPOINT> 5432
   ```

### Issue: "Database does not exist"

**Solution:**
```sql
-- List databases
\l

-- Create database if needed (from postgres database)
CREATE DATABASE vector_db;
```

### Issue: "Redis connection refused"

**Solutions:**
1. Check Redis endpoint is correct
2. Verify security group allows bastion access
3. Test connectivity from bastion:
   ```bash
   redis-cli -h <REDIS_ENDPOINT> -p 6379 PING
   ```

---

## Security Notes

⚠️ **Important:**
- Never commit SSH keys or passwords to git
- Use AWS Secrets Manager for database passwords in production
- Restrict bastion SSH access to your IP only
- Use SSH tunnels instead of exposing databases publicly
- Rotate passwords regularly

---

## Example: Complete Connection Workflow

```bash
# 1. Get all connection info
cd infrastructure/terraform
export BASTION_IP=$(terraform output -raw bastion_public_ip)
export DB_ENDPOINT=$(terraform output -raw database_endpoint | cut -d: -f1)
export REDIS_ENDPOINT=$(terraform output -raw redis_endpoint | cut -d: -f1)

# 2. Create SSH tunnels (in background)
ssh -i ~/.ssh/dev-bastion-key.pem -L 5433:${DB_ENDPOINT}:5432 -N -f ec2-user@${BASTION_IP}
ssh -i ~/.ssh/dev-bastion-key.pem -L 6380:${REDIS_ENDPOINT}:6379 -N -f ec2-user@${BASTION_IP}

# 3. Connect to PostgreSQL
psql -h localhost -p 5433 -U postgres -d vector_db

# 4. Connect to Redis (in another terminal)
redis-cli -h localhost -p 6380
```

---

**Last Updated:** December 7, 2025  
**Environment:** Dev (us-east-1)

