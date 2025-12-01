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
- [x] PostgreSQL 15+ installed or access to a PostgreSQL database
- [x] Redis 7+ installed or access to a Redis instance
- [x] Git installed
- [x] Docker Desktop installed (for local development)
- [x] Visual Studio 2022, VS Code, or Rider IDE
- [x] GitHub account with repository created
- [x] AWS account (for infrastructure)
- [ ] Stripe account (for payments)
- [x] SendGrid account (for emails) ✅

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

### Day 3-4: AWS Infrastructure Basics (Terraform) ✅ COMPLETE
- [x] Create VPC with public/private subnets
- [x] Create RDS PostgreSQL instance (db.t3.micro for dev)
- [x] Create ElastiCache Redis cluster
- [x] Create S3 bucket for user uploads
- [x] Create security groups
- [x] Create IAM roles and policies
- [x] Test Terraform configuration

### Day 5: Database Schema Setup
- [x] Set up Entity Framework Core with PostgreSQL
- [x] Create DbContext and entity models
- [x] Configure database connection pooling with retry logic
- [x] Set up migration folder structure
- [x] Create migration helper scripts
- [x] Create migration files ✅ (InitialCreate migration created)
- [x] Run initial migrations ✅ (Migrations applied successfully - all tables created)
- [x] Test database connection ✅ (Connection verified)

### Day 6-7: CI/CD Pipeline Setup ✅ COMPLETE
- [x] Create .github/workflows/backend.yml ✅
- [x] Create .github/workflows/frontend.yml ✅
- [x] Create GitHub Secrets setup documentation ✅
- [x] Configure AWS secrets in GitHub ✅ (3 secrets added)
- [x] Test CI/CD with initial commit ✅ (Pipeline triggered)
- [x] Create AWS ECS deployment infrastructure ✅
- [x] Create Application Load Balancer module ✅
- [x] Create ECR repositories ✅
- [x] Update CI/CD workflows with ECS deployment ✅
- [x] Deploy infrastructure with Terraform ✅ (Infrastructure deployed successfully)
- [x] Backend and Frontend deployed to AWS ECS ✅
- [x] Swagger UI configured and accessible ✅
- [x] Database migrations running automatically on container startup ✅
- [ ] Set up staging environment (future task)

---

## Week 2: Authentication System

### Day 8-9: User Registration ✅ COMPLETE
- [x] Create registration API endpoint ✅ (POST /api/auth/register)
- [x] Implement password hashing (BCrypt) ✅ (PasswordHasher helper)
- [x] Register services in Program.cs (AuthService, JwtService, EmailService) ✅
- [x] Create registration form UI ✅ (already exists in frontend)
- [x] Add form validation (both client and server) ✅ (Data annotations + ModelState validation)
- [x] Handle errors gracefully ✅ (Try-catch with appropriate status codes)
- [x] Implement email verification token generation ✅ (Token generated and stored in EmailVerifications table)
- [x] Send verification email ✅ (EmailService.SendVerificationEmailAsync called)

### Day 10-11: Login System ✅ COMPLETE
- [x] Create login API endpoint ✅ (POST /api/auth/login)
- [x] Implement JWT token generation ✅ (JwtService.GenerateAccessToken)
- [x] Create login page UI ✅ (LoginPage redesigned to match HTML reference)
- [x] Store tokens securely ✅ (localStorage in useAuth hook)
- [x] Create auth context/hook ✅ (useAuth hook already exists)
- [x] Redesign auth pages ✅ (Login, Register, Forgot Password match HTML reference exactly)
- [x] Add auth.css styling ✅ (Created comprehensive auth.css with all styles)
- [x] Add FontAwesome icons ✅ (Icons for Vector logo, social buttons, etc.)
- [x] Add Inter font ✅ (Google Fonts integration)
- [x] Implement password verification ✅ (PasswordHasher.VerifyPassword)
- [x] Implement email verification check ✅ (Requires verified email to login)
- [ ] Implement protected routes
- [ ] Implement refresh token rotation
- [ ] Store refresh tokens in Redis

### Day 12-13: Email Verification ✅ COMPLETE
- [x] Set up SendGrid account ✅
- [x] Configure SendGrid API key and sender email ✅
- [x] Add SendGrid to Docker configuration ✅
- [x] Add SendGrid to Terraform/ECS configuration ✅
- [x] Add SendGrid secrets to GitHub ✅
- [x] Deploy SendGrid configuration to Development ✅ (Terraform applied, ECS task definition updated)
- [x] Create email templates (basic templates already exist) ✅
- [x] Implement email verification endpoint ✅ (GET /api/auth/verify-email?token=xxx)
- [x] Create verification page UI ✅ (VerifyEmailPage component created and deployed)
- [x] Test email verification flow ✅ (End-to-end flow tested and working)
- [ ] Add resend verification functionality

### Day 14: Password Reset
- [x] Create forgot password page UI ✅ (ForgotPasswordPage redesigned to match HTML reference)
- [ ] Create forgot password endpoint (backend implementation pending)
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
- [x] Users can register with email/password ✅
- [x] Email verification works ✅ (Endpoint implemented: GET /api/auth/verify-email?token=xxx)
- [x] Login/logout works ✅ (Login endpoint implemented: POST /api/auth/login)
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

**Completed:** 60+ items  
**In Progress:** 0 items  
**Remaining:** 60+ items

**Current Status:** Week 2, Day 10-11 (Login System) - ✅ Login System Complete, Auth Pages Redesigned, Ready for Dev Deployment
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
- ✅ **AWS Infrastructure deployed (VPC, RDS, Redis, S3)**
- ✅ **Terraform configuration complete**
- ✅ **Database connection pooling configured**
- ✅ **Migration scripts created**
- ✅ **Initial migration created** (InitialCreate)
- ✅ **Migrations applied successfully** (All tables created)
- ✅ **Database connection tested and verified**
- ✅ **Archive folder created** for completed documents
- ✅ **CI/CD pipelines created** (Backend and Frontend workflows)
- ✅ **Environments and deployment documentation** created
- ✅ **GitHub Secrets setup guide** created
- ✅ **AWS ECS deployment infrastructure** created (ECS, ALB, ECR modules)
- ✅ **CI/CD pipeline tested** (triggered on push to develop)
- ✅ **AWS deployment workflows** configured
- ✅ **Database migrations** running automatically on container startup
- ✅ **SendGrid account created and configured** ✅
- ✅ **SendGrid added to Docker configuration** ✅
- ✅ **SendGrid added to Terraform/ECS configuration** ✅
- ✅ **SendGrid secrets added to GitHub** ✅
- ✅ **SendGrid deployed to Development environment** ✅ (Terraform applied, ECS task definition updated)

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
