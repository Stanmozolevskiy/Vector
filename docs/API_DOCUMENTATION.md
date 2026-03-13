# Vector API Documentation

**Version:** 2.0 (Stage 2)  
**Base URL:** `http://localhost:5000/api` (Development)  
**Swagger UI:** `http://localhost:5000/swagger`

## Table of Contents

1. [Authentication](#authentication)
2. [User Management](#user-management)
3. [Questions](#questions)
4. [Code Execution](#code-execution)
5. [Solutions](#solutions)
6. [Peer Interviews](#peer-interviews)
7. [Analytics](#analytics)
8. [Coins & Leaderboard](#coins--leaderboard)
9. [Site Settings](#site-settings)
10. [Admin](#admin)

---

## Authentication

**Base:** `/api/auth`

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/register` | Register new user |
| GET | `/verify-email?token=` | Verify email |
| POST | `/login` | Login, returns accessToken + refreshToken |
| POST | `/refresh` | Refresh tokens |
| POST | `/forgot-password` | Request password reset |
| POST | `/reset-password` | Reset password |
| POST | `/logout` | Logout |

---

## User Management

**Base:** `/api/users` — Requires Auth

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/me` | Get current user |
| PUT | `/me` | Update profile |
| PUT | `/me/password` | Change password |
| POST | `/me/profile-picture` | Upload profile picture (multipart/form-data) |
| DELETE | `/me/profile-picture` | Delete profile picture |
| DELETE | `/me` | Delete account |

---

## Questions

**Base:** `/api/question` or `/api/questions`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | List questions (filters: category, difficulty, search) |
| GET | `/{id}` | Get question details |
| POST | `/` | Create question (admin/coach) |
| PUT | `/{id}` | Update question (admin/coach) |
| DELETE | `/{id}` | Delete question (admin) |
| GET | `/{id}/test-cases` | Get test cases |
| POST | `/{id}/test-cases` | Add test case |
| GET | `/{id}/solutions` | Get solutions |
| POST | `/{id}/solutions` | Add solution |
| POST | `/{id}/bookmark` | Add bookmark |
| DELETE | `/{id}/bookmark` | Remove bookmark |
| GET | `/bookmarks` | Get bookmarked questions |

---

## Code Execution

**Base:** `/api/codeexecution` — Requires Auth

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/execute` | Execute code (body: code, language, input) |
| POST | `/validate/{questionId}` | Validate solution against test cases |
| GET | `/languages` | Get supported languages |

**Supported languages:** Python, JavaScript, Java, C++, C#, Go

---

## Solutions

**Base:** `/api/solutions` — Requires Auth

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/` | Submit solution |
| GET | `/me` | Get user's solutions |
| GET | `/{id}` | Get solution details |
| GET | `/question/{questionId}` | Get solutions for question |
| GET | `/statistics` | Get user statistics |
| GET | `/question/{questionId}/solved` | Check if user solved question |

---

## Peer Interviews

**Base:** `/api/peer-interviews` — Requires Auth

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/find-match` | Find peer match |
| POST | `/sessions` | Create session |
| GET | `/sessions/me` | Get user's sessions |
| GET | `/sessions/{id}` | Get session details |
| PUT | `/sessions/{id}/status` | Update status |
| PUT | `/sessions/{id}/cancel` | Cancel session |
| PUT | `/sessions/{id}/start` | Start interview |
| PUT | `/match-preferences` | Update match preferences |
| GET | `/match-preferences` | Get match preferences |
| POST | `/sessions/{id}/feedback` | Submit feedback |
| GET | `/sessions/{id}/feedback` | Get feedback |

**SignalR Hub:** `/api/collaboration/{sessionId}` — Real-time code sync, video signaling

---

## Analytics

**Base:** `/api/analytics` — Requires Auth

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/me` | Get user analytics |
| GET | `/category/{category}` | Get category progress |
| GET | `/difficulty/{difficulty}` | Get difficulty progress |
| POST | `/rebuild` | Rebuild analytics from data |

---

## Coins & Leaderboard

**Base:** `/api/coins`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/me` | Get user coins (auth) |
| GET | `/user/{userId}` | Get user coins (public) |
| GET | `/me/transactions` | Get transaction history |
| GET | `/leaderboard` | Get top users (limit=200) |
| GET | `/me/rank` | Get user rank |
| GET | `/achievements` | Get achievement definitions |
| POST | `/award` | Award coins (admin only) |

---

## Site Settings

**Base:** `/api/SiteSettings` — Public

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/dashboard-video` | Get dashboard video URL, title, description |

---

## Admin

**Base:** `/api/admin` — Requires Admin Role

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/users` | Get all users |
| PUT | `/users/{id}/role` | Update user role |
| DELETE | `/users/{id}` | Delete user |
| GET | `/coach-applications/pending` | Get pending applications |
| POST | `/coach-applications/{id}/review` | Approve/reject application |
| POST | `/site-settings/dashboard-video/upload` | Upload dashboard video |
| PUT | `/site-settings/dashboard-video` | Update dashboard video metadata |

---

## Error Responses

- `400 Bad Request` — Invalid input
- `401 Unauthorized` — Missing or invalid token
- `403 Forbidden` — Insufficient permissions
- `404 Not Found` — Resource not found
- `415 Unsupported Media Type` — Invalid Content-Type (use multipart/form-data for file uploads)
- `500 Internal Server Error` — Server error
