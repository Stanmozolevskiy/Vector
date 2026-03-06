# Render Troubleshooting Guide

## Viewing Logs

### Backend logs (vector-backend-qa or vector-backend-prod)
1. Go to [Render Dashboard](https://dashboard.render.com)
2. Click **vector-backend-qa** (or vector-backend-prod)
3. Click **Logs** in the left sidebar
4. Look for:
   - `"Database migrations completed successfully"` or migration errors
   - `"Database seeding completed successfully"` or seeding errors
   - `"DEFAULT ADMIN USER CREATED"` (means admin was seeded)
   - Any `RedisConnectionException` or `DbUpdateException`
   - Login attempts: `"Login failed"` or `"Invalid email or password"`

### Frontend logs
- Frontend is a static site; there are no server logs. Use browser DevTools (F12 → Network, Console) to see failed API calls or CORS errors.

---

## Connecting to the Database

### Option 1: Render Dashboard (easiest)
1. Go to **vector-postgres-qa** (or vector-postgres-prod)
2. In the **Info** tab, find **Connections**
3. Click **Connect** → choose **External connection** or **PSQL**
4. Copy the `psql` command (includes connection string)
5. Run it in a terminal (requires `psql` installed locally)

### Option 2: Connection string from Environment
1. Go to **vector-backend-qa** → **Environment**
2. Find `ConnectionStrings__DefaultConnection` (or view the service’s env vars)
3. Use that connection string with any PostgreSQL client (pgAdmin, DBeaver, etc.)

### Verify users exist
```sql
-- Run this in psql or your SQL client after connecting
SELECT "Id", "Email", "Role", "EmailVerified", "CreatedAt" 
FROM "Users" 
ORDER BY "CreatedAt" DESC 
LIMIT 10;
```

If the query returns no rows, seeding did not run or failed.

---

## Seeded Admin Credentials

If seeding ran successfully, this user exists:

| Field | Value |
|-------|-------|
| **Email** | `admin@vector.com` |
| **Password** | `Admin@123` |

**Important:** Change this password in production.

---

## If Seeding Did Not Run

Migrations and seeding run automatically when the backend starts. If the DB is empty:

1. **Check startup logs** for migration/seeding errors (e.g. connection failures, Redis issues)
2. **Redeploy** the backend:
   - vector-backend-qa → **Manual Deploy** → **Deploy latest commit**
3. After deploy, check logs again for `"Database seeding completed successfully"`

---

## Common Login Issues

| Issue | Cause | Fix |
|-------|-------|-----|
| "Invalid email or password" | No user with that email, or wrong password | Use `admin@vector.com` / `Admin@123` if you expect the seeded admin, or register first |
| "Please verify your email" | New user, email not verified | On QA, `Development__AutoVerifyEmails` is `true` so new registrations are auto-verified. On prod, user must click the verification link. |
| CORS error in browser | Frontend origin not allowed | Backend CORS includes Render frontend URLs; redeploy backend to pick up changes |
| Network error / timeout | Backend not running or wrong URL | Verify backend URL in frontend (VITE_API_URL) and that the service is deployed |
| 500 on login | Backend exception (DB, Redis, etc.) | Check backend logs for the exact error |
| "Invalid port: -1" on login | PostgreSQL connection string missing port | Fixed in code — redeploy backend. Render URLs without explicit port now default to 5432. |

---

## Programmatic Access (Render API)

To fetch logs or manage services from scripts:

1. **Create API key:** [Render Dashboard](https://dashboard.render.com) → Account Settings → API Keys → Create API Key
2. **Get Owner ID:** Dashboard URL or `curl -H "Authorization: Bearer $RENDER_API_KEY" https://api.render.com/v1/owners`
3. **Get Service ID:** Service page URL (e.g. `srv-xxx`) or `GET /v1/services`
4. **Run script:**
   ```powershell
   $env:RENDER_API_KEY = "rnd_xxx"
   $env:RENDER_OWNER_ID = "your-owner-id"
   .\scripts\render-fetch-logs.ps1 -ServiceId "srv-xxx" -Limit 100
   ```

**API docs:** https://api-docs.render.com

---

## Manual Deploy (Trigger Without Code Push)

1. **From Render:** Service → **Manual Deploy** → **Deploy latest commit**
2. **From GitHub:** Push a change to `develop` (QA) or `main` (prod); deploy hooks will trigger a deploy
