# Version Control & CI/CD Pipeline Setup

## Version Control Recommendation

### **Recommended: GitHub** 🏆

#### Why GitHub?

1. **Industry Standard**
   - Most widely used platform
   - Excellent integration with other tools
   - Best support for open-source projects

2. **Cursor Integration**
   - Excellent Cursor IDE integration
   - Built-in GitHub support
   - Seamless workflow

3. **CI/CD Integration**
   - GitHub Actions (free for public repos, generous free tier for private)
   - Built-in CI/CD pipelines
   - Easy to set up and maintain

4. **Features**
   - Pull requests and code review
   - Issues and project management
   - GitHub Pages for documentation
   - Security scanning
   - Dependency management

5. **Ecosystem**
   - Largest community
   - Best third-party integrations
   - Extensive marketplace

### Alternative Options

#### GitLab
- ✅ Self-hosted option available
- ✅ Built-in CI/CD (GitLab CI)
- ✅ Good enterprise features
- ❌ Smaller community than GitHub
- ❌ Less Cursor integration

#### Bitbucket
- ✅ Good Jira integration (if using Atlassian)
- ✅ Free private repos
- ❌ Smaller ecosystem
- ❌ Less popular overall

#### Azure DevOps
- ✅ Good if using Azure
- ✅ Free for small teams
- ❌ Less intuitive interface
- ❌ Smaller community

---

## Repository Structure

### Recommended Structure

```
XponentAlternative/
├── .github/
│   └── workflows/          # GitHub Actions workflows
├── frontend/               # React application
│   ├── src/
│   ├── public/
│   └── package.json
├── backend/                # Node.js API
│   ├── src/
│   └── package.json
├── infrastructure/         # Infrastructure as Code
│   ├── terraform/         # Terraform configurations
│   └── cloudformation/    # AWS CloudFormation (optional)
├── docker/                # Docker configurations
│   ├── Dockerfile.frontend
│   ├── Dockerfile.backend
│   └── docker-compose.yml
├── docs/                  # Additional documentation
├── .gitignore
├── README.md
└── package.json           # Root package.json (optional monorepo)
```

### Branching Strategy

#### Git Flow (Recommended)

```
main                    # Production-ready code
├── develop             # Integration branch
│   ├── feature/user-auth
│   ├── feature/mock-interviews
│   ├── feature/resume-reviews
│   └── feature/courses
├── release/v1.0        # Release preparation
└── hotfix/critical-bug # Emergency fixes
```

**Branch Types:**
- `main`: Production branch (always deployable)
- `develop`: Development integration branch
- `feature/*`: New features (branched from develop)
- `release/*`: Release preparation (branched from develop)
- `hotfix/*`: Critical bug fixes (branched from main)

---

## CI/CD Pipeline Architecture

### Overview

```
Developer Push → GitHub → GitHub Actions → AWS
                                      ↓
                              Build → Test → Deploy
```

### Pipeline Stages

1. **Lint & Format** (fast feedback)
2. **Unit Tests** (run in parallel)
3. **Build** (create production artifacts)
4. **Integration Tests** (test services together)
5. **Security Scanning** (dependencies, code)
6. **Deploy to Staging** (automatic on develop branch)
7. **Deploy to Production** (manual approval for main branch)

---

## GitHub Actions Workflows

### Workflow 1: Frontend CI/CD

**File: `.github/workflows/frontend.yml`**

```yaml
name: Frontend CI/CD

on:
  push:
    branches: [main, develop]
    paths:
      - 'frontend/**'
      - '.github/workflows/frontend.yml'
  pull_request:
    branches: [main, develop]
    paths:
      - 'frontend/**'

env:
  NODE_VERSION: '20.x'
  AWS_REGION: us-east-1

jobs:
  lint-and-test:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: ./frontend
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json
      
      - name: Install dependencies
        run: npm ci
      
      - name: Run linter
        run: npm run lint
      
      - name: Run type check
        run: npm run type-check
      
      - name: Run tests
        run: npm run test:ci
      
      - name: Build application
        run: npm run build
        env:
          REACT_APP_API_URL: ${{ secrets.REACT_APP_API_URL }}
  
  deploy-staging:
    needs: lint-and-test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/develop'
    environment: staging
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}
      
      - name: Build and deploy to S3
        working-directory: ./frontend
        run: |
          npm ci
          npm run build
          aws s3 sync build/ s3://${{ secrets.AWS_S3_BUCKET_STAGING }} --delete
      
      - name: Invalidate CloudFront cache
        run: |
          aws cloudfront create-invalidation \
            --distribution-id ${{ secrets.AWS_CLOUDFRONT_DIST_ID_STAGING }} \
            --paths "/*"
  
  deploy-production:
    needs: lint-and-test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    environment: production
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}
      
      - name: Build and deploy to S3
        working-directory: ./frontend
        run: |
          npm ci
          npm run build
          aws s3 sync build/ s3://${{ secrets.AWS_S3_BUCKET_PRODUCTION }} --delete
      
      - name: Invalidate CloudFront cache
        run: |
          aws cloudfront create-invalidation \
            --distribution-id ${{ secrets.AWS_CLOUDFRONT_DIST_ID_PRODUCTION }} \
            --paths "/*"
```

### Workflow 2: Backend CI/CD

**File: `.github/workflows/backend.yml`**

```yaml
name: Backend CI/CD

on:
  push:
    branches: [main, develop]
    paths:
      - 'backend/**'
      - '.github/workflows/backend.yml'
  pull_request:
    branches: [main, develop]
    paths:
      - 'backend/**'

env:
  NODE_VERSION: '20.x'
  AWS_REGION: us-east-1

jobs:
  lint-and-test:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: ./backend
    
    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_PASSWORD: postgres
          POSTGRES_DB: testdb
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432
      
      redis:
        image: redis:7
        options: >-
          --health-cmd "redis-cli ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 6379:6379
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'
          cache-dependency-path: backend/package-lock.json
      
      - name: Install dependencies
        run: npm ci
      
      - name: Run linter
        run: npm run lint
      
      - name: Run type check
        run: npm run type-check
      
      - name: Run unit tests
        run: npm run test:unit
        env:
          DATABASE_URL: postgresql://postgres:postgres@localhost:5432/testdb
          REDIS_URL: redis://localhost:6379
      
      - name: Run integration tests
        run: npm run test:integration
        env:
          DATABASE_URL: postgresql://postgres:postgres@localhost:5432/testdb
          REDIS_URL: redis://localhost:6379
      
      - name: Build Docker image
        run: |
          docker build -t backend:latest -f docker/Dockerfile.backend .
  
  security-scan:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: ./backend
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Run npm audit
        run: npm audit --audit-level=high
      
      - name: Run Snyk security scan
        uses: snyk/actions/node@master
        env:
          SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}
        continue-on-error: true
  
  deploy-staging:
    needs: [lint-and-test, security-scan]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/develop'
    environment: staging
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}
      
      - name: Login to Amazon ECR
        id: login-ecr
        uses: aws-actions/amazon-ecr-login@v2
      
      - name: Build and push Docker image
        env:
          ECR_REGISTRY: ${{ steps.login-ecr.outputs.registry }}
          ECR_REPOSITORY: backend-staging
          IMAGE_TAG: ${{ github.sha }}
        run: |
          docker build -t $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG -f docker/Dockerfile.backend .
          docker push $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG
          docker tag $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG $ECR_REGISTRY/$ECR_REPOSITORY:latest
          docker push $ECR_REGISTRY/$ECR_REPOSITORY:latest
      
      - name: Deploy to ECS
        run: |
          aws ecs update-service \
            --cluster staging-cluster \
            --service backend-service \
            --force-new-deployment
  
  deploy-production:
    needs: [lint-and-test, security-scan]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    environment: production
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}
      
      - name: Login to Amazon ECR
        id: login-ecr
        uses: aws-actions/amazon-ecr-login@v2
      
      - name: Build and push Docker image
        env:
          ECR_REGISTRY: ${{ steps.login-ecr.outputs.registry }}
          ECR_REPOSITORY: backend-production
          IMAGE_TAG: ${{ github.sha }}
        run: |
          docker build -t $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG -f docker/Dockerfile.backend .
          docker push $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG
          docker tag $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG $ECR_REGISTRY/$ECR_REPOSITORY:latest
          docker push $ECR_REGISTRY/$ECR_REPOSITORY:latest
      
      - name: Deploy to ECS
        run: |
          aws ecs update-service \
            --cluster production-cluster \
            --service backend-service \
            --force-new-deployment
```

### Workflow 3: Infrastructure (Terraform)

**File: `.github/workflows/terraform.yml`**

```yaml
name: Terraform Infrastructure

on:
  push:
    branches: [main, develop]
    paths:
      - 'infrastructure/terraform/**'
      - '.github/workflows/terraform.yml'
  pull_request:
    branches: [main, develop]
    paths:
      - 'infrastructure/terraform/**'
    types: [opened, synchronize, reopened]

env:
  AWS_REGION: us-east-1
  TF_VERSION: 1.6.0

jobs:
  terraform-validate:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: ./infrastructure/terraform
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Setup Terraform
        uses: hashicorp/setup-terraform@v3
        with:
          terraform_version: ${{ env.TF_VERSION }}
      
      - name: Terraform Format Check
        run: terraform fmt -check
      
      - name: Terraform Init
        run: terraform init -backend=false
      
      - name: Terraform Validate
        run: terraform validate
      
      - name: Terraform Plan
        if: github.event_name == 'pull_request'
        env:
          TF_VAR_environment: ${{ github.base_ref == 'main' && 'production' || 'staging' }}
        run: terraform plan
  
  terraform-apply-staging:
    needs: terraform-validate
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/develop' && github.event_name == 'push'
    environment: staging
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}
      
      - name: Setup Terraform
        uses: hashicorp/setup-terraform@v3
        with:
          terraform_version: ${{ env.TF_VERSION }}
          terraform_wrapper: false
      
      - name: Terraform Init
        working-directory: ./infrastructure/terraform
        run: terraform init
      
      - name: Terraform Plan
        working-directory: ./infrastructure/terraform
        run: terraform plan -out=tfplan
        env:
          TF_VAR_environment: staging
      
      - name: Terraform Apply
        working-directory: ./infrastructure/terraform
        run: terraform apply -auto-approve tfplan
        env:
          TF_VAR_environment: staging
```

---

## Cursor IDE Integration

### Using Cursor with GitHub

#### 1. **Git Integration in Cursor**
- Cursor has built-in Git support
- Visual diff viewer
- Integrated terminal for Git commands
- Source control panel (Ctrl+Shift+G / Cmd+Shift+G)

#### 2. **GitHub Copilot Integration**
- Cursor can use GitHub Copilot for AI assistance
- Code completion and suggestions
- Works alongside Cursor's own AI features

#### 3. **Creating CI/CD Pipelines with Cursor**

**Yes, you can create CI/CD pipelines in Cursor!**

##### Option A: Manual Creation
1. Create `.github/workflows/` directory
2. Create workflow YAML files
3. Use Cursor's YAML editor with syntax highlighting
4. Commit and push to GitHub

##### Option B: AI-Assisted Creation
- Ask Cursor to generate GitHub Actions workflows
- Cursor can create workflow files based on your requirements
- Example prompt: "Create a GitHub Actions workflow for deploying a React app to AWS S3"

#### 4. **AWS Configuration with Cursor**

**Yes, you can configure AWS in Cursor!**

##### Infrastructure as Code (IaC) Options:

**Option 1: Terraform** (Recommended)
- Create Terraform files in Cursor
- Use Cursor's AI to generate Terraform configurations
- Use Cursor's terminal to run `terraform` commands
- Example prompt: "Create Terraform configuration for AWS ECS cluster"

**Option 2: AWS CDK** (TypeScript)
- Write CDK code in TypeScript
- Leverage Cursor's TypeScript support
- Type-safe infrastructure
- Example prompt: "Create AWS CDK stack for a Node.js API with ECS"

**Option 3: AWS CloudFormation** (YAML/JSON)
- Create CloudFormation templates in Cursor
- Use Cursor's YAML editor
- Less flexible than Terraform/CDK

#### 5. **Recommended Workflow with Cursor**

```
1. Design infrastructure in Cursor
   - Create Terraform/CDK files
   - Use AI to generate boilerplate

2. Create CI/CD pipelines in Cursor
   - Create GitHub Actions workflows
   - Configure AWS deployment steps

3. Test locally
   - Use Cursor terminal to run commands
   - Test Terraform plans locally

4. Commit and push
   - Use Cursor's Git integration
   - Create pull requests

5. Automate with GitHub Actions
   - Workflows run automatically
   - Deploy to AWS on merge
```

---

## GitHub Secrets Configuration

### Required Secrets for CI/CD

Set these in GitHub Repository Settings → Secrets and variables → Actions:

#### AWS Credentials
```
AWS_ACCESS_KEY_ID
AWS_SECRET_ACCESS_KEY
```

#### AWS Resources
```
AWS_S3_BUCKET_STAGING
AWS_S3_BUCKET_PRODUCTION
AWS_CLOUDFRONT_DIST_ID_STAGING
AWS_CLOUDFRONT_DIST_ID_PRODUCTION
AWS_ECR_REGISTRY
```

#### Application Configuration
```
REACT_APP_API_URL (for frontend builds)
DATABASE_URL (for migrations)
REDIS_URL
STRIPE_SECRET_KEY
SENDGRID_API_KEY
```

#### Optional
```
SNYK_TOKEN (for security scanning)
```

---

## AWS IAM Setup for CI/CD

### Create IAM User for GitHub Actions

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:ListBucket"
      ],
      "Resource": [
        "arn:aws:s3:::your-bucket-staging/*",
        "arn:aws:s3:::your-bucket-production/*",
        "arn:aws:s3:::your-bucket-staging",
        "arn:aws:s3:::your-bucket-production"
      ]
    },
    {
      "Effect": "Allow",
      "Action": [
        "cloudfront:CreateInvalidation"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "ecr:GetAuthorizationToken",
        "ecr:BatchCheckLayerAvailability",
        "ecr:GetDownloadUrlForLayer",
        "ecr:BatchGetImage",
        "ecr:PutImage",
        "ecr:InitiateLayerUpload",
        "ecr:UploadLayerPart",
        "ecr:CompleteLayerUpload"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "ecs:UpdateService",
        "ecs:DescribeServices"
      ],
      "Resource": "*"
    }
  ]
}
```

---

## Quick Start Guide

### 1. Initialize Repository

```bash
# Create repository on GitHub first, then:
git clone https://github.com/yourusername/XponentAlternative.git
cd XponentAlternative
git checkout -b develop
```

### 2. Create Workflow Files

Use Cursor to create the workflow files in `.github/workflows/`:
- `frontend.yml`
- `backend.yml`
- `terraform.yml`

### 3. Configure Secrets

Go to GitHub → Settings → Secrets → Actions, add all required secrets

### 4. Test Pipeline

```bash
# Create a test branch
git checkout -b feature/test-pipeline

# Make a small change
echo "# Test" >> README.md

# Commit and push
git add .
git commit -m "Test CI/CD pipeline"
git push origin feature/test-pipeline

# Create pull request on GitHub
# Check Actions tab to see pipeline run
```

---

## Best Practices

1. **Use Feature Branches**: Never commit directly to `main` or `develop`
2. **Review Before Merge**: Require pull request reviews
3. **Test Before Deploy**: All tests must pass before deployment
4. **Staging First**: Always deploy to staging before production
5. **Monitor Deployments**: Check logs after each deployment
6. **Rollback Plan**: Have a plan to rollback if issues occur
7. **Secure Secrets**: Never commit secrets to repository
8. **Infrastructure as Code**: Use Terraform/CDK for all AWS resources

---

## Troubleshooting

### Pipeline Failures
- Check GitHub Actions logs
- Verify secrets are set correctly
- Ensure AWS credentials have correct permissions
- Check service dependencies (database, Redis)

### AWS Deployment Issues
- Verify AWS credentials
- Check IAM permissions
- Review CloudWatch logs
- Verify resource limits

---

## Next Steps

1. Set up GitHub repository
2. Create initial workflow files in Cursor
3. Configure AWS credentials and secrets
4. Test pipeline with a simple change
5. Set up infrastructure with Terraform
6. Deploy first version to staging

