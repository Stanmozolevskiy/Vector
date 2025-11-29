# AWS Configuration Fix

## Issue: SignatureDoesNotMatch Error

The error "SignatureDoesNotMatch" typically occurs when:
1. The secret key has special characters that weren't properly escaped
2. There are extra spaces or newlines in the credentials
3. The credentials are incorrect

## Solution: Manual Configuration (Recommended)

Since your secret key contains special characters (`/`, `+`), use manual configuration:

### Option 1: Use AWS Configure Directly

```powershell
aws configure
```

Then enter:
1. **AWS Access Key ID:** `AKIAUXH2RUISKZ363JPK`
2. **AWS Secret Access Key:** `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`
3. **Default region name:** `us-east-1`
4. **Default output format:** `json`

### Option 2: Use the Manual Script

```powershell
.\configure-aws-manual.ps1
```

This script will guide you through the process.

### Option 3: Edit Credentials File Directly

1. Navigate to: `C:\Users\stanm\.aws\`
2. Edit `credentials` file:
   ```ini
   [default]
   aws_access_key_id = AKIAUXH2RUISKZ363JPK
   aws_secret_access_key = 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
   ```

3. Edit `config` file:
   ```ini
   [default]
   region = us-east-1
   output = json
   ```

## Verify Configuration

After configuring, verify with:

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

## Common Issues

### Issue: Still getting SignatureDoesNotMatch

**Check:**
- No extra spaces before/after the secret key
- No quotes around the values in the credentials file
- Secret key is copied completely (no truncation)
- Access key is correct

### Issue: Access Denied

**Check:**
- IAM user has necessary permissions
- Access key is active (not disabled)
- MFA is not required (or configure MFA)

## Next Steps

Once AWS is configured:

1. ✅ Verify connection: `aws sts get-caller-identity`
2. ⏳ Edit `infrastructure/terraform/terraform.tfvars`:
   - Set a strong `db_password`
3. ⏳ Review Terraform plan:
   ```powershell
   cd infrastructure/terraform
   terraform plan
   ```
4. ⏳ Apply infrastructure (when ready):
   ```powershell
   terraform apply
   ```

