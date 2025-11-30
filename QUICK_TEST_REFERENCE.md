# Quick Test Reference

## Backend Testing (AWS)

### Quick Health Check
```powershell
$backendUrl = "http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com"
Invoke-WebRequest -Uri "$backendUrl/api/health" -UseBasicParsing | ConvertFrom-Json
```

### Check Database Connection (via logs)
```powershell
aws logs tail /ecs/dev-vector --since 10m --region us-east-1 --filter-pattern "migration" --format short
```

### Test API Endpoints
```powershell
# Health
Invoke-WebRequest -Uri "$backendUrl/api/health" -UseBasicParsing

# API Root
Invoke-WebRequest -Uri "$backendUrl/api" -UseBasicParsing
```

---

## Frontend Testing (AWS)

### Quick Access
```powershell
$frontendUrl = "http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com"
Start-Process $frontendUrl
```

### Test Frontend Health
```powershell
Invoke-WebRequest -Uri "$frontendUrl/health" -UseBasicParsing
```

### Test Frontend-Backend Communication
1. Open frontend in browser
2. Press `F12` → **Network** tab
3. Filter by `XHR` or `Fetch`
4. Look for requests to `/api/*`
5. Check request URLs and status codes

---

## Database Testing

### Connect to Database (via Docker)
```powershell
# Get database endpoint
cd infrastructure/terraform
$dbEndpoint = terraform output -raw database_endpoint
$dbEndpoint = $dbEndpoint -replace ':5432', ''

# Connect using Docker
docker run -it --rm postgres:15 psql `
  -h $dbEndpoint `
  -U postgres `
  -d vector_db
```

### Check Tables
```sql
-- List all tables
\dt

-- Check migration history
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
```

---

## Staging Deployment

### Step 1: Deploy Infrastructure
```powershell
cd infrastructure/terraform
terraform apply `
  -var="environment=staging" `
  -var="vpc_cidr=10.1.0.0/16" `
  -var="db_instance_class=db.t3.small" `
  -var="redis_node_type=cache.t3.small" `
  -var='db_password=YourStagingPassword123!' `
  -auto-approve
```

### Step 2: Add GitHub Secrets
- `STAGING_DB_CONNECTION_STRING`
- `STAGING_API_URL`

### Step 3: Deploy
```powershell
git checkout -b staging
git push origin staging
```

---

**For detailed instructions, see:**
- `STAGING_DEPLOYMENT_GUIDE.md` - Complete staging setup
- `BACKEND_TESTING_GUIDE.md` - Backend testing procedures
- `FRONTEND_TESTING_GUIDE.md` - Frontend testing procedures

