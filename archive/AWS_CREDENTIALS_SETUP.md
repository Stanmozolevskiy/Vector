# AWS Credentials Setup for Profile Image Upload

**Date:** December 3, 2025

---

## Overview

Profile image upload functionality requires AWS credentials to access S3 bucket. This guide explains how to set up credentials for:
1. **Local Docker Development**
2. **AWS ECS Deployment**

---

## Local Docker Development Setup

### Step 1: Create `.env` File

Create a file named `.env` in the `docker/` directory:

```bash
cd docker
```

Create `.env` file with the following content:

```env
# AWS Credentials for S3 file uploads
# Get these from AWS IAM Console
AWS_ACCESS_KEY_ID=AKIA...your_key_here
AWS_SECRET_ACCESS_KEY=your_secret_key_here
AWS_REGION=us-east-1
AWS_S3_BUCKET_NAME=dev-vector-user-uploads

# SendGrid API Key (optional for local testing)
SENDGRID_API_KEY=SG....your_key_here

# JWT Secret (optional, has default)
JWT_SECRET=your-super-secret-key-change-in-production
```

### Step 2: Get AWS Credentials

**Option A: Create IAM User (Recommended for Local Dev)**

1. Go to AWS Console → IAM → Users
2. Click "Create user"
3. User name: `vector-local-dev`
4. Click "Next"
5. Select "Attach policies directly"
6. Click "Create policy" → JSON:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:GetObject",
        "s3:PutObject",
        "s3:DeleteObject"
      ],
      "Resource": "arn:aws:s3:::dev-vector-user-uploads/*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "s3:ListBucket"
      ],
      "Resource": "arn:aws:s3:::dev-vector-user-uploads"
    }
  ]
}
```

7. Name: `VectorS3ProfilePicturesPolicy`
8. Create policy
9. Attach to user
10. Go to "Security credentials"
11. Click "Create access key"
12. Select "Local code"
13. Copy **Access Key ID** and **Secret Access Key**
14. Add to `.env` file

**Option B: Use AWS CLI Profile**

If you already have AWS CLI configured:

```bash
aws configure list
```

Get credentials:
```bash
cat ~/.aws/credentials
```

Copy `aws_access_key_id` and `aws_secret_access_key` to `.env` file.

### Step 3: Restart Docker Containers

```bash
cd docker
docker-compose down
docker-compose up -d
```

### Step 4: Verify

Check backend logs:
```bash
docker logs vector-backend --tail 20
```

Should NOT see:
```
The "AWS_ACCESS_KEY_ID" variable is not set
The "AWS_SECRET_ACCESS_KEY" variable is not set
```

### Step 5: Test Upload

1. Go to: http://localhost:3000/profile
2. Click "Upload New Picture"
3. Select an image
4. Click "Save Changes"
5. Should see success message
6. Image should appear in profile

Verify in S3:
```bash
aws s3 ls s3://dev-vector-user-uploads/profile-pictures/
```

---

## AWS ECS Deployment Setup

For AWS deployment, **DO NOT use access keys**. Use IAM roles instead.

### Step 1: Verify ECS Task Role

The ECS task definition should already have an IAM role with S3 permissions.

Check Terraform configuration:

```bash
cd infrastructure/terraform/modules/ecs
cat task_definition.tf | grep -A 10 "task_role_arn"
```

Should show something like:
```hcl
task_role_arn = aws_iam_role.ecs_task_role.arn
```

### Step 2: Verify IAM Role Policy

```bash
cd infrastructure/terraform/modules/ecs
cat iam.tf | grep -A 20 "s3:PutObject"
```

Should include:
```hcl
{
  "Effect": "Allow",
  "Action": [
    "s3:GetObject",
    "s3:PutObject",
    "s3:DeleteObject"
  ],
  "Resource": "arn:aws:s3:::${var.s3_bucket_name}/*"
}
```

### Step 3: Add Environment Variables to ECS Task Definition

Update `infrastructure/terraform/modules/ecs/task_definition.tf`:

Find the `environment` section and add:

```json
{
  "name": "AWS__Region",
  "value": "us-east-1"
},
{
  "name": "AWS__S3__BucketName",
  "value": "${var.s3_bucket_name}"
}
```

### Step 4: Apply Terraform

```bash
cd infrastructure/terraform
terraform plan
terraform apply
```

### Step 5: Redeploy Application

```bash
git add -A
git commit -m "Add S3 environment variables to ECS"
git push origin develop
```

GitHub Actions will deploy to ECS automatically.

### Step 6: Test on AWS Dev

1. Navigate to dev frontend URL (e.g., `https://dev-frontend-url`)
2. Login
3. Go to Profile
4. Upload profile picture
5. Verify it works

Check ECS logs:
```bash
aws logs tail /ecs/dev-vector --follow --region us-east-1 | grep -i "s3\|upload"
```

Should see:
```
Profile picture uploaded successfully for user {guid}: https://dev-vector-user-uploads.s3.us-east-1.amazonaws.com/profile-pictures/...
```

---

## Security Best Practices

### DO:
- ✅ Use IAM roles for ECS (no access keys)
- ✅ Use least privilege permissions (only S3 access)
- ✅ Never commit `.env` file to git
- ✅ Rotate access keys regularly (if using for local dev)
- ✅ Use separate IAM users for dev and production

### DON'T:
- ❌ Never commit AWS credentials to git
- ❌ Never use root account credentials
- ❌ Never give S3 FullAccess (only specific bucket)
- ❌ Never share credentials between team members
- ❌ Never use access keys in production (use IAM roles)

---

## Troubleshooting

### Issue: "Access Denied" Error

**Possible causes:**
1. AWS credentials not set in `.env` file
2. IAM user doesn't have S3 permissions
3. S3 bucket name is incorrect
4. Region mismatch

**Solution:**
1. Verify `.env` file exists and has correct credentials
2. Check IAM user policy includes S3 permissions
3. Verify bucket name matches: `dev-vector-user-uploads`
4. Check region matches: `us-east-1`

### Issue: "Unable to load credentials from any providers"

**Solution:**
For local Docker:
```bash
# Stop containers
docker-compose down

# Verify .env file exists
cat .env

# Should show AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY

# Restart containers
docker-compose up -d
```

For AWS ECS:
- ECS task role should be attached automatically
- No credentials needed in environment variables
- Check CloudWatch logs for role assumption errors

### Issue: "Bucket does not exist"

**Solution:**
```bash
# Check if bucket exists
aws s3 ls | grep vector

# Should show: dev-vector-user-uploads

# If not, create via Terraform
cd infrastructure/terraform
terraform apply
```

### Issue: Credentials work in AWS CLI but not in Docker

**Solution:**
Docker doesn't use `~/.aws/credentials` automatically. You must:
1. Create `.env` file in `docker/` directory
2. Copy credentials explicitly to `.env`
3. Restart containers

---

## Environment Variable Reference

### Local Docker (.env file):
```env
AWS_ACCESS_KEY_ID=AKIA...
AWS_SECRET_ACCESS_KEY=...
AWS_REGION=us-east-1
AWS_S3_BUCKET_NAME=dev-vector-user-uploads
```

### AWS ECS (Task Definition):
```json
{
  "name": "AWS__Region",
  "value": "us-east-1"
},
{
  "name": "AWS__S3__BucketName",
  "value": "dev-vector-user-uploads"
}
```

**Note:** ECS doesn't need `AWS_ACCESS_KEY_ID` or `AWS_SECRET_ACCESS_KEY` because it uses IAM task role.

---

## Next Steps

1. ✅ Create `.env` file for local Docker
2. ✅ Test profile picture upload locally
3. ✅ Update ECS task definition with S3 environment variables
4. ✅ Deploy to AWS dev
5. ✅ Test on AWS dev environment

---

**Created:** December 3, 2025  
**Status:** ✅ GUIDE COMPLETE

