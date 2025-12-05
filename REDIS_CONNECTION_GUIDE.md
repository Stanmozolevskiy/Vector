# Redis Connection Guide

This guide explains how to connect to Redis locally, similar to how you would connect to SQL Server using SSMS.

## Option 1: Redis CLI (Command Line) - Built-in

The easiest way to connect to Redis is using the Redis CLI that comes with Redis.

### Connect to Local Redis (Docker Container)

```bash
# Connect to Redis container
docker exec -it vector-redis redis-cli

# Or if Redis is running on localhost (not in Docker)
redis-cli
```

### Basic Commands

Once connected, you can run Redis commands:

```bash
# List all keys
KEYS *

# Get a specific key
GET "rt:{user-id}"
GET "session:{user-id}"
GET "bl:{token}"

# List keys matching a pattern
KEYS "rt:*"      # All refresh tokens
KEYS "session:*" # All sessions
KEYS "bl:*"      # All blacklisted tokens
KEYS "rl:*"      # All rate limit counters

# Get key with TTL (Time To Live)
TTL "rt:{user-id}"

# Delete a key
DEL "rt:{user-id}"

# Get all keys and their values (for a pattern)
KEYS "rt:*" | xargs redis-cli MGET

# Monitor all commands in real-time
MONITOR

# Get database info
INFO

# Flush all data (CAREFUL!)
FLUSHDB

# Exit
EXIT
```

### Example: View All Refresh Tokens

```bash
# Connect
docker exec -it vector-redis redis-cli

# List all refresh token keys
KEYS "rt:*"

# Get a specific token (replace {user-id} with actual GUID)
GET "rt:12345678-1234-1234-1234-123456789012"

# Check TTL (expiration time in seconds)
TTL "rt:12345678-1234-1234-1234-123456789012"
```

---

## Option 2: RedisInsight (GUI Tool) - Recommended

RedisInsight is a free GUI tool for Redis, similar to SSMS for SQL Server.

### Installation

1. **Download RedisInsight:**
   - Visit: https://redis.com/redis-enterprise/redis-insight/
   - Download for Windows/Mac/Linux
   - Install the application

2. **Connect to Local Redis:**

   **If Redis is in Docker:**
   - Host: `localhost` or `127.0.0.1`
   - Port: `6379` (default Redis port)
   - Username: (leave empty if no auth)
   - Password: (leave empty if no auth)
   - Database Alias: `Vector Local`

   **If Redis is running directly:**
   - Host: `localhost`
   - Port: `6379`
   - Username: (leave empty)
   - Password: (leave empty)

3. **Connect:**
   - Click "Add Redis Database"
   - Enter connection details
   - Click "Add Redis Database"
   - Click on the database to connect

### Features in RedisInsight

- **Browser**: View all keys, search, filter
- **CLI**: Run Redis commands in a terminal
- **Profiler**: Monitor commands in real-time
- **Slow Log**: View slow queries
- **Memory Analysis**: Analyze memory usage
- **JSON Viewer**: View JSON data formatted

### Example: View Refresh Tokens in RedisInsight

1. Open RedisInsight
2. Connect to `localhost:6379`
3. Click "Browser" tab
4. In the search box, type: `rt:*`
5. Click on any key to view its value
6. View TTL (expiration) in the key details

---

## Option 3: VS Code Extension

If you use VS Code, you can install a Redis extension:

1. **Install Extension:**
   - Open VS Code
   - Go to Extensions (Ctrl+Shift+X)
   - Search for "Redis" by cweijan
   - Install "Redis" extension

2. **Connect:**
   - Click the Redis icon in the sidebar
   - Click "+" to add connection
   - Host: `localhost`
   - Port: `6379`
   - Click "Connect"

3. **Use:**
   - Browse keys in the sidebar
   - Click keys to view values
   - Right-click to delete keys
   - Use the terminal to run commands

---

## Option 4: Another Redis Desktop Manager (Another Redis Desktop Manager)

A lightweight, open-source Redis GUI.

### Installation

1. **Download:**
   - Visit: https://github.com/qishibo/AnotherRedisDesktopManager/releases
   - Download for Windows/Mac/Linux
   - Install

2. **Connect:**
   - Click "New Connection"
   - Name: `Vector Local`
   - Host: `localhost`
   - Port: `6379`
   - Click "Test Connection"
   - Click "OK"

3. **Use:**
   - Browse keys in the left panel
   - Click keys to view values
   - Edit/Delete keys
   - Run commands in the terminal

---

## Common Redis Keys in Vector Application

| Key Pattern | Description | Example |
|------------|-------------|---------|
| `rt:{user-id}` | Refresh token for user | `rt:12345678-1234-1234-1234-123456789012` |
| `bl:{token}` | Blacklisted token | `bl:eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...` |
| `session:{user-id}` | Cached user session | `session:12345678-1234-1234-1234-123456789012` |
| `rl:{key}` | Rate limit counter | `rl:login:user@example.com` |

---

## Useful Commands for Debugging

```bash
# Connect to Redis
docker exec -it vector-redis redis-cli

# View all keys
KEYS *

# Count keys by pattern
KEYS "rt:*" | wc -l

# Get all refresh tokens
KEYS "rt:*"

# Get all sessions
KEYS "session:*"

# Get all blacklisted tokens
KEYS "bl:*"

# Get all rate limit counters
KEYS "rl:*"

# View a specific user's refresh token
GET "rt:12345678-1234-1234-1234-123456789012"

# View a cached session
GET "session:12345678-1234-1234-1234-123456789012"

# Check if a token is blacklisted
EXISTS "bl:eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# View rate limit attempts for login
GET "rl:login:user@example.com"

# Clear all refresh tokens (CAREFUL!)
KEYS "rt:*" | xargs redis-cli DEL

# Clear all sessions
KEYS "session:*" | xargs redis-cli DEL

# Clear all rate limits
KEYS "rl:*" | xargs redis-cli DEL

# Monitor all commands in real-time
MONITOR

# Get Redis server info
INFO server
INFO memory
INFO stats

# Get database size
DBSIZE
```

---

## Troubleshooting

### Cannot Connect to Redis

1. **Check if Redis container is running:**
   ```bash
   docker ps | grep redis
   ```

2. **Check Redis logs:**
   ```bash
   docker logs vector-redis
   ```

3. **Test connection:**
   ```bash
   docker exec -it vector-redis redis-cli PING
   # Should return: PONG
   ```

### Redis Connection String

The connection string is configured in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

For Docker, it's usually `localhost:6379` or `redis:6379` (if using Docker network).

---

## Recommended Tool

**For beginners:** RedisInsight (easiest GUI, similar to SSMS)  
**For developers:** Redis CLI (fast, built-in)  
**For VS Code users:** Redis extension (integrated workflow)

---

## Next Steps

1. Install RedisInsight or use Redis CLI
2. Connect to `localhost:6379`
3. Browse keys to see your application's data
4. Use MONITOR to watch real-time commands
5. Use the Browser/CLI to debug token issues

