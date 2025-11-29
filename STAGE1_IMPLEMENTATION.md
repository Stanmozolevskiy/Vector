# Stage 1: User Management & Authentication - Detailed Implementation Guide

## Overview

Stage 1 focuses on building the foundation: user authentication, profile management, subscription system, and payment integration. This is the critical foundation that all other stages will build upon.

**Timeline: 4-6 weeks**

**Technology Stack:**
- Backend: .NET 8.0 + ASP.NET Core Web API (C#)
- Frontend: React 18+ with TypeScript + Tailwind CSS
- Database: PostgreSQL + Redis
- Payment: Stripe
- Email: SendGrid
- Storage: AWS S3

---

## Prerequisites

Before starting, ensure you have:

- [x] .NET 8.0 SDK installed
- [ ] PostgreSQL 15+ installed or access to a PostgreSQL database
- [ ] Redis 7+ installed or access to a Redis instance
- [x] Git installed
- [ ] Docker Desktop installed (for local development)
- [x] Visual Studio 2022, VS Code, or Rider IDE
- [x] GitHub account with repository created
- [x] AWS account (for infrastructure)
- [ ] Stripe account (for payments)
- [ ] SendGrid account (for emails)

---

## Week 1: Project Setup & Infrastructure

### Day 1-2: Project Initialization

#### Backend Setup ✅ COMPLETE
- [x] Create backend directory structure
- [x] Initialize .NET 8.0 Web API project
- [x] Create solution file
- [x] Set up project directories (Controllers, Services, Models, Data, DTOs, Middleware, Helpers)
- [x] Create all controller files (AuthController, UserController, SubscriptionController, StripeController)
- [x] Create all service interface files (IAuthService, IUserService, IJwtService, IEmailService, IS3Service, IStripeService)
- [x] Create all service implementation files (AuthService, UserService, JwtService, EmailService, S3Service, StripeService)
- [x] Create model files (User, Subscription, Payment, EmailVerification)
- [x] Create ApplicationDbContext
- [x] Create DbInitializer
- [x] Create all DTO files (RegisterDto, LoginDto, ResetPasswordDto, UpdateProfileDto, SubscribeDto)
- [x] Create ErrorHandlingMiddleware
- [x] Create JwtMiddleware
- [x] Create helper files (PasswordHasher, TokenGenerator)
- [x] Configure appsettings.json
- [x] Configure Program.cs with services

#### Frontend Setup ✅ COMPLETE
- [x] Create React app with TypeScript
- [x] Set up frontend directory structure
- [x] Configure package.json
- [x] Set up routing (React Router)
- [x] Set up state management (Auth context)
- [x] Configure Tailwind CSS

#### Infrastructure Setup
- [x] Create Terraform configuration files
- [x] Create Docker files (Dockerfile.backend, Dockerfile.frontend, docker-compose.yml)
- [x] Set up VPC module
- [x] Set up RDS module
- [x] Set up Redis module
- [x] Set up S3 module

#### Git Repository
- [x] Initialize Git repository (already exists)
- [x] Create .gitignore files
- [x] Create initial README.md
- [x] Set up branch protection rules on GitHub ✅
- [x] Create develop branch

### Day 3-4: AWS Infrastructure Basics (Terraform)
- [ ] Create VPC with public/private subnets
- [ ] Create RDS PostgreSQL instance (db.t3.micro for dev)
- [ ] Create ElastiCache Redis cluster
- [ ] Create S3 bucket for user uploads
- [ ] Create security groups
- [ ] Create IAM roles and policies
- [ ] Test Terraform configuration

### Day 5: Database Schema Setup
- [x] Set up Entity Framework Core with PostgreSQL
- [x] Create DbContext and entity models
- [ ] Create migration files
- [ ] Run initial migrations
- [ ] Set up database connection pooling
- [ ] Test database connection

### Day 6-7: CI/CD Pipeline Setup
- [ ] Create .github/workflows/backend.yml
- [ ] Create .github/workflows/frontend.yml
- [ ] Configure AWS secrets in GitHub
- [ ] Test CI/CD with initial commit
- [ ] Set up staging environment

---

## Week 2: Authentication System

### Day 8-9: User Registration
- [ ] Create registration API endpoint
- [ ] Implement password hashing (BCrypt)
- [ ] Create registration form UI
- [ ] Add form validation (both client and server)
- [ ] Handle errors gracefully
- [ ] Implement email verification token generation
- [ ] Send verification email

### Day 10-11: Login System
- [ ] Create login API endpoint
- [ ] Implement JWT token generation
- [ ] Create login page UI
- [ ] Store tokens securely
- [ ] Create auth context/hook
- [ ] Implement protected routes
- [ ] Implement refresh token rotation
- [ ] Store refresh tokens in Redis

### Day 12-13: Email Verification
- [ ] Set up SendGrid account
- [ ] Create email templates
- [ ] Implement email verification endpoint
- [ ] Create verification page UI
- [ ] Add resend verification functionality

### Day 14: Password Reset
- [ ] Create forgot password endpoint
- [ ] Create reset password endpoint
- [ ] Create forgot password page UI
- [ ] Create reset password page UI
- [ ] Add password strength validation

---

## Week 3: User Profile & Roles

### Day 15-16: User Profile Management
- [ ] Create profile API endpoints
- [ ] Implement profile picture upload
- [ ] Set up S3 bucket policies
- [ ] Create profile settings page
- [ ] Add image preview functionality

### Day 17-18: Role-Based Access Control
- [ ] Implement RBAC middleware
- [ ] Create role-based route protection
- [ ] Add role checks in frontend
- [ ] Create admin dashboard structure
- [ ] Test role permissions

### Day 19-20: Coach Application
- [ ] Create coach application endpoint
- [ ] Create coach approval endpoint
- [ ] Create application form UI
- [ ] Add admin approval interface
- [ ] Send approval/rejection emails

---

## Week 4: Subscription System

### Day 21-22: Subscription Plans
- [ ] Define subscription plans
- [ ] Create plans API endpoint
- [ ] Create subscription management page
- [ ] Design plan selection UI
- [ ] Add plan comparison

### Day 23-24: Stripe Integration
- [ ] Set up Stripe account
- [ ] Create Stripe products and prices
- [ ] Implement subscription creation
- [ ] Set up webhook endpoint
- [ ] Test webhook handling

### Day 25-26: Payment Processing
- [ ] Create subscription endpoint
- [ ] Implement payment method collection
- [ ] Create payment form UI
- [ ] Add payment success/failure handling
- [ ] Create invoice generation

---

## Week 5-6: Testing & Polish

### Testing
- [ ] Unit tests for services
- [ ] Integration tests for API endpoints
- [ ] Authentication flow tests
- [ ] Payment flow tests
- [ ] Email service tests
- [ ] Component tests (React Testing Library)
- [ ] Integration tests for forms
- [ ] E2E tests (Playwright/Cypress)

### Bug Fixes & Optimization
- [ ] Fix all identified bugs
- [ ] Optimize database queries
- [ ] Add caching where appropriate
- [ ] Optimize image uploads
- [ ] Improve error messages

### Documentation
- [ ] API documentation (Swagger/OpenAPI)
- [ ] Code comments
- [ ] README updates
- [ ] Deployment guide
- [ ] Environment variables documentation

### Security Audit
- [ ] Review authentication security
- [ ] Check for SQL injection vulnerabilities
- [ ] Verify XSS protection
- [ ] Review CORS settings
- [ ] Check API rate limiting

---

## Success Criteria Checklist

### Authentication
- [ ] Users can register with email/password
- [ ] Email verification works
- [ ] Login/logout works
- [ ] Password reset flow works
- [ ] JWT tokens are properly managed
- [ ] Refresh token rotation works

### User Profile
- [ ] Users can view their profile
- [ ] Users can update their profile
- [ ] Profile picture upload works
- [ ] Image optimization works

### Roles & Permissions
- [ ] Role-based access control works
- [ ] Students can access student features
- [ ] Coaches can access coach features
- [ ] Admins can access admin features

### Subscriptions
- [ ] Subscription plans are displayed
- [ ] Users can subscribe to plans
- [ ] Payment processing works
- [ ] Webhooks update subscription status
- [ ] Users can cancel subscriptions
- [ ] Invoices are generated

### Infrastructure
- [ ] CI/CD pipeline works
- [ ] Automated deployments work
- [ ] Database migrations run automatically
- [ ] Environment variables are configured

### Testing
- [ ] All critical paths are tested
- [ ] Test coverage > 70%
- [ ] E2E tests pass

---

## Progress Summary

**Completed:** 31 items  
**In Progress:** 0 items  
**Remaining:** 85+ items

**Current Status:** Week 1, Day 1-2 (Project Initialization) - ✅ COMPLETE
- ✅ Backend project structure fully initialized
- ✅ All controllers, services, DTOs, middleware, and helpers created
- ✅ Models and DbContext configured
- ✅ Frontend React app created with TypeScript
- ✅ Frontend directory structure set up
- ✅ Routing configured (React Router)
- ✅ Auth context and state management set up
- ✅ Tailwind CSS configured
- ✅ API service created
- ✅ Login and Register pages created
- ✅ Dashboard page created
- ✅ All projects build successfully

---

## Implementation Notes

### Backend Architecture
- Controllers handle HTTP requests/responses
- Services contain business logic
- DTOs for data transfer
- Models represent database entities
- Middleware for cross-cutting concerns
- Helpers for utility functions

### Frontend Architecture
- Pages for route components
- Components for reusable UI
- Hooks for shared logic
- Services for API calls
- Context for global state
- Utils for helper functions

### Key Implementation Files

**Backend:**
- `Program.cs` - Application configuration
- `ApplicationDbContext.cs` - Database context
- `AuthService.cs` - Authentication logic
- `JwtService.cs` - JWT token management
- `EmailService.cs` - Email sending
- `StripeService.cs` - Payment processing

**Frontend:**
- `App.tsx` - Main app component with routing
- `useAuth.tsx` - Authentication hook
- `api.ts` - Axios instance
- `LoginPage.tsx` - Login page
- `RegisterPage.tsx` - Registration page

---

## Next Steps

1. Complete infrastructure setup (Terraform, Docker)
2. Set up database migrations
3. Implement authentication endpoints
4. Connect frontend to backend
5. Test end-to-end authentication flow

For detailed step-by-step implementation instructions, refer to `IMPLEMENTATION_PLAN_STEP_BY_STEP.md`.
