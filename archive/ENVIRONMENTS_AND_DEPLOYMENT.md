# Environments and Deployment Procedures

This document describes the Vector platform's environment structure, deployment procedures, and promotion workflow.

**Last Updated:** November 29, 2024

---

## Environment Overview

The Vector platform uses a three-environment strategy for safe, controlled deployments:

```
Local Development → Dev → Staging → Production
```

### Environment Summary

| Environment | Purpose | Instance Size | Access | Auto-Deploy |
|-------------|---------|---------------|--------|-------------|
| **Local** | Developer machines | Docker containers | Developers | Manual |
| **Dev** | Active development & testing | db.t3.micro, cache.t3.micro | Developers | On push to `develop` |
| **Staging** | Pre-production testing | db.t3.small, cache.t3.small | QA, Stakeholders | On merge to `staging` |
| **Production** | Live application | db.t3.medium+, cache.t3.medium+ | Restricted | On merge to `main` |

---

## Environment Details

### 1. Local Development Environment

**Purpose:** Individual developer workstations

**Components:**
- Docker Compose (PostgreSQL, Redis, Backend, Frontend)
- Local file system
- Development tools (IDE, debuggers)

**Configuration:**
- **Database:** PostgreSQL 15 (Docker container)
- **Cache:** Redis 7 (Docker container)
- **Connection:** `localhost:5432`, `localhost:6379`
- **Auto-migrations:** Enabled (runs on app startup)

**Setup:**
```powershell
cd docker
docker-compose up -d
```

**Database Migrations:**
- Automatic on application startup (Development mode)
- Manual: `dotnet ef database update` in `backend/Vector.Api`

---

### 2. Dev Environment (AWS)

**Purpose:** Shared development environment for team collaboration

**Status:** ✅ **Deployed**

**Infrastructure:**
- **VPC:** `10.0.0.0/16` with public/private subnets
- **RDS PostgreSQL:** `db.t3.micro` (Free Tier eligible)
- **ElastiCache Redis:** `cache.t3.micro` (Free Tier eligible)
- **S3 Bucket:** `dev-vector-user-uploads`
- **Region:** `us-east-1`

**Deployment:**
- **Trigger:** Push to `develop` branch
- **Method:** GitHub Actions CI/CD
- **Database Migrations:** Automatic on deployment

**Access:**
- **Database Endpoint:** Get from `terraform output database_endpoint`
- **Connection:** Via VPC or bastion host (if configured)
- **Credentials:** Stored in GitHub Secrets

**Terraform Configuration:**
```powershell
cd infrastructure/terraform
terraform apply -var="environment=dev" -var="db_password=..."
```

**Cost Estimate:** ~$92/month
- RDS: ~$15/month
- ElastiCache: ~$12/month
- NAT Gateways: ~$64/month (main cost)
- S3: ~$1/month

---

### 3. Staging Environment (AWS)

**Purpose:** Pre-production testing and stakeholder demos

**Status:** ⏳ **Not Yet Deployed**

**Infrastructure:**
- **VPC:** Separate VPC for staging
- **RDS PostgreSQL:** `db.t3.small` (higher performance)
- **ElastiCache Redis:** `cache.t3.small`
- **S3 Bucket:** `staging-vector-user-uploads`
- **Region:** `us-east-1`

**Deployment:**
- **Trigger:** Merge to `staging` branch
- **Method:** GitHub Actions CI/CD
- **Database Migrations:** Manual or automated (configurable)

**Access:**
- **Database Endpoint:** Separate from dev
- **Connection:** Via VPC or bastion host
- **Credentials:** Stored in GitHub Secrets (separate from dev)

**Terraform Configuration:**
```powershell
cd infrastructure/terraform
terraform apply -var="environment=staging" -var="db_password=..."
```

**Cost Estimate:** ~$150/month
- Higher instance sizes for realistic testing

---

### 4. Production Environment (AWS)

**Purpose:** Live application serving end users

**Status:** ⏳ **Not Yet Deployed**

**Infrastructure:**
- **VPC:** Production-grade VPC with enhanced security
- **RDS PostgreSQL:** `db.t3.medium` or higher (with Multi-AZ)
- **ElastiCache Redis:** `cache.t3.medium` or higher (with replication)
- **S3 Bucket:** `prod-vector-user-uploads`
- **Region:** `us-east-1` (or multi-region for HA)
- **Backups:** Automated daily backups, 7-day retention
- **Monitoring:** CloudWatch alarms and logging

**Deployment:**
- **Trigger:** Merge to `main` branch
- **Method:** GitHub Actions CI/CD with approval gates
- **Database Migrations:** Manual only (for safety)

**Access:**
- **Database Endpoint:** Production endpoint
- **Connection:** Highly restricted (VPN or bastion only)
- **Credentials:** Stored in GitHub Secrets (separate from all environments)

**Terraform Configuration:**
```powershell
cd infrastructure/terraform
terraform apply -var="environment=prod" -var="db_password=..."
```

**Cost Estimate:** ~$300-500/month
- Higher instance sizes
- Multi-AZ for high availability
- Enhanced monitoring and backups

---

## Deployment Procedures

### Database Migration Deployment

**Important:** Database migrations are code and should be deployed with application code.

#### Development Environment

**Automatic (Current Setup):**
- Migrations run automatically on application startup
- Configured in `Program.cs`:
  ```csharp
  if (app.Environment.IsDevelopment())
  {
      db.Database.Migrate();
  }
  ```

**Manual (Alternative):**
```powershell
cd backend/Vector.Api
dotnet ef database update --connection "DevConnectionString"
```

#### Staging Environment

**Recommended: Manual or CI/CD Step**
```powershell
# In CI/CD pipeline or manually
dotnet ef database update --connection "$STAGING_DB_CONNECTION"
```

**Best Practice:** Test migrations on dev first, then apply to staging.

#### Production Environment

**Required: Manual or Approved CI/CD Step**
```powershell
# Only after thorough testing
dotnet ef database update --connection "$PROD_DB_CONNECTION"
```

**Safety Measures:**
1. Review migration files before applying
2. Test on staging first
3. Backup database before migration
4. Apply during low-traffic periods
5. Monitor application after migration

### Application Deployment

#### Backend API Deployment

**Process:**
1. Code committed to branch
2. CI/CD pipeline triggered
3. Build and test
4. Create Docker image
5. Push to container registry (if using)
6. Deploy to environment
7. Run database migrations (if configured)
8. Health check

**CI/CD Workflow:** `.github/workflows/backend.yml`

#### Frontend Deployment

**Process:**
1. Code committed to branch
2. CI/CD pipeline triggered
3. Build React application
4. Create Docker image (or deploy to S3/CloudFront)
5. Deploy to environment
6. Health check

**CI/CD Workflow:** `.github/workflows/frontend.yml`

---

## Promotion Workflow

### Code Promotion Path

```
┌─────────────────┐
│ Local Dev       │ → Developer commits code
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ develop branch  │ → Auto-deploy to Dev environment
└────────┬────────┘
         │
         ▼ (After testing)
┌─────────────────┐
│ staging branch  │ → Auto-deploy to Staging environment
└────────┬────────┘
         │
         ▼ (After QA approval)
┌─────────────────┐
│ main branch     │ → Manual/Approved deploy to Production
└─────────────────┘
```

### Database Migration Promotion

**Critical:** Migrations must be applied in the same order across all environments.

**Process:**
1. **Local:** Create and test migration
   ```powershell
   dotnet ef migrations add MigrationName
   dotnet ef database update
   ```

2. **Dev:** Apply after code deployment
   - Automatic (if configured) or manual
   - Verify migration success

3. **Staging:** Apply same migration
   - Use same migration files
   - Test thoroughly

4. **Production:** Apply same migration
   - Only after staging verification
   - During maintenance window (if possible)
   - Monitor closely

**Important Rules:**
- ✅ Never skip environments
- ✅ Always use the same migration files
- ✅ Test migrations on dev/staging first
- ✅ Never modify applied migrations
- ✅ Keep migration files in version control

---

## Environment Configuration

### Terraform Environment Variables

Each environment uses the same Terraform configuration with different variables:

**Dev:**
```hcl
environment = "dev"
db_instance_class = "db.t3.micro"
redis_node_type = "cache.t3.micro"
```

**Staging:**
```hcl
environment = "staging"
db_instance_class = "db.t3.small"
redis_node_type = "cache.t3.small"
```

**Production:**
```hcl
environment = "prod"
db_instance_class = "db.t3.medium"
redis_node_type = "cache.t3.medium"
```

### Application Configuration

**Connection Strings:**
- Stored in GitHub Secrets per environment
- Format: `Host=<endpoint>;Database=vector_db;Username=postgres;Password=<secret>`

**Environment Variables:**
- `ASPNETCORE_ENVIRONMENT`: `Development`, `Staging`, `Production`
- `ConnectionStrings__DefaultConnection`: Environment-specific
- `ConnectionStrings__Redis`: Environment-specific
- JWT secrets, Stripe keys, AWS credentials: All environment-specific

---

## Deployment Checklist

### Pre-Deployment

- [ ] Code reviewed and approved
- [ ] Tests passing locally
- [ ] Migration files reviewed (if applicable)
- [ ] Environment variables updated
- [ ] Database backup taken (staging/prod)

### Deployment

- [ ] CI/CD pipeline triggered
- [ ] Build successful
- [ ] Tests passing
- [ ] Docker image created
- [ ] Deployment to environment successful
- [ ] Database migrations applied (if applicable)
- [ ] Health checks passing

### Post-Deployment

- [ ] Application accessible
- [ ] Database connectivity verified
- [ ] API endpoints responding
- [ ] Frontend loading correctly
- [ ] Monitoring alerts configured
- [ ] Logs reviewed for errors

---

## Rollback Procedures

### Application Rollback

**If deployment fails:**
1. Revert to previous Docker image/version
2. Restore previous application code
3. Verify application functionality

### Database Migration Rollback

**If migration causes issues:**
1. **Option 1:** Create a new migration to reverse changes
   ```powershell
   dotnet ef migrations add RollbackMigrationName
   ```

2. **Option 2:** Restore database from backup
   - Restore to pre-migration state
   - Re-apply previous migrations

**Important:** Always test rollback procedures in dev/staging first.

---

## Monitoring and Alerts

### Dev Environment
- Basic CloudWatch monitoring
- Application logs
- Error tracking

### Staging Environment
- Enhanced monitoring
- Performance metrics
- Error tracking and alerts

### Production Environment
- Comprehensive monitoring
- Real-time alerts
- Performance dashboards
- Cost monitoring
- Security monitoring

---

## Security Considerations

### Environment Isolation
- Separate VPCs for each environment
- Separate database instances
- Separate credentials and secrets
- Network isolation

### Access Control
- **Dev:** Developer access
- **Staging:** QA and stakeholder access
- **Production:** Restricted access, approval required

### Secrets Management
- All secrets stored in GitHub Secrets
- Never commit secrets to code
- Rotate secrets regularly
- Use different secrets per environment

---

## Cost Optimization

### Dev Environment
- Use Free Tier eligible instances
- Consider NAT Instances instead of NAT Gateways (~$60/month savings)
- Auto-shutdown during non-working hours (future enhancement)

### Staging Environment
- Use smaller instances than production
- Can be stopped when not in use

### Production Environment
- Right-size instances based on actual usage
- Use Reserved Instances for long-term savings
- Monitor and optimize continuously

---

## Next Steps

1. ✅ **Dev Environment** - Deployed and operational
2. ⏳ **Staging Environment** - Deploy when ready for QA testing
3. ⏳ **Production Environment** - Deploy when application is production-ready
4. ⏳ **CI/CD Pipelines** - Set up automated deployments
5. ⏳ **Monitoring** - Configure comprehensive monitoring
6. ⏳ **Backup Strategy** - Implement automated backups

---

## Troubleshooting

### Common Issues

**Database Connection Failed:**
- Verify security group rules
- Check VPC configuration
- Verify credentials in GitHub Secrets

**Migration Failed:**
- Check database connectivity
- Verify migration files are correct
- Review migration history

**Deployment Failed:**
- Check CI/CD logs
- Verify environment variables
- Check Docker image build logs

---

**For detailed CI/CD pipeline information, see the workflow files in `.github/workflows/`**

