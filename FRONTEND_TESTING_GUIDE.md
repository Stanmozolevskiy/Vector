# Frontend Testing Guide

## Overview

This guide shows how to test the frontend separately and verify it calls the backend API and pulls data from the database.

---

## Testing Frontend on AWS

### 1. Access Frontend

```powershell
# Open frontend in browser
$frontendUrl = "http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com"
Start-Process $frontendUrl
```

### 2. Test Frontend Health

```powershell
# Frontend has a health endpoint
Invoke-WebRequest -Uri "$frontendUrl/health" -UseBasicParsing
```

**Expected Response:** `healthy`

---

## Testing Frontend-Backend Communication

### 1. Open Browser Developer Tools

1. Open frontend in browser: `http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com`
2. Press `F12` to open Developer Tools
3. Go to **Network** tab
4. Filter by `XHR` or `Fetch`

### 2. Test API Calls

The frontend should make API calls to the backend. Currently, the frontend will attempt to:
- Check authentication status on load
- Call `/api/users/me` if a token exists

**To Test:**

1. **Check Console for Errors:**
   - Open **Console** tab in Developer Tools
   - Look for API call errors
   - Verify API URL is correct

2. **Monitor Network Requests:**
   - Watch for requests to `/api/*` endpoints
   - Check request/response status codes
   - Verify CORS headers are present

### 3. Test with Authentication

Currently, controllers are not implemented, so API calls will fail. However, you can verify:

1. **Frontend loads correctly**
2. **API calls are being made to correct URL**
3. **Error handling works**

---

## Testing Frontend Locally

### Prerequisites

1. **Backend must be running** (locally or on AWS)
2. **Update frontend API URL** if testing against AWS backend

### Run Frontend Locally

```powershell
cd frontend

# Create .env file with API URL
# For local backend:
echo "VITE_API_URL=http://localhost:5000/api" > .env

# For AWS backend:
echo "VITE_API_URL=http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/api" > .env

# Install dependencies
npm install

# Start development server
npm run dev
```

**Frontend will be available at:** `http://localhost:5173`

### Test Local Frontend

1. Open: `http://localhost:5173`
2. Open Developer Tools (F12)
3. Check **Console** and **Network** tabs
4. Verify API calls are made to correct URL

---

## Testing Data Flow: Database → Backend → Frontend

### Current Status

**Controllers are not yet implemented**, so full data flow testing requires implementation. However, you can verify:

### 1. Database Connection (Backend)

```powershell
# Check backend logs for database connection
aws logs tail /ecs/dev-vector --since 10m --region us-east-1 --filter-pattern "database" --format short
```

**Look for:**
- Migration success messages
- Database connection established

### 2. Backend API Availability

```powershell
# Test backend is responding
Invoke-WebRequest -Uri "http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/api/health" -UseBasicParsing
```

### 3. Frontend-Backend Communication

1. Open frontend in browser
2. Open Developer Tools → Network tab
3. Check for API requests
4. Verify requests go to: `http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/api/*`

---

## Testing with Browser DevTools

### Step-by-Step Test

1. **Open Frontend:**
   ```
   http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com
   ```

2. **Open Developer Tools (F12)**

3. **Go to Console Tab:**
   - Check for JavaScript errors
   - Look for API call logs (if logging is enabled)

4. **Go to Network Tab:**
   - Filter by `XHR` or `Fetch`
   - Look for requests to `/api/*`
   - Check request URLs, status codes, and responses

5. **Go to Application Tab:**
   - Check `Local Storage` for tokens
   - Verify `accessToken` and `refreshToken` if logged in

---

## Testing Authentication Flow (When Implemented)

Once controllers are implemented, you can test:

### 1. Registration Flow

1. Navigate to `/register`
2. Fill out registration form
3. Submit form
4. **Verify:**
   - Frontend calls `POST /api/auth/register`
   - Request includes user data
   - Response is handled correctly
   - User is redirected appropriately

### 2. Login Flow

1. Navigate to `/login`
2. Enter credentials
3. Submit form
4. **Verify:**
   - Frontend calls `POST /api/auth/login`
   - Token is stored in localStorage
   - User is redirected to dashboard
   - Subsequent API calls include token

### 3. Data Retrieval Flow

1. After login, frontend should call `GET /api/users/me`
2. **Verify:**
   - Request includes `Authorization: Bearer <token>` header
   - Backend returns user data from database
   - Frontend displays user information

---

## Testing CORS

### Verify CORS Headers

```powershell
# Test CORS preflight
$headers = @{
    'Origin' = 'http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com'
    'Access-Control-Request-Method' = 'GET'
}
Invoke-WebRequest -Uri "http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/api/health" -Method OPTIONS -Headers $headers -UseBasicParsing
```

**Check for:**
- `Access-Control-Allow-Origin` header
- `Access-Control-Allow-Methods` header
- `Access-Control-Allow-Headers` header

---

## Troubleshooting

### Frontend Not Loading

1. **Check ECS Service:**
   ```powershell
   aws ecs describe-services --cluster dev-vector-cluster --services dev-vector-frontend-service --region us-east-1
   ```

2. **Check ALB Target Health:**
   ```powershell
   aws elbv2 describe-target-health --target-group-arn "arn:aws:elasticloadbalancing:us-east-1:324795474468:targetgroup/dev-vector-frontend-tg/3500c229f0d691d7" --region us-east-1
   ```

### API Calls Failing

1. **Check CORS Configuration:**
   - Verify `Frontend:Url` in backend config matches frontend URL
   - Check CORS headers in response

2. **Check API URL:**
   - Verify `VITE_API_URL` in frontend build
   - Check browser console for incorrect API URLs

3. **Check Network Tab:**
   - Look at failed request details
   - Check status codes (404, 500, CORS errors)

### Data Not Loading

1. **Check Backend Logs:**
   ```powershell
   aws logs tail /ecs/dev-vector --since 10m --region us-east-1 --filter-pattern "error" --format short
   ```

2. **Check Database Connection:**
   - Verify backend can connect to database
   - Check migration status

---

## Testing Checklist

- [ ] Frontend loads without errors
- [ ] Frontend health endpoint works (`/health`)
- [ ] API calls are made to correct backend URL
- [ ] CORS headers are present
- [ ] Error handling works for failed API calls
- [ ] Authentication flow works (when implemented)
- [ ] Data flows from database → backend → frontend (when implemented)

---

## Next Steps

1. **Implement Controllers:**
   - AuthController (register, login, logout)
   - UserController (get user, update user)

2. **Test Full Flow:**
   - Register user → Database stores user
   - Login → Backend returns token
   - Get user data → Backend queries database → Frontend displays data

---

**Frontend URL:** `http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/`
**Backend API URL:** `http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/api`

