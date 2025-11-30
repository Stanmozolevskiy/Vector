# AWS IAM Permissions Required

## Summary

The Terraform deployment requires additional IAM permissions for the `Vector-Infrastructure` IAM user. The following permissions are needed:

## Required Permissions

### 1. ECR (Elastic Container Registry)
- `ecr:CreateRepository`
- `ecr:DescribeRepositories`
- `ecr:PutLifecyclePolicy`
- `ecr:GetLifecyclePolicy`
- `ecr:PutImageScanningConfiguration`
- `ecr:GetImageScanningConfiguration`

**Policy:** `AmazonEC2ContainerRegistryFullAccess` (or create custom policy with above permissions)

### 2. ECS (Elastic Container Service)
- `ecs:CreateCluster`
- `ecs:DescribeClusters`
- `ecs:RegisterTaskDefinition`
- `ecs:CreateService`
- `ecs:DescribeServices`
- `ecs:UpdateService`
- `ecs:ListTasks`
- `ecs:DescribeTasks`

**Policy:** `AmazonECS_FullAccess` (or create custom policy with above permissions)

### 3. CloudWatch Logs
- `logs:CreateLogGroup`
- `logs:DescribeLogGroups`
- `logs:PutRetentionPolicy`
- `logs:DeleteLogGroup`

**Policy:** `CloudWatchLogsFullAccess` (or create custom policy with above permissions)

### 4. IAM (for creating ECS task roles)
- `iam:CreateRole`
- `iam:AttachRolePolicy`
- `iam:PutRolePolicy`
- `iam:GetRole`
- `iam:ListRolePolicies`
- `iam:ListAttachedRolePolicies`
- `iam:PassRole` (for ECS task execution role)

**Policy:** Create a custom policy or use `IAMFullAccess` (not recommended for production)

## Quick Fix

Attach these managed policies to the `Vector-Infrastructure` IAM user:

1. **AmazonEC2ContainerRegistryFullAccess**
2. **AmazonECS_FullAccess**
3. **CloudWatchLogsFullAccess**
4. **IAMFullAccess** (or create a custom policy with only the permissions above)

## Steps to Add Permissions

1. Go to AWS IAM Console
2. Navigate to Users → `Vector-Infrastructure`
3. Click "Add permissions" → "Attach policies directly"
4. Search and attach the policies listed above
5. Click "Next" → "Add permissions"

## After Adding Permissions

Re-run Terraform apply:

```powershell
cd infrastructure/terraform
terraform apply -var="environment=dev" -var="db_password=$Memic1234" -auto-approve
```

## Security Note

For production, create custom IAM policies with only the minimum required permissions instead of using full access policies.

