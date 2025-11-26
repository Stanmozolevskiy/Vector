# Stage 1: User Management & Authentication - Detailed Implementation Plan

## Overview

Stage 1 focuses on building the foundation: user authentication, profile management, subscription system, and payment integration. This is the critical foundation that all other stages will build upon.

**Timeline: 4-6 weeks**

---

## Week 1: Project Setup & Infrastructure

### Goals
- Set up development environment
- Configure AWS infrastructure basics
- Initialize repositories and CI/CD
- Set up database and basic services

### Tasks

#### Day 1-2: Project Initialization

**Backend Setup**
```bash
# Create backend directory structure
backend/
├── src/
│   ├── config/
│   ├── controllers/
│   ├── middleware/
│   ├── models/
│   ├── routes/
│   ├── services/
│   ├── utils/
│   └── app.ts
├── tests/
├── package.json
├── tsconfig.json
└── .env.example
```

**Frontend Setup**
```bash
# Create React app with TypeScript
npx create-react-app frontend --template typescript
# Or use Vite for faster builds
npm create vite@latest frontend -- --template react-ts

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   ├── hooks/
│   ├── services/
│   ├── store/
│   ├── utils/
│   └── App.tsx
├── public/
├── package.json
└── tsconfig.json
```

**Infrastructure Setup**
```bash
infrastructure/
├── terraform/
│   ├── main.tf
│   ├── variables.tf
│   ├── outputs.tf
│   └── modules/
│       ├── vpc/
│       ├── rds/
│       ├── ecs/
│       └── s3/
└── docker/
    ├── Dockerfile.backend
    ├── Dockerfile.frontend
    └── docker-compose.yml
```

**Git Repository**
- [ ] Initialize Git repository
- [ ] Create `.gitignore` files
- [ ] Create initial `README.md`
- [ ] Set up branch protection rules on GitHub
- [ ] Create `develop` branch

#### Day 3-4: AWS Infrastructure Basics (Terraform)

**Create Terraform Configuration**

```hcl
# infrastructure/terraform/main.tf

# VPC Module
module "vpc" {
  source = "./modules/vpc"
  
  environment = var.environment
  vpc_cidr    = "10.0.0.0/16"
}

# RDS PostgreSQL
module "database" {
  source = "./modules/rds"
  
  environment     = var.environment
  vpc_id          = module.vpc.vpc_id
  subnet_ids      = module.vpc.private_subnet_ids
  instance_class  = "db.t3.micro"
  allocated_storage = 20
}

# ElastiCache Redis
module "redis" {
  source = "./modules/redis"
  
  environment = var.environment
  vpc_id      = module.vpc.vpc_id
  subnet_ids  = module.vpc.private_subnet_ids
  node_type   = "cache.t3.micro"
}

# S3 Buckets
module "storage" {
  source = "./modules/s3"
  
  environment = var.environment
  
  buckets = {
    "user-uploads" = {
      versioning = true
      encryption = true
    }
  }
}
```

**AWS Resources to Create:**
- [ ] VPC with public/private subnets
- [ ] RDS PostgreSQL instance (db.t3.micro for dev)
- [ ] ElastiCache Redis cluster
- [ ] S3 bucket for user uploads
- [ ] Security groups
- [ ] IAM roles and policies

#### Day 5: Database Schema Setup

**Run Database Migrations**

```sql
-- Create users table
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    role VARCHAR(20) NOT NULL DEFAULT 'student',
    email_verified BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create subscriptions table
CREATE TABLE subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    plan_type VARCHAR(20) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'active',
    current_period_start TIMESTAMP NOT NULL,
    current_period_end TIMESTAMP NOT NULL,
    stripe_subscription_id VARCHAR(255) UNIQUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create payments table
CREATE TABLE payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    amount DECIMAL(10, 2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'USD',
    status VARCHAR(20) NOT NULL,
    stripe_payment_intent_id VARCHAR(255) UNIQUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

**Tasks:**
- [ ] Set up database connection (Prisma or TypeORM)
- [ ] Create migration files
- [ ] Run initial migrations
- [ ] Set up database connection pooling

#### Day 6-7: CI/CD Pipeline Setup

**GitHub Actions Workflows**

- [ ] Create `.github/workflows/backend.yml`
- [ ] Create `.github/workflows/frontend.yml`
- [ ] Configure AWS secrets in GitHub
- [ ] Test CI/CD with initial commit
- [ ] Set up staging environment

**Docker Setup**

```dockerfile
# docker/Dockerfile.backend
FROM node:20-alpine
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production
COPY . .
RUN npm run build
EXPOSE 3000
CMD ["node", "dist/app.js"]
```

---

## Week 2: Authentication System

### Goals
- Implement user registration
- Implement login/logout
- JWT token management
- Email verification

### Tasks

#### Day 8-9: User Registration

**Backend Implementation**

```typescript
// backend/src/controllers/auth.controller.ts

export const register = async (req: Request, res: Response) => {
  // 1. Validate input (email, password)
  // 2. Check if user exists
  // 3. Hash password
  // 4. Create user
  // 5. Generate email verification token
  // 6. Send verification email
  // 7. Return success
};

// backend/src/services/auth.service.ts
export class AuthService {
  async registerUser(data: RegisterDto) {
    // Business logic
  }
  
  async hashPassword(password: string): Promise<string> {
    return bcrypt.hash(password, 10);
  }
  
  async generateEmailToken(userId: string): Promise<string> {
    // Generate and store verification token
  }
}
```

**Frontend Implementation**

```typescript
// frontend/src/pages/Register.tsx
export const RegisterPage = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    firstName: '',
    lastName: ''
  });
  
  const handleSubmit = async (e: FormEvent) => {
    // Submit registration
  };
  
  return (
    <form onSubmit={handleSubmit}>
      {/* Registration form */}
    </form>
  );
};
```

**Tasks:**
- [ ] Create registration API endpoint
- [ ] Implement password hashing (bcrypt)
- [ ] Create registration form UI
- [ ] Add form validation (both client and server)
- [ ] Handle errors gracefully

#### Day 10-11: Login System

**Backend Implementation**

```typescript
// backend/src/controllers/auth.controller.ts

export const login = async (req: Request, res: Response) => {
  // 1. Validate credentials
  // 2. Check password
  // 3. Generate JWT access token
  // 4. Generate refresh token
  // 5. Store refresh token in Redis
  // 6. Return tokens
};

export const logout = async (req: Request, res: Response) => {
  // 1. Remove refresh token from Redis
  // 2. Return success
};
```

**JWT Implementation**

```typescript
// backend/src/services/jwt.service.ts

export class JwtService {
  generateAccessToken(userId: string, role: string): string {
    return jwt.sign(
      { userId, role },
      process.env.JWT_SECRET!,
      { expiresIn: '15m' }
    );
  }
  
  generateRefreshToken(userId: string): string {
    return jwt.sign(
      { userId },
      process.env.JWT_REFRESH_SECRET!,
      { expiresIn: '7d' }
    );
  }
  
  verifyToken(token: string): TokenPayload {
    return jwt.verify(token, process.env.JWT_SECRET!);
  }
}
```

**Authentication Middleware**

```typescript
// backend/src/middleware/auth.middleware.ts

export const authenticate = async (
  req: Request,
  res: Response,
  next: NextFunction
) => {
  // 1. Extract token from header
  // 2. Verify token
  // 3. Attach user to request
  // 4. Call next()
};
```

**Frontend Implementation**

```typescript
// frontend/src/services/auth.service.ts

export const authService = {
  async login(email: string, password: string) {
    const response = await api.post('/auth/login', { email, password });
    // Store tokens
    localStorage.setItem('accessToken', response.data.accessToken);
    localStorage.setItem('refreshToken', response.data.refreshToken);
    return response.data;
  },
  
  async logout() {
    await api.post('/auth/logout');
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  }
};
```

**Tasks:**
- [ ] Create login API endpoint
- [ ] Implement JWT token generation
- [ ] Create login page UI
- [ ] Store tokens securely
- [ ] Create auth context/hook
- [ ] Implement protected routes

#### Day 12-13: Email Verification

**Backend Implementation**

```typescript
// backend/src/controllers/auth.controller.ts

export const verifyEmail = async (req: Request, res: Response) => {
  // 1. Extract token from query params
  // 2. Verify token
  // 3. Update user email_verified status
  // 4. Redirect to login or success page
};

export const resendVerification = async (req: Request, res: Response) => {
  // 1. Generate new verification token
  // 2. Send email
  // 3. Return success
};
```

**Email Service**

```typescript
// backend/src/services/email.service.ts

export class EmailService {
  async sendVerificationEmail(email: string, token: string) {
    const verificationUrl = `${process.env.FRONTEND_URL}/verify-email?token=${token}`;
    
    await sendGrid.send({
      to: email,
      from: 'noreply@yourdomain.com',
      subject: 'Verify your email',
      html: `
        <h1>Verify Your Email</h1>
        <p>Click <a href="${verificationUrl}">here</a> to verify your email.</p>
      `
    });
  }
}
```

**Tasks:**
- [ ] Set up SendGrid account
- [ ] Create email templates
- [ ] Implement email verification endpoint
- [ ] Create verification page UI
- [ ] Add resend verification functionality

#### Day 14: Password Reset

**Backend Implementation**

```typescript
export const forgotPassword = async (req: Request, res: Response) => {
  // 1. Check if user exists
  // 2. Generate reset token
  // 3. Store token in database with expiration
  // 4. Send reset email
  // 5. Return success (don't reveal if user exists)
};

export const resetPassword = async (req: Request, res: Response) => {
  // 1. Verify reset token
  // 2. Check expiration
  // 3. Hash new password
  // 4. Update user password
  // 5. Invalidate reset token
  // 6. Return success
};
```

**Tasks:**
- [ ] Create forgot password endpoint
- [ ] Create reset password endpoint
- [ ] Create forgot password page UI
- [ ] Create reset password page UI
- [ ] Add password strength validation

---

## Week 3: User Profile & Roles

### Goals
- User profile management
- Role-based access control
- Profile picture upload

### Tasks

#### Day 15-16: User Profile Management

**Backend Implementation**

```typescript
// backend/src/controllers/user.controller.ts

export const getProfile = async (req: Request, res: Response) => {
  // Return current user's profile
};

export const updateProfile = async (req: Request, res: Response) => {
  // Update user profile fields
};

export const uploadProfilePicture = async (req: Request, res: Response) => {
  // 1. Validate image file
  // 2. Resize/optimize image
  // 3. Upload to S3
  // 4. Update user profile_picture_url
  // 5. Return new URL
};
```

**S3 Upload Service**

```typescript
// backend/src/services/s3.service.ts

export class S3Service {
  async uploadFile(file: Buffer, key: string, contentType: string) {
    const params = {
      Bucket: process.env.AWS_S3_BUCKET!,
      Key: key,
      Body: file,
      ContentType: contentType,
      ACL: 'public-read'
    };
    
    return s3.upload(params).promise();
  }
}
```

**Tasks:**
- [ ] Create profile API endpoints
- [ ] Implement profile picture upload
- [ ] Set up S3 bucket policies
- [ ] Create profile settings page
- [ ] Add image preview functionality

#### Day 17-18: Role-Based Access Control

**Backend Implementation**

```typescript
// backend/src/middleware/rbac.middleware.ts

export const requireRole = (...roles: string[]) => {
  return (req: Request, res: Response, next: NextFunction) => {
    if (!roles.includes(req.user.role)) {
      return res.status(403).json({ error: 'Forbidden' });
    }
    next();
  };
};

// Usage
router.post('/admin/users', 
  authenticate, 
  requireRole('admin'), 
  createUser
);
```

**Frontend Implementation**

```typescript
// frontend/src/hooks/useAuth.ts

export const useAuth = () => {
  const { user } = useAuthContext();
  
  const hasRole = (role: string) => {
    return user?.role === role;
  };
  
  const hasAnyRole = (roles: string[]) => {
    return roles.includes(user?.role || '');
  };
  
  return { user, hasRole, hasAnyRole };
};
```

**Tasks:**
- [ ] Implement RBAC middleware
- [ ] Create role-based route protection
- [ ] Add role checks in frontend
- [ ] Create admin dashboard structure
- [ ] Test role permissions

#### Day 19-20: Coach Application

**Backend Implementation**

```typescript
// backend/src/controllers/coach.controller.ts

export const applyAsCoach = async (req: Request, res: Response) => {
  // 1. Validate application data
  // 2. Create coach application record
  // 3. Notify admin for approval
  // 4. Return success
};

export const approveCoach = async (req: Request, res: Response) => {
  // 1. Verify admin role
  // 2. Update user role to 'coach'
  // 3. Create coach profile
  // 4. Send approval email
};
```

**Tasks:**
- [ ] Create coach application endpoint
- [ ] Create coach approval endpoint
- [ ] Create application form UI
- [ ] Add admin approval interface
- [ ] Send approval/rejection emails

---

## Week 4: Subscription System

### Goals
- Subscription plan management
- Stripe integration
- Subscription status tracking

### Tasks

#### Day 21-22: Subscription Plans

**Backend Implementation**

```typescript
// backend/src/controllers/subscription.controller.ts

export const getPlans = async (req: Request, res: Response) => {
  // Return available subscription plans
  return res.json([
    {
      id: 'monthly',
      name: 'Monthly',
      price: 29.99,
      interval: 'month'
    },
    {
      id: 'annual',
      name: 'Annual',
      price: 299.99,
      interval: 'year'
    }
  ]);
};

export const getMySubscription = async (req: Request, res: Response) => {
  // Return current user's subscription
};
```

**Frontend Implementation**

```typescript
// frontend/src/pages/SubscriptionPlans.tsx

export const SubscriptionPlansPage = () => {
  const { plans, currentSubscription } = useSubscriptions();
  
  return (
    <div>
      {plans.map(plan => (
        <PlanCard 
          key={plan.id} 
          plan={plan}
          isCurrent={currentSubscription?.planId === plan.id}
        />
      ))}
    </div>
  );
};
```

**Tasks:**
- [ ] Define subscription plans
- [ ] Create plans API endpoint
- [ ] Create subscription management page
- [ ] Design plan selection UI
- [ ] Add plan comparison

#### Day 23-24: Stripe Integration

**Backend Implementation**

```typescript
// backend/src/services/stripe.service.ts

export class StripeService {
  async createCustomer(email: string, name: string) {
    return stripe.customers.create({
      email,
      name
    });
  }
  
  async createSubscription(customerId: string, priceId: string) {
    return stripe.subscriptions.create({
      customer: customerId,
      items: [{ price: priceId }],
      payment_behavior: 'default_incomplete',
      payment_settings: { save_default_payment_method: 'on_subscription' },
      expand: ['latest_invoice.payment_intent']
    });
  }
  
  async handleWebhook(event: Stripe.Event) {
    switch (event.type) {
      case 'customer.subscription.created':
      case 'customer.subscription.updated':
        // Update subscription in database
        break;
      case 'customer.subscription.deleted':
        // Cancel subscription
        break;
      case 'invoice.payment_succeeded':
        // Record payment
        break;
    }
  }
}
```

**Webhook Handler**

```typescript
// backend/src/routes/stripe.routes.ts

router.post('/webhook', 
  express.raw({ type: 'application/json' }),
  async (req, res) => {
    const sig = req.headers['stripe-signature'];
    const event = stripe.webhooks.constructEvent(
      req.body,
      sig,
      process.env.STRIPE_WEBHOOK_SECRET!
    );
    
    await stripeService.handleWebhook(event);
    res.json({ received: true });
  }
);
```

**Tasks:**
- [ ] Set up Stripe account
- [ ] Create Stripe products and prices
- [ ] Implement subscription creation
- [ ] Set up webhook endpoint
- [ ] Test webhook handling

#### Day 25-26: Payment Processing

**Backend Implementation**

```typescript
export const subscribe = async (req: Request, res: Response) => {
  // 1. Get user and plan
  // 2. Create or get Stripe customer
  // 3. Create subscription
  // 4. Create payment intent
  // 5. Save subscription to database
  // 6. Return client secret
};

export const cancelSubscription = async (req: Request, res: Response) => {
  // 1. Get user's subscription
  // 2. Cancel in Stripe
  // 3. Update database
  // 4. Send cancellation email
};
```

**Frontend Implementation**

```typescript
// frontend/src/components/PaymentForm.tsx

import { loadStripe } from '@stripe/stripe-js';

export const PaymentForm = () => {
  const stripePromise = loadStripe(process.env.REACT_APP_STRIPE_PUBLIC_KEY!);
  
  const handleSubmit = async (paymentMethod) => {
    // Create subscription
    const response = await api.post('/subscriptions/subscribe', {
      planId: selectedPlan.id,
      paymentMethodId: paymentMethod.id
    });
    
    // Confirm payment
    const stripe = await stripePromise;
    await stripe.confirmCardPayment(response.data.clientSecret);
  };
};
```

**Tasks:**
- [ ] Create subscription endpoint
- [ ] Implement payment method collection
- [ ] Create payment form UI
- [ ] Add payment success/failure handling
- [ ] Create invoice generation

---

## Week 5-6: Testing & Polish

### Goals
- Comprehensive testing
- Bug fixes
- Performance optimization
- Documentation

### Tasks

#### Testing

**Backend Tests**
- [ ] Unit tests for services
- [ ] Integration tests for API endpoints
- [ ] Authentication flow tests
- [ ] Payment flow tests
- [ ] Email service tests

**Frontend Tests**
- [ ] Component tests (React Testing Library)
- [ ] Integration tests for forms
- [ ] E2E tests (Playwright/Cypress)
- [ ] Authentication flow tests

#### Bug Fixes & Optimization

- [ ] Fix all identified bugs
- [ ] Optimize database queries
- [ ] Add caching where appropriate
- [ ] Optimize image uploads
- [ ] Improve error messages

#### Documentation

- [ ] API documentation (Swagger/OpenAPI)
- [ ] Code comments
- [ ] README updates
- [ ] Deployment guide
- [ ] Environment variables documentation

#### Security Audit

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

## Dependencies

### Backend Dependencies
```json
{
  "dependencies": {
    "express": "^4.18.2",
    "typescript": "^5.3.0",
    "prisma": "^5.7.0",
    "@prisma/client": "^5.7.0",
    "bcrypt": "^5.1.1",
    "jsonwebtoken": "^9.0.2",
    "stripe": "^14.0.0",
    "@sendgrid/mail": "^8.1.0",
    "aws-sdk": "^2.1500.0",
    "redis": "^4.6.0",
    "zod": "^3.22.0",
    "dotenv": "^16.3.0"
  },
  "devDependencies": {
    "@types/express": "^4.17.21",
    "@types/node": "^20.10.0",
    "@types/bcrypt": "^5.0.2",
    "@types/jsonwebtoken": "^9.0.5",
    "jest": "^29.7.0",
    "supertest": "^6.3.3",
    "ts-node": "^10.9.0"
  }
}
```

### Frontend Dependencies
```json
{
  "dependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "react-router-dom": "^6.20.0",
    "@stripe/stripe-js": "^2.3.0",
    "@stripe/react-stripe-js": "^2.4.0",
    "axios": "^1.6.2",
    "react-hook-form": "^7.48.2",
    "zod": "^3.22.0",
    "@hookform/resolvers": "^3.3.2"
  },
  "devDependencies": {
    "@types/react": "^18.2.0",
    "@types/react-dom": "^18.2.0",
    "typescript": "^5.3.0"
  }
}
```

---

## Next Steps After Stage 1

Once Stage 1 is complete:
1. Deploy to staging environment
2. Conduct user acceptance testing
3. Fix any critical issues
4. Begin Stage 2: Mock Interviews System

