# Judge0 Health Endpoint Fix

## Issue
The `/health` endpoint returns 404 when accessing `http://localhost:2358/health`.

## Root Cause
Judge0 **does not have a `/health` endpoint** by default. This is expected behavior - Judge0 doesn't provide a health check endpoint in its API.

## Solution
Updated the Docker healthcheck to use `/languages` endpoint instead, which is a valid Judge0 endpoint that returns the list of supported languages.

### Changes Made:
1. **docker-compose.yml**: Updated healthcheck from `/health` to `/languages`
   ```yaml
   healthcheck:
     test: ["CMD", "curl", "-f", "http://localhost:2358/languages"]
   ```

2. **Language ID Mapping**: Updated CodeExecutionService to use correct language IDs for Judge0 1.13.0:
   - Python 3: ID 71 (Python 3.8.1)
   - JavaScript: ID 63 (Node.js 12.14.0)
   - Java: ID 62 (OpenJDK 13.0.1)
   - C++: ID 54 (GCC 9.2.0) ✓
   - C#: ID 51 (Mono 6.6.0.161) ✓
   - Go: ID 60 (Go 1.13.5) ✓

## Verification
Judge0 is working correctly. You can verify by:
```bash
# Test from host
Invoke-WebRequest -Uri "http://localhost:2358/languages" -UseBasicParsing

# Test from container
docker exec vector-judge0 curl -s http://localhost:2358/languages
```

Both should return a JSON array of supported languages.

## Status
✅ Judge0 is running and functional
✅ `/languages` endpoint works correctly
✅ Code execution service is configured with correct language IDs
⚠️ Container may show as "unhealthy" until healthcheck passes (this is cosmetic - service works)

## Note
The container health status showing "unhealthy" is due to the old healthcheck configuration. After the restart with the new healthcheck, it should become healthy within 30-60 seconds. This does not affect Judge0 functionality - the service is working correctly.

