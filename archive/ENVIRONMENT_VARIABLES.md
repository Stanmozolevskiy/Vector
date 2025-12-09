# Environment Variables Documentation

This document describes all environment variables used in the Vector platform.

## Table of Contents

1. [Backend Environment Variables](#backend-environment-variables)
2. [Frontend Environment Variables](#frontend-environment-variables)
3. [Docker Environment Variables](#docker-environment-variables)
4. [AWS Environment Variables](#aws-environment-variables)
5. [Security Best Practices](#security-best-practices)

---

## Backend Environment Variables

### Database Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | - | Yes |
| `ConnectionStrings__Redis` | Redis connection string | `localhost:6379` | Yes |

**Example:**
```env
ConnectionStrings__DefaultConnection=Host=localhost;Database=vector_db;Username=postgres;Password=your_password
ConnectionStrings__Redis=localhost:6379
```

---

### JWT Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `Jwt__Secret` | JWT signing secret (min 32 characters) | - | Yes |
| `Jwt__Issuer` | JWT issuer claim | `Vector` | Yes |
| `Jwt__Audience` | JWT audience claim | `Vector` | Yes |
| `Jwt__AccessTokenExpirationMinutes` | Access token expiration (minutes) | `15` | No |
| `Jwt__RefreshTokenExpirationDays` | Refresh token expiration (days) | `7` | No |

**Example:**
```env
Jwt__Secret=your-super-secret-key-minimum-32-characters-long-change-in-production
Jwt__Issuer=Vector
Jwt__Audience=Vector
Jwt__AccessTokenExpirationMinutes=15
Jwt__RefreshTokenExpirationDays=7
```

**Security Note:** JWT secret must be at least 32 characters long and unique per environment.

---

### AWS Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `AWS__AccessKeyId` | AWS access key ID | - | Yes (for S3) |
| `AWS__SecretAccessKey` | AWS secret access key | - | Yes (for S3) |
| `AWS__Region` | AWS region | `us-east-1` | Yes |
| `AWS__S3__BucketName` | S3 bucket name for uploads | - | Yes |

**Example:**
```env
AWS__AccessKeyId=AKIAIOSFODNN7EXAMPLE
AWS__SecretAccessKey=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
AWS__Region=us-east-1
AWS__S3__BucketName=vector-user-uploads
```

---

### SendGrid Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `SendGrid__ApiKey` | SendGrid API key | - | Yes |
| `SendGrid__FromEmail` | Default sender email | - | Yes |
| `SendGrid__FromName` | Default sender name | `Vector` | No |

**Example:**
```env
SendGrid__ApiKey=SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
SendGrid__FromEmail=noreply@vector.com
SendGrid__FromName=Vector
```

---

### Stripe Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `Stripe__SecretKey` | Stripe secret key | - | No (for payment features) |
| `Stripe__PublishableKey` | Stripe publishable key | - | No (for payment features) |
| `Stripe__WebhookSecret` | Stripe webhook signing secret | - | No (for webhooks) |

**Example:**
```env
Stripe__SecretKey=sk_test_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
Stripe__PublishableKey=pk_test_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
Stripe__WebhookSecret=whsec_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

---

### Application Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Development` | No |
| `ASPNETCORE_URLS` | URLs to listen on | `http://localhost:5000` | No |
| `Frontend__Url` | Frontend URL for CORS | `http://localhost:3000` | Yes |

**Example:**
```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80
Frontend__Url=https://app.vector.com
```

---

### Logging Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `Logging__LogLevel__Default` | Default log level | `Information` | No |
| `Logging__LogLevel__Microsoft.AspNetCore` | ASP.NET Core log level | `Warning` | No |

**Example:**
```env
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft.AspNetCore=Warning
```

---

## Frontend Environment Variables

### API Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `VITE_API_URL` | Backend API base URL | - | Yes |

**Example:**
```env
VITE_API_URL=http://localhost:5000/api
```

**Note:** All frontend environment variables must be prefixed with `VITE_` to be accessible in the application.

---

## Docker Environment Variables

### PostgreSQL

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `POSTGRES_DB` | Database name | `vector_db` | No |
| `POSTGRES_USER` | Database user | `postgres` | No |
| `POSTGRES_PASSWORD` | Database password | - | Yes |

---

### Redis

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `REDIS_PASSWORD` | Redis password | - | No |

---

## AWS Environment Variables

### ECS Task Definition

These variables are set in the ECS task definition:

| Variable | Description | Source |
|----------|-------------|--------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | Task definition |
| `ConnectionStrings__DefaultConnection` | Database connection | AWS Secrets Manager |
| `ConnectionStrings__Redis` | Redis connection | AWS Secrets Manager |
| `Jwt__Secret` | JWT secret | AWS Secrets Manager |
| `AWS__AccessKeyId` | AWS access key | IAM role (preferred) or Secrets Manager |
| `AWS__SecretAccessKey` | AWS secret key | IAM role (preferred) or Secrets Manager |
| `SendGrid__ApiKey` | SendGrid API key | AWS Secrets Manager |
| `Stripe__SecretKey` | Stripe secret key | AWS Secrets Manager |

---

## Environment-Specific Values

### Development

```env
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=Host=localhost;Database=vector_db_dev;Username=postgres;Password=dev_password
ConnectionStrings__Redis=localhost:6379
Jwt__Secret=dev-secret-key-minimum-32-characters-long
Frontend__Url=http://localhost:3000
```

### Staging

```env
ASPNETCORE_ENVIRONMENT=Staging
ConnectionStrings__DefaultConnection=Host=staging-db.vector.com;Database=vector_db_staging;Username=vector_user;Password=<from-secrets>
ConnectionStrings__Redis=staging-redis.vector.com:6379
Jwt__Secret=<from-secrets>
Frontend__Url=https://staging.vector.com
```

### Production

```env
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=prod-db.vector.com;Database=vector_db;Username=vector_user;Password=<from-secrets>
ConnectionStrings__Redis=prod-redis.vector.com:6379
Jwt__Secret=<from-secrets>
Frontend__Url=https://app.vector.com
```

---

## Security Best Practices

### 1. Never Commit Secrets

- Use `.env` files (not committed to git)
- Use AWS Secrets Manager for production
- Use GitHub Secrets for CI/CD

### 2. Use Strong Secrets

- JWT secret: Minimum 32 characters, random
- Database passwords: Minimum 16 characters, mixed case, numbers, symbols
- API keys: Use service-provided keys

### 3. Rotate Secrets Regularly

- JWT secrets: Every 90 days
- Database passwords: Every 180 days
- API keys: As per service provider recommendations

### 4. Use Different Secrets Per Environment

- Development, Staging, and Production must have different secrets
- Never reuse production secrets in other environments

### 5. Limit Access

- Only grant access to secrets to authorized personnel
- Use IAM roles instead of access keys when possible
- Enable MFA for sensitive operations

---

## Setting Environment Variables

### Local Development

Create a `.env` file in the project root (not committed to git):

```env
ConnectionStrings__DefaultConnection=Host=localhost;Database=vector_db;Username=postgres;Password=password
Jwt__Secret=local-development-secret-key-minimum-32-characters
```

### Docker

Set in `docker-compose.yml` or `.env` file:

```yaml
services:
  backend:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=vector_db;Username=postgres;Password=password
```

### AWS ECS

Set in task definition or use AWS Secrets Manager:

```json
{
  "secrets": [
    {
      "name": "Jwt__Secret",
      "valueFrom": "arn:aws:secretsmanager:region:account:secret:vector/jwt-secret"
    }
  ]
}
```

### GitHub Actions

Set in repository secrets:

1. Go to Settings → Secrets and variables → Actions
2. Add new repository secret
3. Reference in workflow: `${{ secrets.SECRET_NAME }}`

---

## Validation

The application validates required environment variables on startup. Missing required variables will cause the application to fail with a clear error message.

---

## Troubleshooting

### Variable Not Loading

1. Check variable name (case-sensitive)
2. Verify prefix (`VITE_` for frontend)
3. Restart application after changes
4. Check for typos in variable names

### Secret Not Working

1. Verify secret is correct
2. Check for extra spaces or newlines
3. Ensure secret meets requirements (length, format)
4. Verify secret is set in correct environment

---

**Last Updated:** December 6, 2025

