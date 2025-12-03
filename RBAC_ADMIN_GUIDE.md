# Role-Based Access Control (RBAC) & Admin Guide

## 🔐 Default Admin Credentials

**⚠️ IMPORTANT SECURITY NOTICE ⚠️**

A default admin account is automatically created on first deployment:

```
Email: admin@vector.com
Password: Admin@123
```

### 🚨 **CHANGE THIS PASSWORD IMMEDIATELY IN PRODUCTION!** 🚨

### How to Change Admin Password:

1. Log in with default credentials
2. Navigate to Profile → Security
3. Change password using "Change Password" form
4. Recommended: Use a strong password (12+ characters, mix of uppercase, lowercase, numbers, symbols)

---

## 📋 Role System Overview

Vector implements a three-tier role system:

| Role | Access Level | Description |
|------|-------------|-------------|
| **Admin** | Full Access | Manage users, view analytics, change roles, access admin panel |
| **Coach** | Elevated Access | Create coaching content, manage sessions (future feature) |
| **Student** | Standard Access | Access learning content, book sessions, track progress |

---

## 🛡️ Backend RBAC Implementation

### Authorization Attribute

The `[AuthorizeRole]` attribute protects endpoints:

```csharp
[HttpGet("users")]
[AuthorizeRole("admin")]
public async Task<IActionResult> GetAllUsers()
{
    // Only admins can access this endpoint
}
```

### Admin Controller Endpoints

All admin endpoints require authentication AND admin role:

#### **GET /api/admin/users**
- Returns all users with basic info
- Response: `{ users: User[], total: number }`

#### **GET /api/admin/stats**
- Returns user statistics and role breakdown
- Response:
  ```json
  {
    "totalUsers": 150,
    "verifiedUsers": 120,
    "unverifiedUsers": 30,
    "roleBreakdown": {
      "students": 140,
      "coaches": 9,
      "admins": 1
    },
    "recentUsers": User[]
  }
  ```

#### **PUT /api/admin/users/{userId}/role**
- Updates a user's role
- Body: `{ "role": "student" | "coach" | "admin" }`
- Validates role value
- Returns updated user info

#### **DELETE /api/admin/users/{userId}**
- Deletes a user (soft delete recommended in production)
- Protection: Cannot delete last admin user
- Returns success message

---

## 🎨 Frontend RBAC Implementation

### Role Check Hooks

The `useAuth` hook provides role checking:

```typescript
const { user, hasRole, isAdmin, isCoach, isStudent } = useAuth();

// Check specific role
if (hasRole('admin')) {
  // Show admin features
}

// Check multiple roles
if (hasRole(['admin', 'coach'])) {
  // Show features for admins OR coaches
}

// Use convenience flags
if (isAdmin) {
  // Admin-only feature
}
```

### Protected Routes

The `<ProtectedRoute>` component enforces role-based access:

```typescript
// Require authentication only
<Route path="/dashboard" element={
  <ProtectedRoute requireAuth>
    <DashboardPage />
  </ProtectedRoute>
} />

// Require specific role
<Route path="/admin" element={
  <ProtectedRoute requireAuth requiredRole="admin">
    <AdminDashboardPage />
  </ProtectedRoute>
} />

// Require one of multiple roles
<Route path="/coaching" element={
  <ProtectedRoute requiredRole={['admin', 'coach']}>
    <CoachingPage />
  </ProtectedRoute>
} />
```

### Conditional UI Rendering

Show/hide features based on role:

```typescript
{user?.role === 'admin' && (
  <Link to="/admin">
    <i className="fas fa-shield-alt"></i> Admin Panel
  </Link>
)}

{hasRole(['admin', 'coach']) && (
  <button>Create Content</button>
)}
```

---

## 🎯 Admin Dashboard Features

Access: Navigate to `/admin` (admin role required)

### Features:
- **User Statistics**: Total, verified, unverified user counts
- **Role Breakdown**: Students, coaches, admins distribution
- **User Management Table**: View all users with email, name, role, verification status
- **Quick Actions**: (Future) Role updates, user management

### UI Components:
- Clean, modern dashboard with statistics cards
- Responsive table with user information
- Color-coded role badges (admin = yellow, coach = blue, student = green)
- Verification status indicators

---

## 🧪 Testing RBAC

### Backend Tests

Run all tests (including 8 new RBAC tests):

```bash
cd backend
dotnet test
```

### Test Coverage:
- ✅ Get all users (admin only)
- ✅ Get statistics with correct counts
- ✅ Update user role (valid roles)
- ✅ Reject invalid roles
- ✅ Handle non-existent users
- ✅ Delete user successfully
- ✅ Prevent deleting last admin
- ✅ Handle role authorization failures

---

## 🔒 Security Best Practices

### For Production Deployment:

1. **Change Default Password**: Immediately after first deployment
2. **Limit Admin Accounts**: Create only necessary admin users
3. **Use Strong Passwords**: Enforce password complexity
4. **Enable MFA** (future): Add two-factor authentication
5. **Audit Logging** (future): Log all admin actions
6. **Regular Review**: Periodically review user roles
7. **Principle of Least Privilege**: Grant minimum necessary permissions

### Environment Variables:

Ensure these are set securely:

```bash
JWT_SECRET=<strong-secret-key>
DATABASE_URL=<secure-connection-string>
SENDGRID_API_KEY=<api-key>
```

---

## 📊 Database Schema

### User Model (Role Field):

```sql
CREATE TABLE "Users" (
  "Id" uuid PRIMARY KEY,
  "Email" varchar(255) UNIQUE NOT NULL,
  "PasswordHash" text NOT NULL,
  "FirstName" varchar(100),
  "LastName" varchar(100),
  "Role" varchar(50) NOT NULL DEFAULT 'student',
  "EmailVerified" boolean DEFAULT false,
  "CreatedAt" timestamp,
  "UpdatedAt" timestamp
);
```

### Valid Roles:
- `student` (default for new registrations)
- `coach` (elevated permissions)
- `admin` (full access)

---

## 🚀 Deployment Notes

### Database Seeding

The admin user is automatically created on first deployment by `DbSeeder.cs`:

1. Runs after migrations in `Program.cs`
2. Checks if admin exists
3. Creates default admin if none found
4. Logs credentials to console (warning message)

### ECS Deployment

On AWS ECS:
- Seeder runs when container starts
- Check CloudWatch logs for admin creation message
- Credentials logged with warning banner

### Local Development

On local Docker:
- Admin created on first `docker-compose up`
- Check backend logs for credentials
- Use for testing and development

---

## 🛠️ Future Enhancements

### Planned Features:
- [ ] Granular permissions (not just roles)
- [ ] Role hierarchy (admin inherits coach permissions)
- [ ] Custom roles creation
- [ ] Permission-based UI hiding
- [ ] Audit log for admin actions
- [ ] Bulk user management
- [ ] Export user data (GDPR compliance)
- [ ] Activity monitoring dashboard

---

## 📞 Support

For issues or questions about RBAC:
1. Check this documentation
2. Review unit tests for usage examples
3. Check application logs for auth failures
4. Contact system administrator

---

**Last Updated**: December 3, 2025  
**Version**: 1.0  
**Status**: Production Ready ✅

