# Vector Deployment Guide

This guide covers deployment procedures for the Vector platform across different environments.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Local Development Setup](#local-development-setup)
3. [Docker Deployment](#docker-deployment)
4. [AWS Deployment](#aws-deployment)
5. [Environment-Specific Configuration](#environment-specific-configuration)
6. [Database Migrations](#database-migrations)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software
- Docker Desktop (for local Docker deployment)
- .NET 8.0 SDK (for local development)
- Node.js 20+ LTS (for frontend development)
- PostgreSQL 15+ (for local database)
- Redis 7+ (for local cache)
- AWS CLI (for AWS deployment)
- Terraform (for infrastructure as code)

### Required Accounts
- AWS Account with appropriate permissions
- SendGrid account (for email services)
- Stripe account (for payment processing)

---

## Local Development Setup

### 1. Clone Repository

```bash
git clone https://github.com/your-org/vector.git
cd vector
```

### 2. Backend Setup

```bash
cd backend/Vector.Api

# Restore dependencies
dotnet restore

# Update connection string in appsettings.json
# Update JWT secret in appsettings.json

# Run migrations
dotnet ef database update

# Run the application
dotnet run
```

Backend will be available at `http://localhost:5000`

### 3. Frontend Setup

```bash
cd frontend

# Install dependencies
npm install

# Create .env file
echo "VITE_API_URL=http://localhost:5000/api" > .env

# Run development server
npm run dev
```

Frontend will be available at `http://localhost:3000`

---

## Docker Deployment

### Prerequisites
- Docker Desktop installed and running
- Docker Compose v2+

### Quick Start

```bash
cd docker

# Build and start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### Services

The Docker Compose setup includes:
- **Backend** (`vector-backend`) - Port 5000
- **Frontend** (`vector-frontend`) - Port 3000
- **PostgreSQL** (`vector-postgres`) - Port 5432
- **Redis** (`vector-redis`) - Port 6379

### Building Images

```bash
# Build backend without cache
docker-compose build --no-cache backend

# Build frontend without cache
docker-compose build --no-cache frontend

# Build all services
docker-compose build --no-cache
```

### Environment Variables

Create a `.env` file in the `docker` directory:

```env
# Database
POSTGRES_DB=vector_db
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_secure_password

# Redis
REDIS_PASSWORD=

# Backend
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:80
JWT_SECRET=your-super-secret-key-minimum-32-characters-long
JWT_ISSUER=Vector
JWT_AUDIENCE=Vector

# Frontend
VITE_API_URL=http://localhost:5000/api

# AWS (for S3)
AWS_ACCESS_KEY_ID=your_access_key
AWS_SECRET_ACCESS_KEY=your_secret_key
AWS_REGION=us-east-1
AWS_S3_BUCKET=vector-user-uploads

# SendGrid
SENDGRID_API_KEY=your_sendgrid_api_key
SENDGRID_FROM_EMAIL=noreply@vector.com
SENDGRID_FROM_NAME=Vector

# Stripe (optional)
STRIPE_SECRET_KEY=sk_test_...
STRIPE_PUBLISHABLE_KEY=pk_test_...
```

### Database Migrations

Migrations run automatically on backend container startup. To run manually:

```bash
# Enter backend container
docker-compose exec backend bash

# Run migrations
dotnet ef database update
```

### Troubleshooting Docker

```bash
# Check container status
docker-compose ps

# View backend logs
docker-compose logs backend

# View frontend logs
docker-compose logs frontend

# Restart a service
docker-compose restart backend

# Rebuild and restart
docker-compose up -d --build backend
```

---

## AWS Deployment

### Prerequisites
- AWS CLI configured with appropriate credentials
- Terraform installed
- GitHub repository with CI/CD configured

### Infrastructure Setup

```bash
cd infrastructure/terraform

# Initialize Terraform
terraform init

# Review changes
terraform plan

# Apply infrastructure
terraform apply
```

### CI/CD Deployment

The project uses GitHub Actions for automated deployment:

1. **Push to `develop` branch** → Deploys to Development environment
2. **Push to `staging` branch** → Deploys to Staging environment
3. **Push to `main` branch** → Deploys to Production environment

### Manual Deployment Steps

#### 1. Build Backend

```bash
cd backend/Vector.Api

# Build for production
dotnet publish -c Release -o ./publish

# Create Docker image
docker build -t vector-backend:latest .
```

#### 2. Build Frontend

```bash
cd frontend

# Build for production
npm run build

# Create Docker image
docker build -t vector-frontend:latest .
```

#### 3. Push to Container Registry

```bash
# Tag images
docker tag vector-backend:latest <registry>/vector-backend:latest
docker tag vector-frontend:latest <registry>/vector-frontend:latest

# Push images
docker push <registry>/vector-backend:latest
docker push <registry>/vector-frontend:latest
```

#### 4. Update ECS Service

```bash
# Update ECS task definition
aws ecs update-service \
  --cluster vector-cluster \
  --service vector-backend \
  --force-new-deployment

aws ecs update-service \
  --cluster vector-cluster \
  --service vector-frontend \
  --force-new-deployment
```

---

## Environment-Specific Configuration

### Development

- **Database:** Local PostgreSQL or Docker container
- **Redis:** Local Redis or Docker container
- **S3:** Local MinIO or AWS S3 (dev bucket)
- **Email:** SendGrid (test mode)
- **Logging:** Console logging with detailed errors

### Staging

- **Database:** AWS RDS PostgreSQL
- **Redis:** AWS ElastiCache Redis
- **S3:** AWS S3 (staging bucket)
- **Email:** SendGrid (production API key)
- **Logging:** CloudWatch Logs

### Production

- **Database:** AWS RDS PostgreSQL (Multi-AZ)
- **Redis:** AWS ElastiCache Redis (Cluster mode)
- **S3:** AWS S3 (production bucket with versioning)
- **Email:** SendGrid (production API key)
- **Logging:** CloudWatch Logs with retention
- **Monitoring:** CloudWatch Metrics, X-Ray tracing

---

## Database Migrations

### Creating Migrations

```bash
cd backend/Vector.Api

# Create a new migration
dotnet ef migrations add MigrationName --output-dir Data/Migrations

# Review migration SQL
dotnet ef migrations script
```

### Applying Migrations

#### Local Development

```bash
# Apply all pending migrations
dotnet ef database update
```

#### Docker

Migrations run automatically on container startup via `Program.cs`.

#### AWS (ECS)

Migrations run automatically on container startup. Ensure:
- Database connection string is correct
- ECS task has database access permissions
- Migration user has appropriate privileges

### Rollback

```bash
# Rollback to previous migration
dotnet ef database update PreviousMigrationName

# Rollback all migrations
dotnet ef database update 0
```

---

## Health Checks

### Backend Health Check

```bash
# Basic health check
curl http://localhost:5000/api/health

# Detailed health check
curl http://localhost:5000/api/health/detailed
```

### Frontend Health Check

```bash
curl http://localhost:3000
```

### Docker Health Checks

```bash
# Check all services
docker-compose ps

# Check specific service
docker inspect vector-backend | grep -A 10 Health
```

---

## Monitoring

### Application Logs

#### Local Development
- Backend: Console output
- Frontend: Browser console

#### Docker
```bash
# View all logs
docker-compose logs -f

# View specific service
docker-compose logs -f backend
```

#### AWS
- CloudWatch Logs: `/aws/ecs/vector-backend`
- CloudWatch Logs: `/aws/ecs/vector-frontend`

### Metrics

Key metrics to monitor:
- API response times
- Error rates
- Database connection pool usage
- Redis cache hit rates
- S3 upload/download rates
- Email delivery rates

---

## Security Checklist

Before deploying to production:

- [ ] All secrets are in environment variables (not in code)
- [ ] JWT secret is strong and unique
- [ ] Database passwords are strong
- [ ] CORS is configured correctly
- [ ] HTTPS is enabled
- [ ] Rate limiting is configured
- [ ] Input validation is enabled
- [ ] SQL injection protection (EF Core parameterization)
- [ ] XSS protection (input sanitization)
- [ ] Security headers are configured
- [ ] Logging does not expose sensitive data

---

## Troubleshooting

### Backend Won't Start

1. Check database connection:
   ```bash
   docker-compose exec postgres psql -U postgres -d vector_db -c "SELECT 1;"
   ```

2. Check Redis connection:
   ```bash
   docker-compose exec redis redis-cli ping
   ```

3. Check logs:
   ```bash
   docker-compose logs backend
   ```

### Frontend Won't Build

1. Clear node_modules:
   ```bash
   rm -rf node_modules package-lock.json
   npm install
   ```

2. Check for TypeScript errors:
   ```bash
   npm run build
   ```

### Database Migration Fails

1. Check connection string
2. Verify database exists
3. Check user permissions
4. Review migration SQL manually

### Docker Build Fails

1. Clear Docker cache:
   ```bash
   docker system prune -a
   ```

2. Rebuild without cache:
   ```bash
   docker-compose build --no-cache
   ```

---

## Rollback Procedure

### Application Rollback

```bash
# Revert to previous Docker image
docker-compose pull
docker-compose up -d

# Or specify version
docker-compose up -d --image vector-backend:previous-version
```

### Database Rollback

```bash
# Rollback to previous migration
dotnet ef database update PreviousMigrationName
```

---

## Support

For deployment issues:
1. Check logs: `docker-compose logs`
2. Review environment variables
3. Verify network connectivity
4. Check AWS service status
5. Review GitHub Actions workflow logs

---

**Last Updated:** December 6, 2025

