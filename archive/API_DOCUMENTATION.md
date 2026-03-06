# Vector API Documentation

**Version:** 1.0  
**Base URL:** `http://localhost:5000/api` (Development)  
**Base URL:** `https://api.vector.com/api` (Production)

## Table of Contents

1. [Authentication](#authentication)
2. [User Management](#user-management)
3. [Subscription Management](#subscription-management)
4. [Coach Applications](#coach-applications)
5. [Admin Operations](#admin-operations)
6. [Health Check](#health-check)
7. [Error Responses](#error-responses)

---

## Authentication

All authenticated endpoints require a JWT token in the Authorization header:
```
Authorization: Bearer <access_token>
```

### Register User

**Endpoint:** `POST /api/auth/register`

**Description:** Register a new user account. An email verification link will be sent to the provided email address.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "student",
  "emailVerified": false,
  "createdAt": "2025-12-06T20:00:00Z"
}
```

**Error Responses:**
- `400 Bad Request` - Invalid input or email already exists
- `500 Internal Server Error` - Server error

---

### Verify Email

**Endpoint:** `GET /api/auth/verify-email?token={verification_token}`

**Description:** Verify user's email address using the token sent via email.

**Query Parameters:**
- `token` (required) - Email verification token

**Response:** `200 OK`
```json
{
  "message": "Email verified successfully. You can now log in."
}
```

**Error Responses:**
- `400 Bad Request` - Invalid or expired token

---

### Login

**Endpoint:** `POST /api/auth/login`

**Description:** Authenticate user and receive access token. Email must be verified before login.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response:** `200 OK`
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_string",
  "user": {
    "id": "guid",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "student"
  }
}
```

**Error Responses:**
- `401 Unauthorized` - Invalid credentials
- `400 Bad Request` - Email not verified

---

### Forgot Password

**Endpoint:** `POST /api/auth/forgot-password`

**Description:** Request a password reset email. Always returns success to prevent email enumeration.

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

**Response:** `200 OK`
```json
{
  "message": "If an account exists with this email, a password reset link has been sent."
}
```

---

### Reset Password

**Endpoint:** `POST /api/auth/reset-password`

**Description:** Reset password using the token from the password reset email.

**Request Body:**
```json
{
  "email": "user@example.com",
  "token": "reset_token",
  "newPassword": "NewSecurePassword123!"
}
```

**Response:** `200 OK`
```json
{
  "message": "Password reset successfully"
}
```

**Error Responses:**
- `400 Bad Request` - Invalid or expired token

---

### Logout

**Endpoint:** `POST /api/auth/logout`

**Description:** Logout user and revoke refresh tokens. Requires authentication.

**Headers:**
- `Authorization: Bearer <access_token>`

**Response:** `200 OK`
```json
{
  "message": "Logged out successfully"
}
```

---

## User Management

All user endpoints require authentication.

### Get Current User

**Endpoint:** `GET /api/users/me`

**Description:** Get the authenticated user's profile information.

**Response:** `200 OK`
```json
{
  "id": "guid",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "bio": "Software engineer",
  "phoneNumber": "+1234567890",
  "location": "New York, NY",
  "role": "student",
  "profilePictureUrl": "https://s3.amazonaws.com/...",
  "emailVerified": true,
  "createdAt": "2025-12-06T20:00:00Z"
}
```

---

### Update Profile

**Endpoint:** `PUT /api/users/me`

**Description:** Update the authenticated user's profile information.

**Request Body:**
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "bio": "Updated bio",
  "phoneNumber": "+1234567890",
  "location": "San Francisco, CA"
}
```

**Response:** `200 OK` - Returns updated user object

**Error Responses:**
- `404 Not Found` - User not found
- `500 Internal Server Error` - Server error

---

### Change Password

**Endpoint:** `PUT /api/users/me/password`

**Description:** Change the authenticated user's password.

**Request Body:**
```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword123!"
}
```

**Response:** `200 OK`
```json
{
  "message": "Password changed successfully"
}
```

**Error Responses:**
- `400 Bad Request` - Invalid current password
- `401 Unauthorized` - Not authenticated

---

### Upload Profile Picture

**Endpoint:** `POST /api/users/me/profile-picture`

**Description:** Upload a profile picture. Accepts JPEG, PNG, GIF, or WebP images up to 5MB.

**Request:** `multipart/form-data`
- `file` - Image file (required)

**Response:** `200 OK`
```json
{
  "profilePictureUrl": "https://s3.amazonaws.com/bucket/profile-pictures/guid.jpg"
}
```

**Error Responses:**
- `400 Bad Request` - Invalid file type or size exceeded
- `500 Internal Server Error` - Upload failed

---

### Delete Profile Picture

**Endpoint:** `DELETE /api/users/me/profile-picture`

**Description:** Delete the authenticated user's profile picture.

**Response:** `200 OK`
```json
{
  "message": "Profile picture deleted successfully"
}
```

---

### Delete Account

**Endpoint:** `DELETE /api/users/me`

**Description:** Permanently delete the authenticated user's account.

**Response:** `200 OK`
```json
{
  "message": "Account deleted successfully"
}
```

---

## Subscription Management

### Get Available Plans

**Endpoint:** `GET /api/subscriptions/plans`

**Description:** Get all available subscription plans. No authentication required.

**Response:** `200 OK`
```json
[
  {
    "id": "free",
    "name": "Free Plan",
    "description": "Get started with basic features",
    "price": 0,
    "currency": "USD",
    "billingPeriod": "free",
    "features": ["Limited course access", "Basic features"],
    "isPopular": false
  },
  {
    "id": "monthly",
    "name": "Monthly Plan",
    "description": "Perfect for trying out Vector",
    "price": 29.99,
    "currency": "USD",
    "billingPeriod": "monthly",
    "features": ["Access to all courses", "Unlimited mock interviews"],
    "isPopular": false
  },
  {
    "id": "annual",
    "name": "Annual Plan",
    "description": "Best value! Save 2 months",
    "price": 299.99,
    "currency": "USD",
    "billingPeriod": "annual",
    "features": ["Access to all courses", "2 months free"],
    "isPopular": true
  }
]
```

---

### Get Current Subscription

**Endpoint:** `GET /api/subscriptions/me`

**Description:** Get the authenticated user's current subscription. Returns free plan if no subscription exists.

**Response:** `200 OK`
```json
{
  "id": "guid",
  "planType": "monthly",
  "status": "active",
  "currentPeriodStart": "2025-12-06T20:00:00Z",
  "currentPeriodEnd": "2026-01-06T20:00:00Z",
  "price": 29.99,
  "currency": "USD",
  "createdAt": "2025-12-06T20:00:00Z",
  "plan": {
    "id": "monthly",
    "name": "Monthly Plan",
    "price": 29.99
  }
}
```

---

### Update Subscription

**Endpoint:** `PUT /api/subscriptions/update`

**Description:** Update user's subscription plan. Cancels existing subscription and creates a new one.

**Request Body:**
```json
{
  "planId": "annual"
}
```

**Response:** `200 OK` - Returns new subscription object

**Error Responses:**
- `400 Bad Request` - Invalid plan ID
- `401 Unauthorized` - Not authenticated

---

### Cancel Subscription

**Endpoint:** `PUT /api/subscriptions/cancel`

**Description:** Cancel the user's active subscription and move them to the free plan.

**Response:** `200 OK`
```json
{
  "message": "Subscription cancelled successfully. You have been moved to the free plan."
}
```

**Error Responses:**
- `400 Bad Request` - No active subscription to cancel

---

### Get Invoices

**Endpoint:** `GET /api/subscriptions/invoices`

**Description:** Get billing history/invoices for the authenticated user.

**Response:** `200 OK`
```json
[]
```

*Note: Currently returns empty array. Will be implemented with Stripe integration.*

---

## Coach Applications

### Submit Application

**Endpoint:** `POST /api/coach/apply`

**Description:** Submit a coach application. Requires authentication.

**Request Body:**
```json
{
  "bio": "Experienced software engineer",
  "experience": "10 years in tech",
  "specializations": ["System Design", "Algorithms"],
  "portfolioUrl": "https://portfolio.com",
  "linkedInUrl": "https://linkedin.com/in/user"
}
```

**Request:** `multipart/form-data` (for image upload)
- `application` - JSON string with application data
- `image` - Portfolio image file (optional)

**Response:** `201 Created`
```json
{
  "id": "guid",
  "status": "pending",
  "submittedAt": "2025-12-06T20:00:00Z"
}
```

---

### Get My Application

**Endpoint:** `GET /api/coach/my-application`

**Description:** Get the authenticated user's coach application status.

**Response:** `200 OK` - Returns application object or `404 Not Found` if no application exists

---

## Admin Operations

All admin endpoints require `admin` role.

### Get All Users

**Endpoint:** `GET /api/admin/users`

**Description:** Get paginated list of all users.

**Query Parameters:**
- `page` (optional, default: 1) - Page number
- `pageSize` (optional, default: 10) - Items per page

**Response:** `200 OK`
```json
{
  "users": [...],
  "totalCount": 100,
  "page": 1,
  "pageSize": 10
}
```

---

### Update User Role

**Endpoint:** `PUT /api/admin/users/{userId}/role`

**Description:** Update a user's role.

**Request Body:**
```json
{
  "role": "coach"
}
```

**Response:** `200 OK` - Returns updated user

---

### Delete User

**Endpoint:** `DELETE /api/admin/users/{userId}`

**Description:** Delete a user account.

**Response:** `200 OK`
```json
{
  "message": "User deleted successfully"
}
```

---

### Get Pending Coach Applications

**Endpoint:** `GET /api/admin/coach-applications/pending`

**Description:** Get all pending coach applications.

**Response:** `200 OK` - Returns array of applications

---

### Review Coach Application

**Endpoint:** `POST /api/admin/coach-applications/{applicationId}/review`

**Description:** Approve or reject a coach application.

**Request Body:**
```json
{
  "status": "approved",
  "notes": "Application approved"
}
```

**Response:** `200 OK` - Returns updated application

---

## Health Check

### Basic Health Check

**Endpoint:** `GET /api/health`

**Description:** Check if the API is running.

**Response:** `200 OK`
```json
{
  "status": "healthy",
  "timestamp": "2025-12-06T20:00:00Z"
}
```

---

### Detailed Health Check

**Endpoint:** `GET /api/health/detailed`

**Description:** Get detailed health status including database and Redis connectivity.

**Response:** `200 OK`
```json
{
  "status": "healthy",
  "timestamp": "2025-12-06T20:00:00Z",
  "database": "connected",
  "redis": "connected"
}
```

---

## Error Responses

All error responses follow a consistent format:

```json
{
  "error": "Human-readable error message",
  "errorCode": "ERROR_CODE",
  "details": "Optional additional details",
  "validationErrors": {
    "fieldName": ["Error message 1", "Error message 2"]
  }
}
```

### Common Error Codes

- `INVALID_TOKEN` - Invalid or expired authentication token
- `USER_NOT_FOUND` - User not found
- `VALIDATION_ERROR` - Request validation failed
- `FILE_TOO_LARGE` - Uploaded file exceeds size limit
- `INVALID_FILE_TYPE` - Invalid file type
- `PLAN_NOT_FOUND` - Subscription plan not found
- `CANCEL_FAILED` - Unable to cancel subscription
- `UPLOAD_ERROR` - File upload failed

### HTTP Status Codes

- `200 OK` - Request successful
- `201 Created` - Resource created successfully
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Authentication required or invalid
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

---

## Rate Limiting

Currently, rate limiting is not implemented at the API level. Consider implementing rate limiting for:
- Authentication endpoints (login, register, password reset)
- File upload endpoints
- API endpoints in general

---

## Authentication Flow

1. User registers → Receives email verification link
2. User verifies email → Can now log in
3. User logs in → Receives access token and refresh token
4. Access token expires (15 minutes) → Use refresh token to get new access token
5. Refresh token expires (7 days) → User must log in again

---

## Swagger Documentation

Interactive API documentation is available at:
- Development: `http://localhost:5000/swagger`
- Staging: `https://staging-api.vector.com/swagger`

Swagger UI includes:
- All API endpoints
- Request/response schemas
- Authentication testing
- Try-it-out functionality

---

**Last Updated:** December 6, 2025

