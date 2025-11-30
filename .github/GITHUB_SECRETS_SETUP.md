# GitHub Secrets Configuration Guide

This document explains how to configure GitHub Secrets for CI/CD pipelines.

## Required Secrets

The CI/CD pipelines require the following secrets to be configured in GitHub:

### AWS Credentials

**Secret Name:** `AWS_ACCESS_KEY_ID`
- **Description:** AWS Access Key ID for programmatic access
- **How to get:** AWS IAM → Users → Your User → Security Credentials → Create Access Key
- **Required for:** All deployments

**Secret Name:** `AWS_SECRET_ACCESS_KEY`
- **Description:** AWS Secret Access Key
- **How to get:** Same as above (shown only once when created)
- **Required for:** All deployments

### Database Connection Strings

**Secret Name:** `DEV_DB_CONNECTION_STRING`
- **Description:** PostgreSQL connection string for Dev environment
- **Format:** `Host=<rds-endpoint>;Database=vector_db;Username=postgres;Password=<password>;Port=5432`
- **How to get:** 
  ```powershell
  cd infrastructure/terraform
  terraform output database_endpoint
  ```
- **Required for:** Dev deployments

**Secret Name:** `STAGING_DB_CONNECTION_STRING`
- **Description:** PostgreSQL connection string for Staging environment
- **Format:** Same as above
- **Required for:** Staging deployments

**Secret Name:** `PROD_DB_CONNECTION_STRING`
- **Description:** PostgreSQL connection string for Production environment
- **Format:** Same as above
- **Required for:** Production deployments

### Application Configuration (Optional for now)

**Secret Name:** `DEV_API_URL`
- **Description:** API URL for frontend builds (Dev)
- **Example:** `https://dev-api.vector.com`
- **Required for:** Frontend builds (Dev)

**Secret Name:** `STAGING_API_URL`
- **Description:** API URL for frontend builds (Staging)
- **Example:** `https://staging-api.vector.com`
- **Required for:** Frontend builds (Staging)

**Secret Name:** `PROD_API_URL`
- **Description:** API URL for frontend builds (Production)
- **Example:** `https://api.vector.com`
- **Required for:** Frontend builds (Production)

### Additional Secrets (Future)

These will be needed as features are implemented:

- `STRIPE_SECRET_KEY` - Stripe API secret key
- `STRIPE_WEBHOOK_SECRET` - Stripe webhook signing secret
- `SENDGRID_API_KEY` - SendGrid email API key
- `JWT_SECRET` - JWT token signing secret (per environment)

## How to Add Secrets to GitHub

### Step 1: Navigate to Repository Settings

1. Go to your GitHub repository: `https://github.com/Stanmozolevskiy/Vector`
2. Click **Settings** (top menu)
3. In the left sidebar, click **Secrets and variables** → **Actions**

### Step 2: Add New Secret

1. Click **New repository secret**
2. Enter the **Name** (exactly as listed above)
3. Enter the **Secret** value
4. Click **Add secret**

### Step 3: Verify Secrets

After adding secrets, they will appear in the list (values are hidden for security).

## Environment-Specific Secrets

GitHub also supports **Environment Secrets** which are scoped to specific environments:

### Setting Up Environment Secrets

1. Go to **Settings** → **Environments**
2. Create environments: `dev`, `staging`, `production`
3. Add secrets to each environment
4. Configure protection rules (required reviewers, wait timers)

### Benefits of Environment Secrets

- Secrets are scoped to specific environments
- Can require approval for production deployments
- Better security and access control

## Security Best Practices

1. **Never commit secrets to code**
   - Use GitHub Secrets for all sensitive data
   - Use `.gitignore` to exclude config files with secrets

2. **Rotate secrets regularly**
   - Rotate AWS keys every 90 days
   - Rotate database passwords periodically
   - Update GitHub Secrets when rotating

3. **Use least privilege**
   - Create IAM users with minimal required permissions
   - Use separate AWS credentials for each environment

4. **Monitor secret usage**
   - Review GitHub Actions logs (secrets are masked)
   - Monitor AWS CloudTrail for API access

5. **Use environment protection**
   - Require approvals for production
   - Use deployment branches
   - Enable wait timers for critical deployments

## Testing Secrets

After adding secrets, test them by:

1. Triggering a workflow (push to `develop` branch)
2. Checking workflow logs (secrets will be masked)
3. Verifying deployment succeeds

## Troubleshooting

### Secret Not Found Error

**Error:** `Secret 'SECRET_NAME' not found`

**Solution:**
- Verify secret name matches exactly (case-sensitive)
- Check that secret is added to the correct repository
- For environment secrets, verify environment name matches

### Access Denied Error

**Error:** `Access denied` when using AWS credentials

**Solution:**
- Verify IAM user has required permissions
- Check AWS credentials are correct
- Ensure IAM policies are attached

### Connection String Error

**Error:** Database connection fails

**Solution:**
- Verify connection string format is correct
- Check RDS security group allows connections
- Verify database endpoint is correct
- Test connection string locally first

## Current Status

### ✅ Configured Secrets (Required Now)

- [x] `AWS_ACCESS_KEY_ID` ✅ **CONFIGURED**
- [x] `AWS_SECRET_ACCESS_KEY` ✅ **CONFIGURED**
- [x] `DEV_DB_CONNECTION_STRING` ✅ **CONFIGURED**

### ⏳ Future Secrets (As Needed)

- [ ] `STAGING_DB_CONNECTION_STRING` (when staging is deployed)
- [ ] `PROD_DB_CONNECTION_STRING` (when production is deployed)
- [ ] `DEV_API_URL` (when frontend needs API URL)
- [ ] `STRIPE_SECRET_KEY` (when payment integration is added)
- [ ] `SENDGRID_API_KEY` (when email service is added)
- [ ] `JWT_SECRET` (when JWT is configured)

## Quick Setup Checklist

- [x] Add AWS credentials to GitHub Secrets ✅
- [x] Get Dev database endpoint from Terraform ✅
- [x] Create Dev database connection string ✅
- [x] Add `DEV_DB_CONNECTION_STRING` to GitHub Secrets ✅
- [ ] Test workflow by pushing to `develop` branch (in progress)
- [ ] Verify workflow runs successfully

---

**Note:** Secrets are encrypted and can only be viewed when adding/editing. Once saved, the value cannot be retrieved (only updated or deleted).

