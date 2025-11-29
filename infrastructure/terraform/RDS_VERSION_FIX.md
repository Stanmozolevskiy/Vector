# RDS PostgreSQL Version Fix

## Issue

When running `terraform apply`, you encountered this error:

```
Error: creating RDS DB Instance (dev-postgres): operation error RDS: CreateDBInstance, 
api error InvalidParameterCombination: Cannot find version 15.4 for postgres
```

## Root Cause

AWS RDS doesn't have PostgreSQL version `15.4` available. The exact available versions depend on:
- AWS region
- Current AWS RDS offerings
- Version lifecycle (some versions are deprecated)

## Solution Applied

### Option 1: Use Data Source (Recommended) ✅

Added a data source to automatically fetch the latest available PostgreSQL 15 version:

```hcl
data "aws_rds_engine_version" "postgres" {
  engine  = "postgres"
  version = "15"
}

module "database" {
  ...
  engine_version = data.aws_rds_engine_version.postgres.version
  ...
}
```

**Benefits:**
- Automatically uses the latest available PostgreSQL 15.x version
- No need to manually update version numbers
- Works across different AWS regions

### Option 2: Configurable Version (Fallback)

Also made `engine_version` a variable in the RDS module with default `15.5`:

```hcl
variable "engine_version" {
  description = "PostgreSQL engine version"
  type        = string
  default     = "15.5"
}
```

This allows manual override if needed.

## Next Steps

1. **Re-run Terraform Apply:**
   ```powershell
   cd infrastructure/terraform
   terraform apply
   ```

2. **The RDS instance should now create successfully** with an available PostgreSQL version.

## Verify Available Versions

To check what PostgreSQL versions are available in your region:

```powershell
aws rds describe-db-engine-versions --engine postgres --query 'DBEngineVersions[*].EngineVersion' --output table
```

Or in Terraform, you can see the selected version:

```powershell
terraform plan
# Look for: data.aws_rds_engine_version.postgres
```

## Common PostgreSQL 15 Versions in AWS RDS

Typical available versions (may vary by region):
- `15.3`
- `15.5`
- `15.6`
- `15.7`
- `15.8` (if available)

The data source will automatically select the latest available version in the `15.x` series.

## Alternative: Specify Exact Version

If you need a specific version, you can override it:

```hcl
module "database" {
  ...
  engine_version = "15.5"  # or any available version
  ...
}
```

But using the data source is recommended as it's more flexible.

