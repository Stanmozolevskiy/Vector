# Email Verification Troubleshooting Guide

## Issue
Verification emails are not being sent after user registration. No activity in SendGrid dashboard.

## Fixes Applied

### 1. Fixed docker-compose.yml Indentation
- **Problem**: Incorrect indentation on line 53 caused environment variable parsing issues
- **Fix**: Corrected indentation for `SendGrid__ApiKey` environment variable

### 2. Created .env File
- **Location**: `docker/.env`
- **Contents**:
  ```
  SENDGRID_API_KEY=your_sendgrid_api_key_here
  SENDGRID_FROM_EMAIL=stanmozolevskiy90@gmail.com
  SENDGRID_FROM_NAME=Vector
  ```
- **Note**: Docker Compose automatically reads `.env` files from the same directory

### 3. Enhanced Logging
- Added detailed logging in `AuthService` for email sending
- Added logging in `EmailService` for SendGrid initialization
- All email-related logs use `LogWarning` level so they're always visible

## How Email Sending Works

1. **User Registration**: When a user registers, `AuthService.RegisterUserAsync` is called
2. **Email Service**: `EmailService` is a scoped service, so it's created when first used
3. **Background Task**: Email sending happens in a `Task.Run` (fire-and-forget) so registration doesn't wait
4. **SendGrid Client**: The `SendGridClient` is initialized in the `EmailService` constructor

## Verification Steps

### Step 1: Check Environment Variables
```powershell
cd docker
docker exec vector-backend env | Select-String -Pattern "SendGrid"
```

You should see:
- `SendGrid__ApiKey=your_sendgrid_api_key_here`
- `SendGrid__FromEmail=stanmozolevskiy90@gmail.com`
- `SendGrid__FromName=Vector`

### Step 2: Register a Test Account
1. Go to: http://localhost:3000/register
2. Fill in the registration form
3. Submit

### Step 3: Check Backend Logs
```powershell
docker logs vector-backend -f
```

Look for:
- `=== SendGrid Configuration Debug ===`
- `SendGrid email service initialized successfully!`
- `=== STARTING EMAIL SEND TASK ===`
- `Sending email via SendGrid to {email}`
- `SendGrid response status code: {code}`
- `Verification email sent successfully`

### Step 4: Check SendGrid Dashboard
1. Log into SendGrid dashboard
2. Go to Activity Feed
3. Look for email sending attempts

## Common Issues

### Issue: "SendGrid API Key is not configured"
**Solution**: 
- Ensure `.env` file exists in `docker/` directory
- Restart backend: `docker-compose restart backend`
- Verify API key in container: `docker exec vector-backend env | grep SendGrid__ApiKey`

### Issue: No logs appear
**Solution**:
- EmailService only initializes when first used (during registration)
- Try registering a new account
- Check logs immediately after registration

### Issue: API Key Invalid
**Solution**:
- Verify API key is correct in SendGrid dashboard
- Check if API key has proper permissions (Mail Send)
- Ensure API key hasn't been revoked

### Issue: Email sent but not received
**Solution**:
- Check spam/junk folder
- Verify sender email is verified in SendGrid
- Check SendGrid Activity Feed for delivery status

## Testing Email Sending

To test if emails are working:

1. **Register a new account**
2. **Watch logs in real-time**:
   ```powershell
   docker logs vector-backend -f
   ```
3. **Look for these log messages**:
   - SendGrid Configuration Debug
   - Email service initialized
   - Email sending attempt
   - SendGrid response

## Manual API Key Setup

If `.env` file doesn't work, set environment variables manually:

```powershell
# In PowerShell (current session only)
$env:SENDGRID_API_KEY='your_sendgrid_api_key_here'
$env:SENDGRID_FROM_EMAIL='stanmozolevskiy90@gmail.com'
$env:SENDGRID_FROM_NAME='Vector'

# Then restart
cd docker
docker-compose restart backend
```

## Current Status

✅ **Fixed Issues:**
- docker-compose.yml indentation
- Environment variable configuration
- Enhanced logging for debugging

✅ **Configuration:**
- SendGrid API Key: Set in container
- From Email: stanmozolevskiy90@gmail.com
- From Name: Vector

🧪 **Next Step:**
- Register a new account and monitor logs
- Check SendGrid dashboard for activity

