# Terraform & AWS Setup Guide

## Prerequisites Installation

### 1. Install AWS CLI

**Windows:**
1. Download AWS CLI MSI installer: https://awscli.amazonaws.com/AWSCLIV2.msi
2. Run the installer
3. Verify installation:
   ```powershell
   aws --version
   ```

**Alternative (using Chocolatey):**
```powershell
choco install awscli
```

### 2. Install Terraform

**Windows:**
1. Download Terraform: https://developer.hashicorp.com/terraform/downloads
2. Extract to a folder (e.g., `C:\terraform`)
3. Add to PATH:
   - System Properties → Environment Variables
   - Add `C:\terraform` to Path
4. Verify installation:
   ```powershell
   terraform --version
   ```

**Alternative (using Chocolatey):**
```powershell
choco install terraform
```

### 3. Configure AWS Credentials

**Option 1: AWS CLI Configure**
```powershell
aws configure
```
Enter:
- AWS Access Key ID
- AWS Secret Access Key
- Default region: `us-east-1` (or your preferred region)
- Default output format: `json`

**Option 2: Environment Variables**
```powershell
$env:AWS_ACCESS_KEY_ID="your-access-key"
$env:AWS_SECRET_ACCESS_KEY="your-secret-key"
$env:AWS_DEFAULT_REGION="us-east-1"
```

**Option 3: AWS Credentials File**
Create `C:\Users\YourUsername\.aws\credentials`:
```ini
[default]
aws_access_key_id = your-access-key
aws_secret_access_key = your-secret-key
```

Create `C:\Users\YourUsername\.aws\config`:
```ini
[default]
region = us-east-1
```

### 4. Verify AWS Connection

```powershell
aws sts get-caller-identity
```

This should return your AWS account information.

## Terraform Initialization

### Step 1: Navigate to Terraform Directory

```powershell
cd infrastructure/terraform
```

### Step 2: Create terraform.tfvars File

Create `terraform.tfvars` (DO NOT COMMIT THIS FILE):

```hcl
aws_region        = "us-east-1"
environment       = "dev"
vpc_cidr          = "10.0.0.0/16"
db_instance_class = "db.t3.micro"
redis_node_type   = "cache.t3.micro"
db_name           = "vector_db"
db_username       = "postgres"
db_password       = "CHANGE_THIS_TO_SECURE_PASSWORD"
```

**Important:** Use a strong password for `db_password`!

### Step 3: Initialize Terraform

```powershell
terraform init
```

This will:
- Download the AWS provider
- Initialize the backend
- Set up modules

### Step 4: Validate Configuration

```powershell
terraform validate
```

### Step 5: Plan Infrastructure

```powershell
terraform plan
```

This will show you what resources will be created without actually creating them.

### Step 6: Apply Infrastructure (When Ready)

```powershell
terraform apply
```

Type `yes` when prompted to confirm.

**Note:** This will create real AWS resources and incur costs. Start with `dev` environment.

### Step 7: View Outputs

After applying, view the outputs:

```powershell
terraform output
```

### Step 8: Destroy Infrastructure (When Done Testing)

```powershell
terraform destroy
```

## Cost Estimation

For `dev` environment with default settings:
- RDS db.t3.micro: ~$15/month
- ElastiCache cache.t3.micro: ~$12/month
- S3: Minimal cost (pay per GB)
- VPC/NAT Gateway: ~$32/month (NAT Gateway is the main cost)
- **Total: ~$60/month for dev environment**

**Cost Optimization Tips:**
- Use NAT Instance instead of NAT Gateway for dev (saves ~$30/month)
- Use smaller instance types
- Stop/terminate resources when not in use

## Security Best Practices

1. **Never commit `terraform.tfvars`** - It contains sensitive data
2. **Use AWS Secrets Manager** for production passwords
3. **Enable MFA** on AWS account
4. **Use IAM roles** with least privilege
5. **Enable CloudTrail** for audit logging
6. **Use encrypted storage** (already configured in modules)

## Remote State (Optional - For Team Collaboration)

### Create S3 Bucket for State

```powershell
aws s3 mb s3://vector-terraform-state
aws s3api put-bucket-versioning --bucket vector-terraform-state --versioning-configuration Status=Enabled
aws s3api put-bucket-encryption --bucket vector-terraform-state --server-side-encryption-configuration '{"Rules":[{"ApplyServerSideEncryptionByDefault":{"SSEAlgorithm":"AES256"}}]}'
```

### Update main.tf

Uncomment the backend block in `main.tf`:

```hcl
backend "s3" {
  bucket = "vector-terraform-state"
  key    = "stage1/terraform.tfstate"
  region = "us-east-1"
}
```

Then reinitialize:
```powershell
terraform init -migrate-state
```

## Troubleshooting

### Error: "No valid credential sources found"
- Verify AWS credentials: `aws sts get-caller-identity`
- Check environment variables or credentials file

### Error: "Insufficient permissions"
- Ensure IAM user/role has necessary permissions
- Required permissions: EC2, RDS, ElastiCache, S3, VPC, IAM (for some operations)

### Error: "Resource already exists"
- Check if resources were created manually
- Import existing resources or destroy and recreate

## Next Steps

After infrastructure is created:
1. Update backend `appsettings.json` with RDS endpoint
2. Update backend `appsettings.json` with Redis endpoint
3. Update backend `appsettings.json` with S3 bucket name
4. Test database connection
5. Test Redis connection
6. Test S3 upload

