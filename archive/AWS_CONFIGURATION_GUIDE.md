# AWS Configuration Guide

## Option 1: Use the Configuration Script (Recommended)

I've created a PowerShell script to help you configure AWS credentials:

```powershell
.\configure-aws.ps1
```

The script will:
1. Check if AWS CLI is installed
2. Prompt for your credentials
3. Configure AWS CLI
4. Verify the connection
5. Show your account information

### Using the Script with Parameters

You can also provide credentials directly (less secure):

```powershell
.\configure-aws.ps1 -AccessKeyId "YOUR_ACCESS_KEY" -SecretAccessKey "YOUR_SECRET_KEY" -Region "us-east-1"
```

**Note:** This is less secure as credentials may appear in command history.

## Option 2: Manual Configuration

### Step 1: Run AWS Configure

```powershell
aws configure
```

You'll be prompted for:
- **AWS Access Key ID:** Your access key
- **AWS Secret Access Key:** Your secret key
- **Default region name:** `us-east-1` (or your preferred region)
- **Default output format:** `json`

### Step 2: Verify Configuration

```powershell
aws sts get-caller-identity
```

**Expected Output:**
```json
{
    "UserId": "AIDA...",
    "Account": "123456789012",
    "Arn": "arn:aws:iam::123456789012:user/your-username"
}
```

## Option 3: Environment Variables

You can also set credentials via environment variables:

```powershell
$env:AWS_ACCESS_KEY_ID = "YOUR_ACCESS_KEY"
$env:AWS_SECRET_ACCESS_KEY = "YOUR_SECRET_KEY"
$env:AWS_DEFAULT_REGION = "us-east-1"
```

**Note:** These are only for the current session. For persistence, use `aws configure`.

## Where Credentials Are Stored

After configuration, credentials are stored in:

- **Windows:** `C:\Users\YourUsername\.aws\credentials`
- **Config:** `C:\Users\YourUsername\.aws\config`

**Security:** These files are protected by Windows permissions, but keep them secure.

**✅ Configuration Complete:** AWS credentials have been successfully configured and verified.

## Getting AWS Credentials

If you don't have AWS credentials yet:

1. **Sign in to AWS Console:** https://console.aws.amazon.com/
2. **Go to IAM:** https://console.aws.amazon.com/iam/
3. **Users → Your User → Security Credentials**
4. **Create Access Key:**
   - Click "Create access key"
   - Choose "Command Line Interface (CLI)"
   - Download or copy the keys
   - **Important:** Save the Secret Access Key immediately (you can't view it again)

## Required IAM Permissions

For Terraform to work, your AWS user/role needs these permissions:

- **EC2:** Create VPC, subnets, security groups, NAT gateways
- **RDS:** Create database instances, subnet groups
- **ElastiCache:** Create Redis clusters, subnet groups
- **S3:** Create buckets, manage bucket policies
- **IAM:** Read account information (for S3 bucket policies)

**✅ IAM Policies Configured:** The following AWS managed policies have been attached:
- `AmazonEC2FullAccess` - For VPC, subnets, NAT gateways
- `AmazonRDSFullAccess` - For RDS database instances
- `AmazonElastiCacheFullAccess` - For Redis clusters
- `AmazonS3FullAccess` - For S3 buckets
- `IAMReadOnlyAccess` - For reading account information

**Note:** These policies provide full access. For production, consider creating custom policies with least privilege.

## Testing the Configuration

After configuration, test with:

```powershell
# Verify connection
aws sts get-caller-identity

# List S3 buckets (test S3 access)
aws s3 ls

# List EC2 regions (test EC2 access)
aws ec2 describe-regions
```

## Next Steps After Configuration

1. ✅ AWS credentials configured
2. ⏳ Edit `infrastructure/terraform/terraform.tfvars`:
   - Set a strong `db_password`
   - Adjust other values if needed
3. ⏳ Review Terraform plan:
   ```powershell
   cd infrastructure/terraform
   terraform plan
   ```
4. ⏳ Apply infrastructure (when ready):
   ```powershell
   terraform apply
   ```

## Security Best Practices

1. ✅ **Never commit credentials** to Git
2. ✅ **Use IAM roles** when possible (instead of access keys)
3. ✅ **Rotate credentials** regularly
4. ✅ **Use MFA** on your AWS account
5. ✅ **Limit permissions** to what's needed
6. ✅ **Use separate credentials** for different environments

## Troubleshooting

### Error: "Unable to locate credentials"

**Solution:**
- Run `aws configure` again
- Check if credentials file exists: `Test-Path $env:USERPROFILE\.aws\credentials`
- Verify credentials are correct

### Error: "Access Denied"

**Solution:**
- Check IAM permissions
- Verify the access key is active
- Check if MFA is required

### Error: "Invalid credentials"

**Solution:**
- Verify access key and secret key are correct
- Check if credentials have expired
- Create new access key if needed

## Cost Awareness

Before running `terraform apply`, be aware of estimated costs:

- **RDS db.t3.micro:** ~$15/month
- **ElastiCache cache.t3.micro:** ~$12/month
- **NAT Gateway:** ~$32/month
- **S3:** ~$1/month
- **Total:** ~$60/month for dev environment

**Remember:** Always run `terraform plan` first to review what will be created!

