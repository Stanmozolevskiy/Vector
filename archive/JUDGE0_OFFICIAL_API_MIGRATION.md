# Judge0 Official API Migration

## Summary

Migrated from self-hosted Judge0 (Docker/EC2) to Judge0 Official API (free tier) for MVP. This simplifies deployment and removes infrastructure complexity.

**Date:** December 16, 2025

---

## Changes Made

### 1. Removed Self-Hosted Judge0 Infrastructure

#### Terraform Files Removed
- `infrastructure/terraform/modules/judge0/main.tf`
- `infrastructure/terraform/modules/judge0/variables.tf`
- `infrastructure/terraform/modules/judge0/outputs.tf`
- `infrastructure/terraform/modules/judge0/judge0-user-data.sh`

#### Terraform Configuration Updated
- **`infrastructure/terraform/main.tf`**:
  - Removed Judge0 module declaration
  - Removed `judge0_endpoint` from ECS module
  - Removed security group rule for ECS → Judge0

- **`infrastructure/terraform/variables.tf`**:
  - Removed `judge0_instance_type`
  - Removed `judge0_use_elastic_ip`
  - Removed `judge0_db_password`
  - Removed `judge0_rabbitmq_password`

- **`infrastructure/terraform/outputs.tf`**:
  - Removed `judge0_endpoint` output
  - Removed `judge0_private_ip` output
  - Removed `judge0_instance_id` output

- **`infrastructure/terraform/modules/ecs/task_definition.tf`**:
  - Removed `Judge0__BaseUrl` environment variable from backend task definition

- **`infrastructure/terraform/modules/ecs/variables.tf`**:
  - Removed `judge0_endpoint` variable

#### Docker Files Removed
- `docker/init-judge0-db.sh`
- `docker/init-judge0-db.sql`

#### Docker Compose Updated
- **`docker/docker-compose.yml`**:
  - Removed `judge0-ce` service (entire service block)
  - Removed `rabbitmq` service (no longer needed)
  - Updated backend environment variables:
    - Changed `Judge0__BaseUrl` from `http://judge0-ce:2358` to `${JUDGE0_BASE_URL:-https://ce.judge0.com}`
    - Added `Judge0__ApiKey` environment variable (optional, for paid tier)

### 2. Backend Code Updates

#### Configuration
- **`backend/Vector.Api/appsettings.json`**:
  ```json
  "Judge0": {
    "BaseUrl": "https://ce.judge0.com",
    "ApiKey": ""
  }
  ```

#### Service Configuration
- **`backend/Vector.Api/Program.cs`**:
  - Updated HttpClient configuration to use Judge0 Official API
  - Changed default URL from `http://localhost:2358` to `https://ce.judge0.com`
  - Increased timeout from 30s to 60s (API calls may take longer)
  - Added support for API key header (for paid tier via RapidAPI)
  - Removed localhost-specific configuration

#### Language ID Mapping
- **`backend/Vector.Api/Services/CodeExecutionService.cs`**:
  - Updated language ID mapping for Judge0 Official API:
    - Python: 71 → **92** (Python 3.11.1)
    - JavaScript: 63 → **93** (Node.js 18.15.0)
    - Java: 62 → **91** (Java 17.0.2)
    - C++, C#, Go: IDs remain the same
  - Updated comments to reference Official API
  - No changes to execution logic (API is compatible)

---

## Judge0 Official API Details

### Free Tier
- **Base URL**: `https://ce.judge0.com`
- **Rate Limits**: 
  - 100 requests per day (free tier)
  - 10 requests per minute
- **Supported Languages**: 60+ languages
- **No API Key Required**: For free tier
- **Documentation**: https://ce.judge0.com/docs

### Paid Tier (Optional)
- **Base URL**: `https://judge0-ce.p.rapidapi.com` (via RapidAPI)
- **API Key**: Required (set in `Judge0__ApiKey`)
- **Higher Rate Limits**: Based on subscription
- **Better Performance**: Faster response times

### Language IDs (Official API)
| Language | ID | Version |
|----------|-----|---------|
| Python 3 | 92 | 3.11.1 |
| JavaScript | 93 | Node.js 18.15.0 |
| Java | 91 | 17.0.2 |
| C++ | 54 | GCC 9.2.0 |
| C# | 51 | Mono 6.6.0.161 |
| Go | 60 | 1.19.5 |

---

## Environment Variables

### Local Development (Docker)
```bash
# Optional: Override Judge0 URL (defaults to https://ce.judge0.com)
JUDGE0_BASE_URL=https://ce.judge0.com

# Optional: API key for paid tier (leave empty for free tier)
JUDGE0_API_KEY=
```

### AWS Deployment
Set in ECS task definition or environment variables:
- `Judge0__BaseUrl`: `https://ce.judge0.com` (or RapidAPI URL for paid tier)
- `Judge0__ApiKey`: Leave empty for free tier, or set RapidAPI key for paid tier

---

## Migration Benefits

### Simplified Infrastructure
- ✅ No EC2 instance needed for Judge0
- ✅ No Docker containers for Judge0 services
- ✅ No RabbitMQ dependency
- ✅ Reduced infrastructure costs (~$33/month saved)
- ✅ No maintenance overhead

### Improved Reliability
- ✅ Managed service (no downtime for updates)
- ✅ Better scalability
- ✅ No cgroups/isolate configuration issues
- ✅ Works on all platforms (Windows, Linux, macOS)

### Faster Development
- ✅ No local setup required
- ✅ Works immediately after configuration
- ✅ No Docker/Windows compatibility issues

---

## Limitations (Free Tier)

### Rate Limits
- **100 requests/day**: May be limiting for development/testing
- **10 requests/minute**: Should be sufficient for MVP

### Solutions
1. **Use Paid Tier**: Upgrade to RapidAPI for higher limits
2. **Self-Host Later**: Can migrate back to self-hosted when needed
3. **Caching**: Implement result caching to reduce API calls

---

## Testing

### Verify Configuration
```powershell
# Test from backend API
POST http://localhost:5000/api/CodeExecution/execute
{
  "sourceCode": "print('Hello from Python!');",
  "language": "python",
  "stdin": ""
}
```

### Expected Response
```json
{
  "status": "Accepted",
  "output": "Hello from Python!\n",
  "error": "",
  "runtime": 123.45,
  "memory": 1024
}
```

---

## Rollback Plan

If needed, can rollback to self-hosted Judge0:
1. Restore Terraform module files
2. Restore Docker Compose configuration
3. Revert language ID mappings
4. Redeploy infrastructure

---

## Next Steps

1. ✅ **Completed**: Remove self-hosted infrastructure
2. ✅ **Completed**: Update backend to use Official API
3. ⏳ **Optional**: Set up RapidAPI account for paid tier (if needed)
4. ⏳ **Optional**: Implement caching to reduce API calls
5. ⏳ **Future**: Consider self-hosting when scale requires it

---

## Documentation Files

- `JUDGE0_OFFICIAL_API_MIGRATION.md` - This file
- `JUDGE0_WINDOWS_LIMITATION.md` - Previous documentation (can be archived)
- `JUDGE0_AWS_DEPLOYMENT.md` - Previous documentation (can be archived)
- `JUDGE0_AWS_DEPLOYMENT_QUICK_START.md` - Previous documentation (can be archived)

---

## References

- [Judge0 Official API](https://ce.judge0.com)
- [Judge0 Documentation](https://ce.judge0.com/docs)
- [Judge0 Languages](https://ce.judge0.com/languages)
- [RapidAPI Judge0](https://rapidapi.com/judge0-official/api/judge0-ce) - Paid tier option

## Related Documentation

- `JUDGE0_OFFICIAL_API_LIMITATIONS.md` - Detailed limitations and rate limits

