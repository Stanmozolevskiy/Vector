# Fix SSM Session Manager Permissions

## Problem

The IAM user `Vector-Infrastructure` doesn't have permission to use SSM Session Manager, resulting in:

```
AccessDeniedException: User is not authorized to perform: ssm:StartSession
```

## Solution: Add SSM Permissions

### Option 1: Quick Fix - Attach AWS Managed Policy (Recommended)

This is the simplest approach:

```powershell
# Attach the AWS managed policy for SSM Session Manager
aws iam attach-user-policy `
    --user-name Vector-Infrastructure `
    --policy-arn arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore
```

**Note:** This policy is designed for EC2 instances, but it also grants the necessary permissions for users to start sessions.

### Option 2: Create Custom Policy (More Restrictive)

If you want more granular control, create a custom policy:

```powershell
# 1. Create policy document
$policyDoc = @"
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "ssm:StartSession"
      ],
      "Resource": [
        "arn:aws:ec2:us-east-1:324795474468:instance/*",
        "arn:aws:ssm:us-east-1:324795474468:document/SSM-SessionManagerRunShell"
      ]
    },
    {
      "Effect": "Allow",
      "Action": [
        "ssm:DescribeInstanceInformation",
        "ssm:DescribeSessions",
        "ssm:GetConnectionStatus",
        "ssm:TerminateSession"
      ],
      "Resource": "*"
    }
  ]
}
"@

$policyDoc | Out-File -FilePath ssm-user-policy.json -Encoding UTF8

# 2. Create the policy
aws iam create-policy `
    --policy-name Vector-SSM-User-Policy `
    --policy-document file://ssm-user-policy.json `
    --description "Allows Vector-Infrastructure user to use SSM Session Manager"

# 3. Attach to user (replace POLICY_ARN with the ARN from step 2)
aws iam attach-user-policy `
    --user-name Vector-Infrastructure `
    --policy-arn arn:aws:iam::324795474468:policy/Vector-SSM-User-Policy
```

### Option 3: Via AWS Console

1. **Go to IAM Console:**
   - Navigate to: https://console.aws.amazon.com/iam/
   - Click: **Users** → **Vector-Infrastructure**

2. **Add Permissions:**
   - Click: **Add permissions** → **Attach policies directly**
   - Search for: `AmazonSSMManagedInstanceCore`
   - Check the box and click **Next** → **Add permissions**

   **OR** create a custom policy:
   - Click: **Add permissions** → **Create inline policy**
   - Click: **JSON** tab
   - Paste the policy from Option 2 above
   - Name it: `Vector-SSM-User-Policy`
   - Click: **Create policy**

## Verify Permissions

After adding permissions, test SSM access:

```powershell
# Get bastion instance ID
cd infrastructure/terraform
$INSTANCE_ID = terraform output -raw bastion_instance_id

# Test SSM connection
aws ssm start-session --target $INSTANCE_ID --region us-east-1
```

## Required Permissions Summary

The IAM user needs these permissions:

| Permission | Resource | Purpose |
|------------|----------|---------|
| `ssm:StartSession` | EC2 instances, SSM documents | Start SSM sessions |
| `ssm:DescribeInstanceInformation` | `*` | List instances |
| `ssm:DescribeSessions` | `*` | List active sessions |
| `ssm:GetConnectionStatus` | `*` | Check connection status |
| `ssm:TerminateSession` | `*` | End sessions |

## Alternative: Use SSH Instead

If you prefer not to modify IAM permissions, you can use SSH (once the security group is configured):

```powershell
ssh -i "$env:USERPROFILE\.ssh\dev-bastion-key" -L 5433:dev-postgres.cahsciiy4v4q.us-east-1.rds.amazonaws.com:5432 ec2-user@13.216.193.180
```

## Troubleshooting

### Still Getting AccessDenied?

1. **Wait a few minutes** - IAM changes can take 1-2 minutes to propagate
2. **Verify policy attachment:**
   ```powershell
   aws iam list-attached-user-policies --user-name Vector-Infrastructure
   ```
3. **Check policy contents:**
   ```powershell
   aws iam get-policy --policy-arn <POLICY_ARN>
   aws iam get-policy-version --policy-arn <POLICY_ARN> --version-id <VERSION>
   ```

### Instance Not Showing in SSM?

The bastion instance needs:
- ✅ SSM Agent installed (pre-installed on Amazon Linux 2023)
- ✅ IAM role with `AmazonSSMManagedInstanceCore` policy (already configured)
- ✅ Network connectivity to SSM endpoints

Verify instance is registered:

```powershell
aws ssm describe-instance-information --filters "Key=InstanceIds,Values=i-08c32ba31f518fdac"
```

If no results, the instance may need to be restarted or the SSM agent may need to be started.

---

**Last Updated:** December 7, 2025

