# Terraform RDS Free Tier Fix

## Issue

When running `terraform apply`, you encountered this error:

```
Error: creating RDS DB Instance (dev-postgres): operation error RDS: CreateDBInstance, 
https response error StatusCode: 400, RequestID: ..., 
api error FreeTierRestrictionError: The specified backup retention period exceeds 
the maximum available to free tier customers.
```

## Root Cause

AWS Free Tier for RDS has a **maximum backup retention period of 1 day** (not 7 days). The Terraform configuration was set to 7 days, which exceeds the Free Tier limit.

## Solution Applied

Updated `infrastructure/terraform/modules/rds/main.tf` to use environment-based backup retention:

```hcl
# Free Tier allows 0-1 day backup retention. Use 1 day for dev, 7 for production
backup_retention_period = var.environment == "dev" ? 1 : 7
```

**For dev environment:** 1 day (Free Tier compliant)  
**For production:** 7 days (standard retention)

## Next Steps

1. **Re-run Terraform Apply:**
   ```powershell
   cd infrastructure/terraform
   terraform apply
   ```

2. **The RDS instance should now create successfully** with a 1-day backup retention period.

## AWS Free Tier RDS Limits

For reference, AWS Free Tier RDS includes:
- **Instance:** db.t2.micro or db.t3.micro
- **Storage:** 20 GB General Purpose (SSD)
- **Backup Storage:** 20 GB
- **Backup Retention:** 0-1 day (maximum)
- **Valid for:** 12 months from account creation

## Verification

After successful creation, verify the RDS instance:

```powershell
aws rds describe-db-instances --db-instance-identifier dev-postgres
```

Check the `BackupRetentionPeriod` - it should be `1` for dev environment.

## Cost Impact

- **Backup Retention 1 day:** Minimal cost (within Free Tier)
- **Backup Retention 7 days:** Would exceed Free Tier and incur charges

The fix ensures you stay within Free Tier limits for the dev environment.

