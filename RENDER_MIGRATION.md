# Render.com Migration Plan
**From:** AWS (ECS Fargate + RDS + ElastiCache + ALB + NAT Gateway + Bastion)  
**To:** Render.com (Web Services + Managed PostgreSQL + Managed Redis)  
**Estimated Cost Reduction:** ~$115/mo → ~$38/mo (~67% savings)  
**Estimated Migration Time:** 1–2 hours (using Blueprint auto-provisioning)

---

## ⚡ Fast Track: Blueprint Auto-Provisioning (Recommended)

Instead of manually clicking through the Render dashboard for Phases 1–7, you can use the **`render.yaml` Blueprint** file at the repo root to provision **all 8 services at once** (2 backends, 2 frontends, 2 PostgreSQL databases, 2 Redis instances).

### Steps
- [ ] **A.1** Create a [Render account](https://render.com) and connect your GitHub repo
- [ ] **A.2** In Render Dashboard → **New → Blueprint Instance**
- [ ] **A.3** Select the `Vecotr` repository — Render reads `render.yaml` automatically
- [ ] **A.4** Review the services list and click **Apply**
- [ ] **A.5** Wait ~5 minutes for all services to provision
- [ ] **A.6** After provisioning, fill in the `sync: false` secrets in each backend's **Environment** tab:
  - `SendGrid__ApiKey`, `SendGrid__FromEmail`
  - `Storage__ServiceUrl`, `Storage__BucketName`, `Storage__AccessKeyId`, `Storage__SecretAccessKey`, `Storage__PublicUrl`
- [ ] **A.7** Copy deploy hook URLs → each service **Settings → Deploy Hook** → add as GitHub Secrets (see below)

> **Note on service names:** The blueprint uses `vector-backend-qa`, `vector-backend-prod`, etc. If those names are taken on Render (by another user), rename them in `render.yaml` and update the matching `VITE_API_URL` env vars and `Frontend__Url` env vars accordingly.

> **Note on free-tier QA:** QA backend and Redis use `free` plan — they spin down after 15 min of inactivity. Upgrade to `starter` if you need always-on QA.

### GitHub Secrets for CI/CD Deploy Hooks

After services are created, add these 4 secrets so pushes auto-deploy:

| GitHub Secret | Where to get it |
|---------------|-----------------|
| `RENDER_DEPLOY_HOOK_BACKEND_QA` | Render → vector-backend-qa → Settings → Deploy Hook → copy URL |
| `RENDER_DEPLOY_HOOK_BACKEND_PROD` | Render → vector-backend-prod → Settings → Deploy Hook → copy URL |
| `RENDER_DEPLOY_HOOK_FRONTEND_QA` | Render → vector-frontend-qa → Settings → Deploy Hook → copy URL |
| `RENDER_DEPLOY_HOOK_FRONTEND_PROD` | Render → vector-frontend-prod → Settings → Deploy Hook → copy URL |

**Add at:** Repo → Settings → Secrets and variables → Actions → New repository secret

`QA_API_URL` is already set for frontend CI build validation.

---

## Architecture After Migration

```
GitHub (push to qa or main branch)
         │
         ▼
GitHub Actions CI/CD
         │
    ┌────┴─────┐
    ▼          ▼
Render        Render
Backend       Frontend
Web Service   Static Site
(Docker)      (Static)
    │          │
    ▼          │
Render         │
PostgreSQL ◄───┤
    │          │
Render         │
Redis      ◄───┘
    │
Cloudflare R2
(file uploads — S3-compatible, free tier)
```

### Services per Environment

| Service | QA/Staging | Production |
|---|---|---|
| Backend (.NET API) | Render Web Service (Docker) | Render Web Service (Docker) |
| Frontend (React) | Render Static Site | Render Static Site |
| PostgreSQL | Render Managed PostgreSQL | Render Managed PostgreSQL |
| Redis | Render Managed Redis | Render Managed Redis |
| File Uploads | Cloudflare R2 | Cloudflare R2 |
| Container Registry | Render builds from GitHub (no ECR needed) | Render builds from GitHub (no ECR needed) |

---

## Phase 1: Render Account & Project Setup

- [ ] **1.1** Create a [Render account](https://render.com) (free to sign up)
- [ ] **1.2** Connect your GitHub repository to Render
  - Dashboard → New → Connect GitHub
  - Grant access to the `Vecotr` repository
- [ ] **1.3** Create a **Render Team** (optional but recommended for multi-environment management)
- [ ] **1.4** Note your Render account owner ID — needed for API calls in CI/CD

---

## Phase 2: QA/Staging Environment — Databases

### 2.1 PostgreSQL (QA)

- [ ] **2.1.1** In Render Dashboard → New → PostgreSQL
- [ ] **2.1.2** Configure:
  - **Name:** `vector-postgres-qa`
  - **Region:** Oregon (US West) — or closest to your users
  - **Plan:** Starter ($7/mo, 1GB RAM, 256MB storage)
  - **PostgreSQL version:** 15
  - **Database name:** `vector_db`
  - **User:** `postgres`
- [ ] **2.1.3** Click **Create Database** and wait for provisioning (~2 min)
- [ ] **2.1.4** Copy the **Internal Connection String** — you'll use this in the backend env vars
  - Format: `postgresql://postgres:<password>@<host>/vector_db`

### 2.2 Redis (QA)

- [ ] **2.2.1** In Render Dashboard → New → Redis
- [ ] **2.2.2** Configure:
  - **Name:** `vector-redis-qa`
  - **Region:** Same region as PostgreSQL (Oregon)
  - **Plan:** Starter ($10/mo)
  - **Max memory policy:** `allkeys-lru` (matches current ElastiCache config)
- [ ] **2.2.3** Click **Create Redis** and wait for provisioning
- [ ] **2.2.4** Copy the **Internal Redis URL** — format: `redis://red-<id>:6379`

---

## Phase 3: QA/Staging Environment — Backend Service

- [ ] **3.1** In Render Dashboard → New → Web Service
- [ ] **3.2** Select **Deploy from GitHub repo** → select `Vecotr`
- [ ] **3.3** Configure:
  - **Name:** `vector-backend-qa`
  - **Region:** Oregon (same as databases)
  - **Branch:** `develop` ← auto-deploys when CI/CD pushes here
  - **Root Directory:** *(leave blank — Docker context is repo root)*
  - **Environment:** `Docker`
  - **Dockerfile path:** `./docker/Dockerfile.backend`
  - **Plan:** Starter ($7/mo, 512MB RAM, shared CPU)
- [ ] **3.4** Set **Environment Variables** (click "Add Environment Variable" for each):

  | Key | Value | Notes |
  |---|---|---|
  | `ASPNETCORE_ENVIRONMENT` | `Staging` | |
  | `ASPNETCORE_URLS` | `http://+:80` | Render routes to port 80 by default |
  | `ConnectionStrings__DefaultConnection` | *(Internal connection string from step 2.1.4)* | |
  | `ConnectionStrings__Redis` | *(Internal Redis URL from step 2.2.4)* | Strip `redis://` prefix if needed |
  | `Jwt__Secret` | *(generate a 64-char random string)* | Keep secret |
  | `Jwt__Issuer` | `Vector` | |
  | `Jwt__Audience` | `Vector` | |
  | `Frontend__Url` | `https://vector-frontend-qa.onrender.com` | Update after frontend created |
  | `Development__AutoVerifyEmails` | `false` | |
  | `SendGrid__ApiKey` | *(your SendGrid key)* | |
  | `SendGrid__FromEmail` | *(your verified sender email)* | |
  | `SendGrid__FromName` | `Vector` | |
  | `AWS__Region` | `us-east-1` | For S3 |
  | `AWS__S3__BucketName` | *(your existing S3 bucket name)* | Keep existing bucket |
  | `AWS_ACCESS_KEY_ID` | *(your AWS key)* | For S3 only |
  | `AWS_SECRET_ACCESS_KEY` | *(your AWS secret)* | For S3 only |
  | `Judge0__BaseUrl` | `https://ce.judge0.com` | |
  | `Judge0__ApiKey` | *(your Judge0 key if any)* | |

- [ ] **3.5** Under **Health & Alerts**:
  - **Health Check Path:** `/api/health`
- [ ] **3.6** Click **Create Web Service**
- [ ] **3.7** Wait for the first deploy to complete (5–10 min for .NET build)
- [ ] **3.8** Copy the service URL: `https://vector-backend-qa.onrender.com`
- [ ] **3.9** Copy the **Deploy Hook URL** from Settings → Deploy Hook — needed for CI/CD
  - Format: `https://api.render.com/deploy/srv-<id>?key=<key>`

---

## Phase 4: QA/Staging Environment — Frontend Service

- [ ] **4.1** In Render Dashboard → New → **Static Site**
  - *Note: We use Static Site instead of Docker for the frontend because Render builds static sites natively and they are **free**. We keep the Docker image for local dev only.*
  - *Alternative: If you need the Docker nginx container for specific nginx configs, use Web Service with Docker instead at $7/mo.*
- [ ] **4.2** Select the `Vecotr` GitHub repository
- [ ] **4.3** Configure:
  - **Name:** `vector-frontend-qa`
  - **Branch:** `develop`
  - **Root directory:** `frontend`
  - **Build command:** `npm ci && npm run build`
  - **Publish directory:** `dist`
- [ ] **4.4** Set **Environment Variables**:

  | Key | Value |
  |---|---|
  | `VITE_API_URL` | `https://vector-backend-qa.onrender.com/api` |

- [ ] **4.5** Click **Create Static Site**
- [ ] **4.6** Wait for first deploy (~3 min)
- [ ] **4.7** Copy the frontend URL: `https://vector-frontend-qa.onrender.com`
- [ ] **4.8** Go back to the **backend service** → Environment → update `Frontend__Url` to match this URL
- [ ] **4.9** Trigger a backend redeploy so the CORS config picks up the new URL

---

## Phase 5: Production Environment — Databases

### 5.1 PostgreSQL (Production)

- [ ] **5.1.1** In Render Dashboard → New → PostgreSQL
- [ ] **5.1.2** Configure:
  - **Name:** `vector-postgres-prod`
  - **Region:** Oregon
  - **Plan:** Starter ($7/mo) — upgrade to Standard ($20/mo) when traffic grows
  - **PostgreSQL version:** 15
  - **Database name:** `vector_db`
  - **User:** `postgres`
- [ ] **5.1.3** Click **Create Database**, wait for provisioning
- [ ] **5.1.4** Copy the **Internal Connection String**

### 5.2 Redis (Production)

- [ ] **5.2.1** In Render Dashboard → New → Redis
- [ ] **5.2.2** Configure:
  - **Name:** `vector-redis-prod`
  - **Region:** Oregon
  - **Plan:** Starter ($10/mo) — upgrade to Standard ($30/mo) when needed
  - **Max memory policy:** `allkeys-lru`
- [ ] **5.2.3** Click **Create Redis**, wait for provisioning
- [ ] **5.2.4** Copy the **Internal Redis URL**

---

## Phase 6: Production Environment — Backend & Frontend Services

### 6.1 Backend (Production)

- [ ] **6.1.1** New → Web Service → select `Vecotr` repo
- [ ] **6.1.2** Configure:
  - **Name:** `vector-backend-prod`
  - **Region:** Oregon
  - **Branch:** `main` ← auto-deploys when CI/CD pushes here
  - **Environment:** Docker
  - **Dockerfile path:** `./docker/Dockerfile.backend`
  - **Plan:** Starter ($7/mo) — upgrade to Standard ($25/mo) for always-on (no sleep)
- [ ] **6.1.3** Set all same environment variables as QA, but with:
  - `ASPNETCORE_ENVIRONMENT` = `Production`
  - `ConnectionStrings__DefaultConnection` = *(prod DB connection string)*
  - `ConnectionStrings__Redis` = *(prod Redis URL)*
  - `Frontend__Url` = `https://vector-frontend-prod.onrender.com` *(update after step 6.2)*
  - `Development__AutoVerifyEmails` = `false`
  - Use a **different** `Jwt__Secret` than QA
- [ ] **6.1.4** Health Check Path: `/api/health`
- [ ] **6.1.5** Click **Create Web Service**
- [ ] **6.1.6** Copy the **Deploy Hook URL** from Settings → Deploy Hook

### 6.2 Frontend (Production)

- [ ] **6.2.1** New → Static Site → select `Vecotr` repo
- [ ] **6.2.2** Configure:
  - **Name:** `vector-frontend-prod`
  - **Branch:** `main`
  - **Root directory:** `frontend`
  - **Build command:** `npm ci && npm run build`
  - **Publish directory:** `dist`
- [ ] **6.2.3** Set Environment Variables:

  | Key | Value |
  |---|---|
  | `VITE_API_URL` | `https://vector-backend-prod.onrender.com/api` |

- [ ] **6.2.4** Click **Create Static Site**
- [ ] **6.2.5** Update backend prod `Frontend__Url` env var with the actual frontend URL
- [ ] **6.2.6** Copy the **Deploy Hook URL** from Settings → Deploy Hook

---

## Phase 7: GitHub Actions Secrets Setup

Add the following secrets in GitHub → Settings → Secrets and variables → Actions:

- [ ] **7.1** `RENDER_DEPLOY_HOOK_BACKEND_QA` — Deploy hook URL from step 3.9
- [ ] **7.2** `RENDER_DEPLOY_HOOK_FRONTEND_QA` — Deploy hook URL from step 4.6 settings
- [ ] **7.3** `RENDER_DEPLOY_HOOK_BACKEND_PROD` — Deploy hook URL from step 6.1.6
- [ ] **7.4** `RENDER_DEPLOY_HOOK_FRONTEND_PROD` — Deploy hook URL from step 6.2.6
- [ ] **7.5** `QA_API_URL` — `https://vector-backend-qa.onrender.com/api`
- [ ] **7.6** `PROD_API_URL` — `https://vector-backend-prod.onrender.com/api`
- [ ] **7.7** Keep existing `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` (still needed for S3)
- [ ] **7.8** Remove old secrets that are no longer needed (optional cleanup):
  - `DEV_API_URL` (replaced by `QA_API_URL`)

---

## Phase 8: Update GitHub Actions Workflows ✅ COMPLETED

> Both workflow files have already been updated. No action needed.

### 8.1 Update `backend.yml`

- [x] **8.1.1** Replace the entire `deploy-dev` job with a Render deploy trigger for QA:

  ```yaml
  deploy-qa:
    name: Deploy to QA
    runs-on: ubuntu-latest
    needs: [build-and-test]
    if: github.ref == 'refs/heads/develop' && github.event_name == 'push'
    environment:
      name: qa
      url: https://vector-backend-qa.onrender.com

    steps:
      - name: Trigger Render deploy (QA backend)
        run: |
          curl -X POST "${{ secrets.RENDER_DEPLOY_HOOK_BACKEND_QA }}" \
            --fail \
            --silent \
            --show-error
          echo "QA backend deploy triggered on Render"
  ```

- [x] **8.1.2** Replace the `deploy-production` job with:

  ```yaml
  deploy-production:
    name: Deploy to Production
    runs-on: ubuntu-latest
    needs: [build-and-test]
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'
    environment:
      name: production
      url: https://vector-backend-prod.onrender.com

    steps:
      - name: Trigger Render deploy (Production backend)
        run: |
          curl -X POST "${{ secrets.RENDER_DEPLOY_HOOK_BACKEND_PROD }}" \
            --fail \
            --silent \
            --show-error
          echo "Production backend deploy triggered on Render"
  ```

- [x] **8.1.3** Remove the `build-docker-image` job — Render builds Docker images internally from GitHub source
- [x] **8.1.4** Remove the `deploy-staging` job (was already disabled, now replaced by `deploy-qa`)
- [x] **8.1.5** Remove all `Configure AWS credentials`, `Login to Amazon ECR`, and `Update ECS service` steps

### 8.2 Update `frontend.yml`

- [x] **8.2.1** Replace the `deploy-dev` job with:

  ```yaml
  deploy-qa:
    name: Deploy to QA
    runs-on: ubuntu-latest
    needs: [build-and-test]
    if: github.ref == 'refs/heads/develop' && github.event_name == 'push'
    environment:
      name: qa
      url: https://vector-frontend-qa.onrender.com

    steps:
      - name: Trigger Render deploy (QA frontend)
        run: |
          curl -X POST "${{ secrets.RENDER_DEPLOY_HOOK_FRONTEND_QA }}" \
            --fail \
            --silent \
            --show-error
          echo "QA frontend deploy triggered on Render"
  ```

- [x] **8.2.2** Replace the `deploy-production` job with:

  ```yaml
  deploy-production:
    name: Deploy to Production
    runs-on: ubuntu-latest
    needs: [build-and-test]
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'
    environment:
      name: production
      url: https://vector-frontend-prod.onrender.com

    steps:
      - name: Trigger Render deploy (Production frontend)
        run: |
          curl -X POST "${{ secrets.RENDER_DEPLOY_HOOK_FRONTEND_PROD }}" \
            --fail \
            --silent \
            --show-error
          echo "Production frontend deploy triggered on Render"
  ```

- [x] **8.2.3** Update the `build-and-test` job's `VITE_API_URL` env var reference:
  - Change `secrets.DEV_API_URL` → `secrets.QA_API_URL`
- [x] **8.2.4** Remove the `build-docker-image` job
- [x] **8.2.5** Remove all `Configure AWS credentials`, `Login to Amazon ECR`, and `Update ECS service` steps

---

## Phase 9: Validate Deployments

### 9.1 QA Environment Validation

- [ ] **9.1.1** Push a small change to `develop` branch and confirm CI/CD triggers
- [ ] **9.1.2** Watch GitHub Actions — confirm `deploy-qa` jobs succeed for both backend and frontend
- [ ] **9.1.3** Watch Render Dashboard — confirm both services show "Deploy succeeded"
- [ ] **9.1.4** Test the backend health endpoint: `GET https://vector-backend-qa.onrender.com/api/health`
- [ ] **9.1.5** Open the frontend: `https://vector-frontend-qa.onrender.com`
- [ ] **9.1.6** Verify login works end-to-end (auth → JWT → API calls)
- [ ] **9.1.7** Verify database migrations ran automatically on container startup
- [ ] **9.1.8** Verify Redis caching works (check token refresh and session behavior)
- [ ] **9.1.9** Verify S3 file uploads still work (upload a profile picture or attachment)
- [ ] **9.1.10** Run a quick smoke test: browse questions, solve one, check dashboard

### 9.2 Production Environment Validation

- [ ] **9.2.1** Push to `main` branch (or merge a PR) and confirm CI/CD triggers
- [ ] **9.2.2** Confirm `deploy-production` jobs succeed in GitHub Actions
- [ ] **9.2.3** Confirm Render shows "Deploy succeeded" for prod services
- [ ] **9.2.4** Test `GET https://vector-backend-prod.onrender.com/api/health`
- [ ] **9.2.5** Full smoke test on production URL
- [ ] **9.2.6** Verify migrations ran on prod database

---

## Phase 10: Custom Domain Setup (Optional)

- [ ] **10.1** Purchase or identify your domain (e.g., `vector.app`)
- [ ] **10.2** In Render → `vector-frontend-prod` → Settings → Custom Domain
  - Add `vector.app` and `www.vector.app`
  - Render auto-provisions a free Let's Encrypt SSL certificate
- [ ] **10.3** In Render → `vector-backend-prod` → Settings → Custom Domain
  - Add `api.vector.app`
- [ ] **10.4** In your DNS provider, add the CNAME records Render provides
- [ ] **10.5** Update backend prod env var: `Frontend__Url` = `https://vector.app`
- [ ] **10.6** Update frontend prod env var: `VITE_API_URL` = `https://api.vector.app/api`
- [ ] **10.7** Trigger redeploys for both services after env var updates
- [ ] **10.8** Repeat steps 10.2–10.7 for QA with a subdomain (e.g., `qa.vector.app`, `api-qa.vector.app`)

---

## Phase 11: Tear Down AWS Resources (After Validation)

> ⚠️ Only do this after QA and Production are confirmed working on Render.

- [ ] **11.1** Run `terraform destroy` in `infrastructure/terraform/` to remove:
  - ECS Fargate services and cluster
  - Application Load Balancer
  - NAT Gateway (biggest cost item — $35/mo)
  - ElastiCache Redis cluster
  - RDS PostgreSQL instance
  - Bastion Host EC2 instance
  - ECR repositories (after confirming no rollback needed)
  - VPC and all networking resources
- [ ] **11.2** Keep AWS S3 bucket — still used for file uploads
- [ ] **11.3** Keep AWS IAM user for S3 access only
- [ ] **11.4** Confirm AWS billing drops to ~$1–5/mo (S3 only) in the following billing cycle
- [ ] **11.5** Archive or delete the Terraform modules that are no longer needed:
  - `modules/ecs/`
  - `modules/ecr/`
  - `modules/alb/`
  - `modules/rds/`
  - `modules/redis/`
  - `modules/bastion/`
  - `modules/vpc/`

---

## Final Cost Comparison

| Service | AWS (Before) | Render (After) | Savings |
|---|---|---|---|
| Backend compute (2 envs) | ~$18/mo (2× ECS Fargate) | ~$14/mo (2× Starter Web Service) | $4/mo |
| Frontend hosting (2 envs) | ~$18/mo (2× ECS Fargate) | **$0** (Static Sites are free) | $18/mo |
| PostgreSQL (2 envs) | ~$15/mo (1× RDS t3.micro) | ~$14/mo (2× Starter PG) | $1/mo |
| Redis (2 envs) | ~$12/mo (1× cache.t3.micro) | ~$20/mo (2× Starter Redis) | -$8/mo |
| Load Balancer | ~$18/mo (ALB) | **$0** (built into Render) | $18/mo |
| NAT Gateway | **~$35/mo** | **$0** | $35/mo |
| Bastion Host | ~$8/mo (EC2 t3.micro) | **$0** | $8/mo |
| Container Registry | ~$1/mo (ECR) | **$0** (Render builds from GitHub) | $1/mo |
| **Total** | **~$115/mo** | **~$48/mo** | **~$67/mo saved** |

> **Note:** The $48/mo can be further reduced by upgrading Redis to Upstash (pay-per-use, free tier covers low traffic) saving another ~$20/mo, bringing the total to ~$28/mo.

---

## Key Notes for This Migration

### What Changes
- CI/CD deploy steps: replace ECR push + ECS update with a single `curl` to Render deploy hook
- Container registry: Render builds Docker images internally from GitHub — no ECR needed
- Networking: no VPC, no NAT, no ALB — Render handles all routing and TLS
- Database: Render managed PostgreSQL replaces RDS — connection string format is identical for .NET

### What Does NOT Change
- `Dockerfile.backend` and `Dockerfile.frontend` — used as-is
- `docker/nginx.conf` — used as-is
- Application code — zero changes required
- AWS S3 integration — keep the existing bucket and IAM credentials
- GitHub Actions structure (build → test → deploy) — only deploy steps change
- All environment variable names in the app — only the values change

### Render Deploy Hook Flow
```
GitHub push to 'develop'
         │
         ▼
GitHub Actions: build + test (same as today)
         │ (on success)
         ▼
curl POST → Render Deploy Hook URL
         │
         ▼
Render: pulls latest commit → builds Docker image → deploys → health check
```

### Database Migration Notes
- EF Core migrations run automatically on app startup (already configured in `Program.cs`)
- No manual migration step needed — just deploy and the container applies migrations
- For production, consider adding a manual approval gate in GitHub Actions before the deploy step
