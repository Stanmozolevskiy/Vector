# Email Fix Summary - AWS Dev

## Problem

Emails were not being sent on AWS dev environment for:
1. Registration verification emails
2. Password reset emails

## Root Cause

**SendGrid API key was not configured in the ECS task definition.**

### Evidence from Logs
```
ApiKey first 10 chars: 'your_sendg'  ← Placeholder value!
SendGrid API Key is not configured. Email sending is disabled.
ApiKey value: 'your_sendgrid_api_key_here'
```

The ECS task was receiving a placeholder value instead of the real SendGrid API key.

## Solution Applied

### Step 1: Updated Terraform Variables
**File:** `infrastructure/terraform/terraform.tfvars`

Added SendGrid configuration:
```hcl
sendgrid_api_key = "YOUR_SENDGRID_API_KEY_HERE"
sendgrid_from_email = "stanmozolevskiy90@gmail.com"
sendgrid_from_name = "Vector"
```

### Step 2: Applied Terraform Changes
```powershell
cd infrastructure/terraform
terraform plan -out=tfplan
terraform apply tfplan
```

**Result:**
- ECS task definition updated with SendGrid environment variables
- Backend service redeployed with new configuration

### Step 3: Verified Configuration
Check logs after deployment:
```powershell
aws logs tail /ecs/dev-vector --since 5m --region us-east-1 | Select-String "SendGrid|ApiKey"
```

**Expected output:**
- `ApiKey first 10 chars: 'SG.w7I6-sV'` ✅
- `SendGrid email service initialized successfully!` ✅

## How SendGrid Config Works

### Terraform Flow
1. `terraform.tfvars` → Variables defined
2. `main.tf` → Passes to ECS module
3. `modules/ecs/task_definition.tf` → Sets environment variables
4. ECS task → Receives env vars
5. Backend container → Reads env vars

### Environment Variables in ECS
```json
{
  "name": "SendGrid__ApiKey",
  "value": "YOUR_SENDGRID_API_KEY"
},
{
  "name": "SendGrid__FromEmail",
  "value": "stanmozolevskiy90@gmail.com"
},
{
  "name": "SendGrid__FromName",
  "value": "Vector"
}
```

### Backend Reads Configuration
```csharp
// EmailService.cs reads env vars with multiple fallbacks
var apiKey = _configuration["SendGrid:ApiKey"] 
          ?? Environment.GetEnvironmentVariable("SendGrid__ApiKey")
          ?? _configuration["SendGrid__ApiKey"];
```

## Verification Steps

### 1. Check ECS Task Configuration
```powershell
# Get task definition
aws ecs describe-task-definition --task-definition dev-vector-backend --region us-east-1 --query 'taskDefinition.containerDefinitions[0].environment' | Select-String "SendGrid"
```

### 2. Check Running Container
```powershell
# Get task ARN
$TASK_ARN = (aws ecs list-tasks --cluster dev-vector-cluster --service-name dev-vector-backend-service --region us-east-1 --query 'taskArns[0]' --output text)

# Describe task
aws ecs describe-tasks --cluster dev-vector-cluster --tasks $TASK_ARN --region us-east-1
```

### 3. Check Logs for SendGrid Init
```powershell
aws logs tail /ecs/dev-vector --since 10m --region us-east-1 | Select-String "SendGrid Configuration Debug" -Context 5,10
```

## Testing

### Test Registration Email
1. Go to: http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com
2. Register a new account
3. Check logs:
   ```powershell
   aws logs tail /ecs/dev-vector --follow --region us-east-1
   ```
4. Look for:
   - `Attempting to send verification email`
   - `Sending email via SendGrid`
   - `SendGrid response status code: Accepted`
   - `Verification email sent successfully`

### Test Password Reset Email
1. Go to: http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/forgot-password
2. Enter email
3. Check logs:
   ```powershell
   aws logs tail /ecs/dev-vector --follow --region us-east-1 | Select-String "password reset"
   ```
4. Check email inbox

## Future: Using GitHub Secrets (Recommended)

For better security, use GitHub Secrets instead of Terraform variables:

### Option 1: Update via CI/CD
Add to GitHub Secrets:
- `SENDGRID_API_KEY`
- `SENDGRID_FROM_EMAIL`

Update CI/CD workflows to pass these to ECS.

### Option 2: AWS Secrets Manager
Store in AWS Secrets Manager and reference in ECS task definition.

## Monitoring Email Sending

### Real-Time Monitoring
```powershell
# Watch for email events
aws logs tail /ecs/dev-vector --follow --region us-east-1 | Select-String "Email|SendGrid|verification|password reset"
```

### Check SendGrid Dashboard
1. Go to: https://app.sendgrid.com
2. Activity Feed → View all email activity
3. Should see emails being sent from AWS

## Common Issues After Fix

### Issue: Still Not Sending
**Cause:** Old task still running

**Solution:**
```powershell
# Force new deployment
aws ecs update-service --cluster dev-vector-cluster --service dev-vector-backend-service --force-new-deployment --region us-east-1
```

### Issue: SendGrid API Error
**Cause:** API key invalid or revoked

**Solution:**
- Check SendGrid dashboard for API key status
- Generate new API key if needed
- Update terraform.tfvars and reapply

## Current Status

✅ **Fixed:**
- SendGrid API key configured in Terraform
- ECS task definition updated
- Backend service redeploying

⏳ **In Progress:**
- ECS deploying new task with SendGrid config
- Old task will drain after new task is healthy

🧪 **Ready to Test:**
- Register account on AWS dev
- Request password reset on AWS dev
- Emails should now be sent via SendGrid

