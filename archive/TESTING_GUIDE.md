# Testing Implementation Guide

This document outlines what's required to implement the remaining test types for the Vector platform.

## Current Test Status

- ✅ **Unit Tests**: 134 tests passing
  - Service layer tests (Auth, User, Coach, Subscription, Email)
  - Controller tests (Auth, User, Coach, Subscription, Admin)
  - Email service tests (14 tests covering all email methods)
  - All using InMemory database and Moq for mocking

- ✅ **Integration Tests**: 17 tests implemented, all passing
  - Auth endpoints (Register, Login, GetMe)
  - Subscription endpoints (GetPlans, GetPlan, GetMySubscription, UpdateSubscription, CancelSubscription, GetInvoices)
  - Using WebApplicationFactory with InMemory database
  - Mocked external services (Redis, S3, Email)

- ✅ **Form Integration Tests**: 25 tests implemented, all passing
  - Login page tests (8 tests - form rendering, validation, submission, error handling)
  - Register page tests (9 tests - form rendering, validation, submission, error handling)
  - Profile page tests (8 tests - form rendering, data loading, updates, password changes)
  - Using React Testing Library, Vitest, and MSW for API mocking

**Total: 176 tests (134 unit + 17 API integration + 25 form integration) - 100% passing**

---

## 1. Integration Tests for API Endpoints

### What They Are
Integration tests verify that multiple components work together correctly, testing the full request/response cycle through the API.

### What's Required

#### Setup
1. **Test Web Application Factory**
   - Use `WebApplicationFactory<Program>` from `Microsoft.AspNetCore.Mvc.Testing`
   - Configure test database (PostgreSQL test instance or InMemory)
   - Configure test Redis instance (or mock)
   - Override configuration for test environment

2. **Test Database**
   - Option A: Use InMemory database (simpler, faster)
   - Option B: Use separate PostgreSQL test database (more realistic)
   - Seed test data before each test
   - Clean up after each test

3. **Test HTTP Client**
   - Use `HttpClient` from `WebApplicationFactory`
   - Handle authentication tokens
   - Test all HTTP methods (GET, POST, PUT, DELETE)

#### Example Structure
```csharp
public class SubscriptionIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SubscriptionIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace database with test database
                // Replace Redis with test Redis
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetPlans_ReturnsAllPlans()
    {
        // Act
        var response = await _client.GetAsync("/api/subscriptions/plans");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var plans = await response.Content.ReadFromJsonAsync<List<SubscriptionPlanDto>>();
        Assert.NotNull(plans);
        Assert.Equal(3, plans.Count);
    }
}
```

#### What to Test
- ✅ Full request/response cycle
- ✅ Authentication/authorization
- ✅ Database operations (CRUD)
- ✅ Error handling (404, 400, 401, 500)
- ✅ Validation
- ✅ Status codes
- ✅ Response formats

#### Estimated Effort
- **Setup**: 4-6 hours
- **Tests per endpoint**: 1-2 hours
- **Total for all endpoints**: 20-30 hours

#### Dependencies
- `Microsoft.AspNetCore.Mvc.Testing` (already installed)
- Test database setup
- Test data seeding utilities

---

## 2. Email Service Tests ✅ COMPLETE

### What They Are
Tests that verify email sending functionality works correctly, including email content, recipients, and delivery.

### Status: ✅ **14 unit tests implemented and passing**

**Tests Implemented:**
- `SendVerificationEmailAsync_WhenSendGridDisabled_LogsToConsole` - Verifies logging when SendGrid is not configured
- `SendVerificationEmailAsync_WhenSendGridEnabled_SendsEmail` - Verifies email sending when SendGrid is configured
- `SendPasswordResetEmailAsync_WhenSendGridDisabled_LogsToConsole` - Verifies password reset email logging
- `SendPasswordResetEmailAsync_WithValidInput_CompletesSuccessfully` - Verifies method completes without errors
- `SendWelcomeEmailAsync_WhenSendGridDisabled_LogsToConsole` - Verifies welcome email logging
- `SendWelcomeEmailAsync_WithValidInput_CompletesSuccessfully` - Verifies welcome email completion
- `SendSubscriptionConfirmationEmailAsync_WhenSendGridDisabled_LogsToConsole` - Verifies subscription confirmation logging
- `SendSubscriptionConfirmationEmailAsync_WithValidInput_CompletesSuccessfully` - Verifies subscription confirmation completion
- `SendEmailAsync_WhenSendGridDisabled_LogsToConsole` - Verifies generic email logging
- `SendEmailAsync_WithValidInput_CompletesSuccessfully` - Verifies generic email completion
- `SendVerificationEmailAsync_UsesCorrectFrontendUrl` - Verifies frontend URL is used correctly
- `SendPasswordResetEmailAsync_UsesCorrectFrontendUrl` - Verifies frontend URL in password reset
- `SendEmailAsync_HandlesNullConfigurationGracefully` - Verifies graceful handling of null configuration
- `AllEmailMethods_HandleEmptyStringsGracefully` - Verifies all methods handle empty strings

**Implementation Details:**
- Uses `Moq` to mock `IConfiguration` and `ILogger<EmailService>`
- Tests both SendGrid enabled and disabled scenarios
- Verifies logging behavior when SendGrid is not configured
- Tests graceful handling of edge cases (null config, empty strings)
- All tests verify methods complete without throwing exceptions

**File Location:** `backend/Vector.Api.Tests/Services/EmailServiceTests.cs`

**What's Covered:**
- ✅ Email sending for registration (verification email)
- ✅ Email sending for password reset
- ✅ Email sending for email verification
- ✅ Email sending for subscription confirmations
- ✅ Email sending for welcome messages
- ✅ Generic email sending
- ✅ Error handling (SendGrid disabled, null config, empty strings)
- ✅ Frontend URL configuration

**Future Enhancements (Optional):**
- Integration tests with actual email service (Mailtrap, MailHog)
- Email content validation (subject, body, recipient verification)
- SendGrid API response validation

---

## 3. Integration Tests for Forms ✅ COMPLETE

### What They Are
Tests that verify frontend forms work correctly with the backend API, including validation, submission, and error handling.

### Status: ✅ **25 tests implemented**

**Tests Implemented:**
- **LoginPage (8 tests):**
  - Form rendering with all fields
  - Email validation
  - Password validation
  - Successful login and navigation
  - Invalid credentials error handling
  - Unverified email error handling
  - Loading state during submission
  - Remember me checkbox toggle

- **RegisterPage (8 tests):**
  - Form rendering with all fields
  - Email validation
  - Password length validation
  - Password match validation
  - Terms acceptance validation
  - Successful registration and success message
  - Existing email error handling
  - Loading state during submission
  - Optional name fields

- **ProfilePage (9 tests):**
  - Form rendering with all fields
  - User data loading into form
  - Profile update functionality
  - Password change form in Privacy tab
  - Password validation (matching passwords)
  - Successful password change
  - Incorrect current password error handling
  - Subscription information display

**Implementation Details:**
- Uses Vitest as test runner
- React Testing Library for component testing
- MSW (Mock Service Worker) for API mocking
- Custom test utilities with AuthProvider and Router
- Comprehensive form validation testing
- Error handling and success message testing
- Loading state verification

**File Locations:**
- `frontend/src/pages/auth/__tests__/LoginPage.test.tsx`
- `frontend/src/pages/auth/__tests__/RegisterPage.test.tsx`
- `frontend/src/pages/profile/__tests__/ProfilePage.test.tsx`
- `frontend/src/test/mocks/handlers.ts` - MSW handlers
- `frontend/src/test/mocks/server.ts` - MSW server setup
- `frontend/src/test/utils/test-utils.tsx` - Test utilities

**What's Covered:**
- ✅ Form rendering
- ✅ Form validation (client-side)
- ✅ Form submission
- ✅ Success/error messages
- ✅ Loading states
- ✅ User interactions (typing, clicking, selecting)
- ✅ Navigation after form submission
- ✅ API error handling

**Setup:**
1. **Dependencies Installed:**
   ```bash
   npm install --save-dev vitest @testing-library/react @testing-library/jest-dom @testing-library/user-event msw jsdom
   ```

2. **Test Configuration:**
   - Vitest configured in `vite.config.ts`
   - MSW server setup in `frontend/src/test/mocks/server.ts`
   - Test utilities in `frontend/src/test/utils/test-utils.tsx`
   - Test setup in `frontend/src/test/setup.ts`

#### Example Structure
```typescript
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SubscriptionPlansPage } from './SubscriptionPlansPage';
import { server } from '../mocks/server';

describe('SubscriptionPlansPage', () => {
  it('displays all subscription plans', async () => {
    // Arrange
    render(<SubscriptionPlansPage />);

    // Act
    await waitFor(() => {
      expect(screen.getByText('Free Plan')).toBeInTheDocument();
      expect(screen.getByText('Monthly Plan')).toBeInTheDocument();
      expect(screen.getByText('Annual Plan')).toBeInTheDocument();
    });
  });

  it('marks current plan correctly', async () => {
    // Arrange
    server.use(
      rest.get('/api/subscriptions/me', (req, res, ctx) => {
        return res(ctx.json({ planType: 'monthly' }));
      })
    );
    
    render(<SubscriptionPlansPage />);

    // Act & Assert
    await waitFor(() => {
      expect(screen.getByText('Current Plan')).toBeInTheDocument();
    });
  });
});
```

#### Running Tests
```bash
# Run all tests
npm test

# Run tests in watch mode
npm test -- --watch

# Run tests with UI
npm run test:ui

# Run tests with coverage
npm run test:coverage
```

#### Future Enhancements (Optional)
- Add tests for ForgotPasswordPage
- Add tests for ResetPasswordPage
- Add tests for CoachApplicationPage
- Add tests for SubscriptionPlansPage form interactions
- Add visual regression testing
- Add accessibility testing

---

## Implementation Priority

### Phase 1: Integration Tests for API Endpoints (High Priority)
**Why**: Ensures the full API stack works correctly
**Effort**: 20-30 hours
**Impact**: High - Catches integration issues early

### Phase 2: Email Service Tests (Medium Priority)
**Why**: Ensures critical communication works
**Effort**: 10-15 hours
**Impact**: Medium - Important for user experience

### Phase 3: Form Integration Tests (Medium Priority)
**Why**: Ensures frontend forms work correctly
**Effort**: 20-30 hours
**Impact**: Medium - Important for user experience

---

## Recommended Next Steps

1. **Start with API Integration Tests**
   - Set up `WebApplicationFactory`
   - Create test database setup
   - Write tests for critical endpoints (Auth, Subscription)

2. **Add Email Service Tests**
   - Set up email testing service
   - Add content validation tests
   - Add integration tests

3. **Add Form Integration Tests**
   - Set up React Testing Library
   - Set up MSW for API mocking
   - Write tests for critical forms (Login, Register, Subscription)

---

## Test Coverage Goals

- **Unit Tests**: 80%+ coverage (Current: ~70%)
- **Integration Tests**: 60%+ coverage of critical paths
- **Form Tests**: 100% coverage of user-facing forms

---

## Resources

- [ASP.NET Core Integration Tests](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [React Testing Library](https://testing-library.com/react)
- [Mock Service Worker (MSW)](https://mswjs.io/)
- [xUnit Documentation](https://xunit.net/)

