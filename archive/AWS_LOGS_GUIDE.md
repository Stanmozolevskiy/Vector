# AWS Application Logs Guide

## Overview

This guide explains how to view and monitor application logs for the Vector platform running on AWS.

## Log Locations

### CloudWatch Log Groups

All ECS container logs are stored in AWS CloudWatch:

- **Backend Logs:** `/ecs/dev-vector`
- **Log Streams:** Each container has its own stream (e.g., `ecs/vector-backend/[task-id]`)
- **Frontend Logs:** Same log group (`/ecs/dev-vector`), prefix `ecs-frontend`

## Viewing Logs

### Method 1: AWS CLI (Recommended)

#### View Recent Backend Logs
```powershell
# Last 30 minutes
aws logs tail /ecs/dev-vector --since 30m --region us-east-1

# Live tail (follow mode)
aws logs tail /ecs/dev-vector --follow --region us-east-1

# Filter for specific patterns
aws logs tail /ecs/dev-vector --since 1h --region us-east-1 | Select-String -Pattern "error|Error|SendGrid|Email"
```

#### View Frontend Logs
```powershell
# Frontend logs (same log group, different prefix)
aws logs tail /ecs/dev-vector --since 30m --region us-east-1 | Select-String -Pattern "ecs-frontend"
```

#### Filter Logs by Pattern
```powershell
# Registration events
aws logs tail /ecs/dev-vector --since 1h --region us-east-1 | Select-String -Pattern "Registration|register"

# Email events
aws logs tail /ecs/dev-vector --since 1h --region us-east-1 | Select-String -Pattern "SendGrid|Email|email"

# Errors only
aws logs tail /ecs/dev-vector --since 1h --region us-east-1 | Select-String -Pattern "error|Error|ERROR|Exception"

# Database queries
aws logs tail /ecs/dev-vector --since 30m --region us-east-1 | Select-String -Pattern "Executed DbCommand"
```

### Method 2: AWS Console

1. **Navigate to CloudWatch:**
   - Go to AWS Console: https://console.aws.amazon.com/cloudwatch
   - Select region: **US East (N. Virginia) us-east-1**

2. **Find Log Group:**
   - Left menu → **Logs** → **Log groups**
   - Search for: `/ecs/dev-vector`
   - Click on the log group

3. **View Log Streams:**
   - Click on a log stream (e.g., `ecs/vector-backend/[task-id]`)
   - View logs in real-time or historical

4. **Use CloudWatch Logs Insights:**
   - Click **Logs Insights** in left menu
   - Select log group: `/ecs/dev-vector`
   - Run queries:

```sql
-- All errors
fields @timestamp, @message
| filter @message like /error|Error|ERROR/
| sort @timestamp desc
| limit 50

-- Email-related logs
fields @timestamp, @message
| filter @message like /SendGrid|Email|email/
| sort @timestamp desc
| limit 50

-- Registration events
fields @timestamp, @message
| filter @message like /Registration|register/
| sort @timestamp desc
| limit 50
```

### Method 3: ECS Task Logs

#### Get Running Tasks
```powershell
# List backend tasks
aws ecs list-tasks --cluster dev-vector-cluster --service-name dev-vector-backend-service --region us-east-1

# Get task details
aws ecs describe-tasks --cluster dev-vector-cluster --tasks TASK_ARN --region us-east-1
```

#### View Specific Task Logs
```powershell
# Get task ID from list-tasks output
$TASK_ID = "d88e8d0116d64e799cfcbb37cb7ca03a"

# View logs for specific task
aws logs get-log-events --log-group-name /ecs/dev-vector --log-stream-name "ecs/vector-backend/$TASK_ID" --region us-east-1 --limit 100
```

## Common Log Patterns

### SendGrid Configuration Check
```powershell
aws logs tail /ecs/dev-vector --since 10m --region us-east-1 | Select-String -Pattern "SendGrid Configuration|ApiKey"
```

**What to look for:**
- `ApiKey first 10 chars: 'SG.w7I6-sV'` ✅ (configured)
- `ApiKey first 10 chars: 'your_sendg'` ❌ (not configured)

### Email Sending Events
```powershell
aws logs tail /ecs/dev-vector --since 30m --region us-east-1 | Select-String -Pattern "Attempting to send|email sent|EMAIL"
```

### Database Migrations
```powershell
aws logs tail /ecs/dev-vector --since 10m --region us-east-1 | Select-String -Pattern "migration|Migration|Database"
```

### Health Check Status
```powershell
aws logs tail /ecs/dev-vector --since 5m --region us-east-1 | Select-String -Pattern "health|Health"
```

## Troubleshooting with Logs

### Issue: No Emails Being Sent

**Check logs:**
```powershell
aws logs tail /ecs/dev-vector --since 1h --region us-east-1 | Select-String -Pattern "SendGrid|ApiKey"
```

**Look for:**
- `SendGrid API Key is not configured` ← Problem!
- `SendGrid email service initialized successfully!` ← Good!

### Issue: Registration Failing

**Check logs:**
```powershell
aws logs tail /ecs/dev-vector --since 30m --region us-east-1 | Select-String -Pattern "Registration|register|error"
```

### Issue: Login Not Working

**Check logs:**
```powershell
aws logs tail /ecs/dev-vector --since 30m --region us-east-1 | Select-String -Pattern "Login|login|users/me"
```

## Log Retention

- **Default:** 7 days (can be changed in Terraform)
- **Location:** ECS module `main.tf`, `aws_cloudwatch_log_group.ecs` resource
- **Cost:** Minimal for dev (free tier: 5 GB ingestion, 5 GB archival)

## Useful Commands

### Get Log Stream Names
```powershell
aws logs describe-log-streams --log-group-name /ecs/dev-vector --order-by LastEventTime --descending --region us-east-1 --max-items 5
```

### Download Logs to File
```powershell
aws logs tail /ecs/dev-vector --since 1h --region us-east-1 > backend-logs.txt
```

### Count Errors
```powershell
aws logs tail /ecs/dev-vector --since 1h --region us-east-1 | Select-String -Pattern "error|Error" | Measure-Object
```

### Search for Specific User
```powershell
aws logs tail /ecs/dev-vector --since 24h --region us-east-1 | Select-String -Pattern "user@example.com"
```

## Real-Time Monitoring

### Follow Logs in Real-Time
```powershell
# Follow all logs
aws logs tail /ecs/dev-vector --follow --region us-east-1

# Follow and filter
aws logs tail /ecs/dev-vector --follow --region us-east-1 | Select-String -Pattern "error|Email"
```

**Tip:** Keep this running in a separate PowerShell window while testing.

## Log Levels

The application uses these log levels:
- **Information:** General informational messages
- **Warning:** Warnings (e.g., SendGrid not configured)
- **Error:** Errors that need attention
- **Critical:** Critical errors

**Filter by level:**
```powershell
aws logs tail /ecs/dev-vector --since 1h --region us-east-1 | Select-String -Pattern "fail:|error:|crit:"
```

## Automated Log Alerts (Future)

You can set up CloudWatch Alarms for:
- Error rate threshold
- Email sending failures
- Database connection errors
- Container restarts

See Terraform documentation for CloudWatch Alarms configuration.

## Quick Reference

```powershell
# View recent logs
aws logs tail /ecs/dev-vector --since 30m --region us-east-1

# Follow logs live
aws logs tail /ecs/dev-vector --follow --region us-east-1

# Filter for errors
aws logs tail /ecs/dev-vector --since 1h --region us-east-1 | Select-String "error"

# Check SendGrid config
aws logs tail /ecs/dev-vector --since 10m --region us-east-1 | Select-String "SendGrid|ApiKey"

# Download logs
aws logs tail /ecs/dev-vector --since 24h --region us-east-1 > logs.txt
```

