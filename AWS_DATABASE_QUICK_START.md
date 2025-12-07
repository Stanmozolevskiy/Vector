# AWS Database Quick Start Guide

Simple step-by-step guide to connect to PostgreSQL and Redis on AWS.

---

## Step 1: Add Your IP to Security Group

```powershell
cd infrastructure/terraform

# Get your current IP
$MY_IP = (Invoke-RestMethod -Uri "https://api.ipify.org?format=json").ip
Write-Host "Your IP: $MY_IP"

# Get security group ID
$INSTANCE_ID = terraform output -raw bastion_instance_id
$SG_ID = aws ec2 describe-instances --instance-ids $INSTANCE_ID --query 'Reservations[0].Instances[0].SecurityGroups[0].GroupId' --output text

# Add your IP with /32
aws ec2 authorize-security-group-ingress --group-id $SG_ID --protocol tcp --port 22 --cidr "$MY_IP/32"

# Wait 15 seconds for propagation
Start-Sleep -Seconds 15
```

---

## Step 2: Create PostgreSQL SSH Tunnel

**Open PowerShell Window 1** and run:

```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180
```

**Keep this window open!**

---

## Step 3: Create Redis SSH Tunnel

**Open PowerShell Window 2** and run:

```powershell
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key -L 6380:dev-redis.fmc307.ng.0001.use1.cache.amazonaws.com:6379 ec2-user@13.216.193.180
```

**Keep this window open!**

---

## Step 4: Connect to Redis with Docker

**Open PowerShell Window 3** and run:

```powershell
docker run -it --rm redis:7-alpine redis-cli -h host.docker.internal -p 6380
```

---

## Step 5: Simple Redis Commands

Once connected to Redis:

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

# Get a key value
GET "rt:user-id"

# Get key TTL (Time To Live)
TTL "rt:user-id"

# Delete a key
DEL "rt:user-id"

# Monitor all commands in real-time
MONITOR

# Exit
exit
```

---

## Quick Reference

### Get Connection Info
```powershell
cd infrastructure/terraform
terraform output bastion_public_ip
terraform output database_endpoint
aws elasticache describe-replication-groups --replication-group-id dev-redis --query 'ReplicationGroups[0].NodeGroups[0].PrimaryEndpoint.Address' --output text
```

### PostgreSQL (pgAdmin)
- Host: `localhost`
- Port: `5433`
- Database: `vector_db`
- Username: `postgres`
- Password: (from terraform.tfvars)

### Redis (Docker)
```powershell
docker run -it --rm redis:7-alpine redis-cli -h host.docker.internal -p 6380
```

---

**Last Updated:** December 7, 2025

