# SendGrid Setup Guide

## Overview

This guide will help you set up SendGrid for email functionality in the Vector application.

---

## Step 1: Create SendGrid Account

1. Go to: https://sendgrid.com
2. Click **"Start for Free"** or **"Sign Up"**
3. Fill in your account details:
   - Email address
   - Password
   - Company name (optional)
4. Verify your email address
5. Complete the account setup

---

## Step 2: Verify Your Sender Identity

SendGrid requires you to verify a sender email address before you can send emails.

### Option A: Single Sender Verification (Recommended for Development)

1. Go to **Settings** → **Sender Authentication**
2. Click **"Verify a Single Sender"**
3. Fill in the form:
   - **From Email Address**: `noreply@yourdomain.com` (or use your personal email for testing)
   - **From Name**: `Vector`
   - **Reply To**: (same as From Email)
   - **Company Address**: Your address
   - **Company Website**: Your website (can be placeholder for testing)
4. Click **"Create"**
5. **Check your email** and click the verification link
6. ✅ Once verified, you can use this email address to send emails

### Option B: Domain Authentication (Recommended for Production)

1. Go to **Settings** → **Sender Authentication**
2. Click **"Authenticate Your Domain"**
3. Follow the DNS configuration steps
4. Add the required DNS records to your domain
5. Wait for verification (can take up to 48 hours)

**For Development:** Use Option A (Single Sender Verification)

---

## Step 3: Create API Key

1. Go to **Settings** → **API Keys**
2. Click **"Create API Key"**
3. **API Key Name**: `Vector Production` (or `Vector Development`)
4. **API Key Permissions**: Select **"Full Access"** (or **"Restricted Access"** with only **"Mail Send"** permission)
5. Click **"Create & View"**
6. ⚠️ **IMPORTANT**: Copy the API key immediately - you won't be able to see it again!
   - The API key will look like: `SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`
7. Save it securely (password manager, secure note, etc.)

---

## Step 4: Get SendGrid Configuration Values

You'll need these values for your application configuration:

### Required Information:

1. **API Key**: 
   - Format: `SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`
   - Location: Settings → API Keys (you need to create it)
   - ⚠️ Keep this secret - never commit it to Git!

2. **From Email**:
   - The verified sender email address
   - Example: `noreply@yourdomain.com` or `your-email@gmail.com` (if using single sender)
   - Location: Settings → Sender Authentication → Verified Senders

3. **From Name**:
   - Display name for emails
   - Example: `Vector` or `Vector Platform`
   - This is what you set when creating the sender

---

## Step 5: Configure Application

### For Local Development (`appsettings.Development.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=vector_db;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "SendGrid": {
    "ApiKey": "SG.your_actual_api_key_here",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Vector"
  },
  "Frontend": {
    "Url": "http://localhost:3000"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### For AWS Deployment (Environment Variables):

Add these to your ECS Task Definition or GitHub Secrets:

- `SendGrid__ApiKey`: Your SendGrid API key
- `SendGrid__FromEmail`: Your verified sender email
- `SendGrid__FromName`: Your sender display name

**Note:** In environment variables, use double underscore `__` instead of `:` for nested configuration.

---

## Step 6: Test Email Sending

### Test Locally:

1. Update `appsettings.Development.json` with your SendGrid credentials
2. Restart the backend
3. Register a new user via Swagger or API
4. Check the email inbox of the registered user
5. You should receive a verification email

### Verify in SendGrid Dashboard:

1. Go to **Activity** → **Email Activity**
2. You should see sent emails listed
3. Check delivery status (Delivered, Bounced, etc.)

---

## SendGrid Free Tier Limits

- **100 emails per day** (free tier)
- **Unlimited contacts**
- **Email API access**
- **Email activity dashboard**

**Upgrade Options:**
- **Essentials**: $19.95/month - 50,000 emails/month
- **Pro**: $89.95/month - 100,000 emails/month
- **Premier**: Custom pricing - Higher limits

---

## Security Best Practices

1. **Never commit API keys to Git**
   - Use `appsettings.Development.json` (already in `.gitignore`)
   - Use environment variables for production
   - Use GitHub Secrets for CI/CD

2. **Use Restricted API Keys**
   - Only grant "Mail Send" permission
   - Don't use "Full Access" unless necessary

3. **Rotate API Keys Regularly**
   - Create new keys periodically
   - Revoke old keys when no longer needed

4. **Monitor Email Activity**
   - Check SendGrid dashboard regularly
   - Watch for unusual activity
   - Set up alerts for bounces/spam reports

---

## Troubleshooting

### Emails Not Sending

1. **Check API Key**:
   - Verify the API key is correct
   - Ensure it has "Mail Send" permission
   - Check if the key is active

2. **Check Sender Verification**:
   - Ensure sender email is verified
   - Check verification status in SendGrid dashboard

3. **Check Application Logs**:
   - Look for SendGrid errors in backend logs
   - Check for authentication failures

4. **Check SendGrid Activity**:
   - Go to Activity → Email Activity
   - Look for failed sends
   - Check error messages

### Common Errors

**Error: "The provided authorization grant is invalid"**
- API key is incorrect or expired
- Solution: Create a new API key

**Error: "The from address does not match a verified Sender Identity"**
- Sender email is not verified
- Solution: Verify the sender email in SendGrid

**Error: "Daily sending quota exceeded"**
- You've hit the 100 emails/day limit (free tier)
- Solution: Wait until next day or upgrade plan

---

## Next Steps

1. ✅ Create SendGrid account
2. ✅ Verify sender email
3. ✅ Create API key
4. ⏳ Update `appsettings.Development.json` with credentials
5. ⏳ Test email sending locally
6. ⏳ Configure for AWS deployment (environment variables)

---

## Quick Reference

**SendGrid Dashboard**: https://app.sendgrid.com

**API Key Location**: Settings → API Keys

**Sender Verification**: Settings → Sender Authentication

**Email Activity**: Activity → Email Activity

**Documentation**: https://docs.sendgrid.com

---

**Remember:** Never commit your SendGrid API key to version control!

