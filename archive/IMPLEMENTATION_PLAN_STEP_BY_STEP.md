# Stage 1 Implementation Plan - Step by Step

This document provides detailed step-by-step instructions for implementing Stage 1, following the checklist structure.

---

## Week 1: Project Setup & Infrastructure

### Day 1-2: Project Initialization

#### ✅ Backend Setup (COMPLETE)
All backend structure files have been created. Next steps:

#### Frontend Setup

**Step 1: Create React App**
```bash
cd c:\Users\stanm\source\repos\Vecotr
mkdir frontend
cd frontend
npx create-react-app . --template typescript
# OR use Vite (faster):
npm create vite@latest . -- --template react-ts
```

**Step 2: Install Dependencies**
```bash
cd frontend
npm install react-router-dom axios react-hook-form zod @hookform/resolvers
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
```

**Step 3: Configure Tailwind CSS**
- Update `tailwind.config.js`:
```js
content: [
  "./src/**/*.{js,jsx,ts,tsx}",
]
```
- Add to `src/index.css`:
```css
@tailwind base;
@tailwind components;
@tailwind utilities;
```

**Step 4: Set Up Project Structure**
```
frontend/src/
├── components/
│   ├── common/
│   ├── forms/
│   └── layout/
├── pages/
│   ├── auth/
│   ├── dashboard/
│   └── profile/
├── hooks/
│   ├── useAuth.ts
│   └── useApi.ts
├── services/
│   ├── api.ts
│   └── auth.service.ts
├── store/
│   └── authStore.ts
├── utils/
│   └── constants.ts
└── App.tsx
```

**Step 5: Set Up Routing**
```typescript
// src/App.tsx
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { LoginPage } from './pages/auth/LoginPage';
import { RegisterPage } from './pages/auth/RegisterPage';
import { DashboardPage } from './pages/dashboard/DashboardPage';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/dashboard" element={<DashboardPage />} />
      </Routes>
    </BrowserRouter>
  );
}
```

**Step 6: Create API Service**
```typescript
// src/services/api.ts
import axios from 'axios';

const api = axios.create({
  baseURL: process.env.REACT_APP_API_URL || 'http://localhost:5000/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add request interceptor to include JWT token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default api;
```

#### Infrastructure Setup

**Step 1: Create Docker Files**

Create `docker/Dockerfile.backend`:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["backend/Vector.Api/Vector.Api.csproj", "backend/Vector.Api/"]
RUN dotnet restore "backend/Vector.Api/Vector.Api.csproj"
COPY . .
RUN dotnet build "backend/Vector.Api/Vector.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "backend/Vector.Api/Vector.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Vector.Api.dll"]
```

Create `docker/Dockerfile.frontend`:
```dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/build /usr/share/nginx/html
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

Create `docker/docker-compose.yml`:
```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: vector_db
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

  backend:
    build:
      context: ..
      dockerfile: docker/Dockerfile.backend
    ports:
      - "5000:80"
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=vector_db;Username=postgres;Password=postgres
      - ConnectionStrings__Redis=redis:6379
    depends_on:
      - postgres
      - redis

volumes:
  postgres_data:
  redis_data:
```

**Step 2: Create Terraform Structure**
```bash
mkdir -p infrastructure/terraform/modules/{vpc,rds,redis,s3}
```

#### Git Repository

**Step 1: Create .gitignore**
```bash
# Create .gitignore in root
# .NET
backend/**/bin/
backend/**/obj/
backend/**/*.user
backend/**/*.suo

# Node
frontend/node_modules/
frontend/build/
frontend/.env.local

# IDE
.vs/
.vscode/
.idea/

# Environment
.env
.env.local
*.local

# Terraform
infrastructure/terraform/.terraform/
infrastructure/terraform/*.tfstate
infrastructure/terraform/*.tfstate.backup
```

**Step 2: Create develop branch**
```bash
git checkout -b develop
git push -u origin develop
```

---

### Day 3-4: AWS Infrastructure Basics (Terraform)

**Step 1: Create Main Terraform File**
```hcl
# infrastructure/terraform/main.tf
terraform {
  required_version = ">= 1.0"
  
  backend "s3" {
    bucket = "vector-terraform-state"
    key    = "stage1/terraform.tfstate"
    region = "us-east-1"
  }
}

provider "aws" {
  region = var.aws_region
}

module "vpc" {
  source = "./modules/vpc"
  environment = var.environment
  vpc_cidr = "10.0.0.0/16"
}

module "database" {
  source = "./modules/rds"
  environment = var.environment
  vpc_id = module.vpc.vpc_id
  subnet_ids = module.vpc.private_subnet_ids
  instance_class = "db.t3.micro"
}

module "redis" {
  source = "./modules/redis"
  environment = var.environment
  vpc_id = module.vpc.vpc_id
  subnet_ids = module.vpc.private_subnet_ids
}

module "storage" {
  source = "./modules/s3"
  environment = var.environment
}
```

**Step 2: Create VPC Module**
```hcl
# infrastructure/terraform/modules/vpc/main.tf
resource "aws_vpc" "main" {
  cidr_block = var.vpc_cidr
  enable_dns_hostnames = true
  enable_dns_support = true
  
  tags = {
    Name = "${var.environment}-vpc"
  }
}

# Add public and private subnets, internet gateway, NAT gateway, etc.
```

**Step 3: Create RDS Module**
```hcl
# infrastructure/terraform/modules/rds/main.tf
resource "aws_db_instance" "postgres" {
  identifier = "${var.environment}-postgres"
  engine = "postgres"
  engine_version = "15.4"
  instance_class = var.instance_class
  allocated_storage = 20
  
  db_name = "vector_db"
  username = "postgres"
  password = var.db_password
  
  vpc_security_group_ids = [aws_security_group.rds.id]
  db_subnet_group_name = aws_db_subnet_group.main.name
  
  backup_retention_period = 7
  skip_final_snapshot = var.environment == "dev"
}
```

---

### Day 5: Database Schema Setup

**Step 1: Configure Connection String**
Update `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=vector_db;Username=postgres;Password=postgres"
  }
}
```

**Step 2: Create Initial Migration**
```bash
cd backend/Vector.Api
dotnet ef migrations add InitialCreate
```

**Step 3: Review Migration**
Check the generated migration file in `Migrations/` folder

**Step 4: Run Migration**
```bash
dotnet ef database update
```

**Step 5: Test Database Connection**
```bash
# Start PostgreSQL (if using Docker)
docker-compose up -d postgres

# Test connection
dotnet run
# Check if application starts without database errors
```

---

### Day 6-7: CI/CD Pipeline Setup

**Step 1: Create GitHub Actions Directory**
```bash
mkdir -p .github/workflows
```

**Step 2: Create Backend Workflow**
```yaml
# .github/workflows/backend.yml
name: Backend CI/CD

on:
  push:
    branches: [main, develop]
    paths:
      - 'backend/**'
  pull_request:
    branches: [main, develop]
    paths:
      - 'backend/**'

env:
  DOTNET_VERSION: '8.0.x'

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: ./backend/Vector.Api
    
    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_PASSWORD: postgres
          POSTGRES_DB: testdb
        ports:
          - 5432:5432
      
      redis:
        image: redis:7
        ports:
          - 6379:6379
    
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Run tests
        run: dotnet test --no-build --verbosity normal
        env:
          ConnectionStrings__DefaultConnection: Host=localhost;Database=testdb;Username=postgres;Password=postgres
```

**Step 3: Create Frontend Workflow**
```yaml
# .github/workflows/frontend.yml
name: Frontend CI/CD

on:
  push:
    branches: [main, develop]
    paths:
      - 'frontend/**'
  pull_request:
    branches: [main, develop]
    paths:
      - 'frontend/**'

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: ./frontend
    
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '20.x'
          cache: 'npm'
      
      - name: Install dependencies
        run: npm ci
      
      - name: Run linter
        run: npm run lint
      
      - name: Build
        run: npm run build
```

---

## Week 2: Authentication System

### Day 8-9: User Registration

**Step 1: Implement AuthService.RegisterUserAsync**

```csharp
// Services/AuthService.cs
public async Task<User> RegisterUserAsync(RegisterDto dto)
{
    // Check if user exists
    if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
    {
        throw new InvalidOperationException("User with this email already exists");
    }

    // Hash password
    var passwordHash = PasswordHasher.HashPassword(dto.Password);

    // Create user
    var user = new User
    {
        Email = dto.Email,
        PasswordHash = passwordHash,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Role = "student",
        EmailVerified = false
    };

    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    // Generate email verification token
    var token = TokenGenerator.GenerateEmailVerificationToken();
    var emailVerification = new EmailVerification
    {
        UserId = user.Id,
        Token = token,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    _context.EmailVerifications.Add(emailVerification);
    await _context.SaveChangesAsync();

    // Send verification email
    await _emailService.SendVerificationEmailAsync(user.Email, token);

    return user;
}
```

**Step 2: Implement AuthController.Register**

```csharp
// Controllers/AuthController.cs
[HttpPost("register")]
[AllowAnonymous]
public async Task<IActionResult> Register([FromBody] RegisterDto dto)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    try
    {
        var user = await _authService.RegisterUserAsync(dto);
        return Ok(new { message = "Registration successful. Please check your email to verify your account." });
    }
    catch (InvalidOperationException ex)
    {
        return Conflict(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = "An error occurred during registration." });
    }
}
```

**Step 3: Register Services in Program.cs**

```csharp
// Program.cs - Add these lines
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddScoped<IStripeService, StripeService>();

// Add AWS S3 client
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var accessKey = config["AWS:AccessKeyId"];
    var secretKey = config["AWS:SecretAccessKey"];
    var region = config["AWS:Region"] ?? "us-east-1";
    
    return new AmazonS3Client(accessKey, secretKey, RegionEndpoint.GetBySystemName(region));
});
```

**Step 4: Create Registration Form (Frontend)**

```typescript
// frontend/src/pages/auth/RegisterPage.tsx
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import api from '../../services/api';

const registerSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  firstName: z.string().optional(),
  lastName: z.string().optional(),
});

export const RegisterPage = () => {
  const { register, handleSubmit, formState: { errors } } = useForm({
    resolver: zodResolver(registerSchema),
  });

  const onSubmit = async (data: any) => {
    try {
      await api.post('/auth/register', data);
      alert('Registration successful! Please check your email.');
    } catch (error: any) {
      alert(error.response?.data?.error || 'Registration failed');
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="max-w-md mx-auto mt-8">
      <input {...register('email')} type="email" placeholder="Email" />
      {errors.email && <span>{errors.email.message}</span>}
      
      <input {...register('password')} type="password" placeholder="Password" />
      {errors.password && <span>{errors.password.message}</span>}
      
      <button type="submit">Register</button>
    </form>
  );
};
```

---

### Day 10-11: Login System

**Step 1: Implement AuthService.LoginAsync**

```csharp
// Services/AuthService.cs
public async Task<string> LoginAsync(LoginDto dto)
{
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
    
    if (user == null || !PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash))
    {
        throw new UnauthorizedAccessException("Invalid email or password");
    }

    if (!user.EmailVerified)
    {
        throw new InvalidOperationException("Please verify your email before logging in");
    }

    // Generate tokens
    var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Role);
    var refreshToken = _jwtService.GenerateRefreshToken(user.Id);

    // Store refresh token in Redis
    var redis = _connectionMultiplexer.GetDatabase();
    await redis.StringSetAsync($"refresh_token:{user.Id}", refreshToken, TimeSpan.FromDays(7));

    // Return tokens (you might want to return both in a DTO)
    return accessToken;
}
```

**Step 2: Implement AuthController.Login**

```csharp
[HttpPost("login")]
[AllowAnonymous]
public async Task<IActionResult> Login([FromBody] LoginDto dto)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    try
    {
        var accessToken = await _authService.LoginAsync(dto);
        // Also get refresh token from service
        return Ok(new { accessToken, tokenType = "Bearer" });
    }
    catch (UnauthorizedAccessException ex)
    {
        return Unauthorized(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = "An error occurred during login." });
    }
}
```

**Step 3: Create Auth Context (Frontend)**

```typescript
// frontend/src/hooks/useAuth.ts
import { createContext, useContext, useState, useEffect } from 'react';
import api from '../services/api';

interface AuthContextType {
  user: any | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [user, setUser] = useState<any | null>(null);

  const login = async (email: string, password: string) => {
    const response = await api.post('/auth/login', { email, password });
    localStorage.setItem('accessToken', response.data.accessToken);
    // Fetch user profile
    const userResponse = await api.get('/users/me');
    setUser(userResponse.data);
  };

  const logout = () => {
    localStorage.removeItem('accessToken');
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, login, logout, isAuthenticated: !!user }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');
  return context;
};
```

---

### Day 12-13: Email Verification

**Step 1: Implement Email Verification Endpoint**

```csharp
[HttpGet("verify-email")]
[AllowAnonymous]
public async Task<IActionResult> VerifyEmail([FromQuery] string token)
{
    try
    {
        var verified = await _authService.VerifyEmailAsync(token);
        if (verified)
        {
            return Ok(new { message = "Email verified successfully" });
        }
        return BadRequest(new { error = "Invalid or expired token" });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = "An error occurred during verification." });
    }
}
```

**Step 2: Implement VerifyEmailAsync in AuthService**

```csharp
public async Task<bool> VerifyEmailAsync(string token)
{
    var verification = await _context.EmailVerifications
        .Include(v => v.User)
        .FirstOrDefaultAsync(v => v.Token == token && !v.IsUsed);

    if (verification == null || verification.ExpiresAt < DateTime.UtcNow)
    {
        return false;
    }

    verification.IsUsed = true;
    verification.User.EmailVerified = true;
    
    await _context.SaveChangesAsync();
    return true;
}
```

---

### Day 14: Password Reset

**Step 1: Implement Forgot Password**

```csharp
[HttpPost("forgot-password")]
[AllowAnonymous]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
{
    try
    {
        await _authService.ForgotPasswordAsync(dto.Email);
        // Always return success to prevent email enumeration
        return Ok(new { message = "If an account exists, a password reset email has been sent." });
    }
    catch
    {
        return Ok(new { message = "If an account exists, a password reset email has been sent." });
    }
}
```

**Step 2: Implement Reset Password**

```csharp
[HttpPost("reset-password")]
[AllowAnonymous]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    try
    {
        var success = await _authService.ResetPasswordAsync(dto);
        if (success)
        {
            return Ok(new { message = "Password reset successfully" });
        }
        return BadRequest(new { error = "Invalid or expired token" });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = "An error occurred during password reset." });
    }
}
```

---

## Week 3: User Profile & Roles

### Day 15-16: User Profile Management

**Step 1: Implement Get Profile Endpoint**

```csharp
[HttpGet("me")]
public async Task<IActionResult> GetProfile()
{
    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var user = await _userService.GetUserByIdAsync(userId);
    
    if (user == null)
    {
        return NotFound();
    }

    return Ok(new
    {
        id = user.Id,
        email = user.Email,
        firstName = user.FirstName,
        lastName = user.LastName,
        bio = user.Bio,
        profilePictureUrl = user.ProfilePictureUrl,
        role = user.Role
    });
}
```

**Step 2: Implement Update Profile**

```csharp
[HttpPut("me")]
public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
{
    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var user = await _userService.UpdateProfileAsync(userId, dto);
    return Ok(user);
}
```

**Step 3: Implement Profile Picture Upload**

```csharp
[HttpPost("me/profile-picture")]
public async Task<IActionResult> UploadProfilePicture(IFormFile file)
{
    if (file == null || file.Length == 0)
    {
        return BadRequest(new { error = "No file uploaded" });
    }

    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var key = $"profile-pictures/{userId}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
    
    var url = await _s3Service.UploadFileAsync(file.OpenReadStream(), key, file.ContentType);
    await _userService.UpdateProfilePictureAsync(userId, url);
    
    return Ok(new { profilePictureUrl = url });
}
```

---

## Week 4: Subscription System

### Day 21-22: Subscription Plans

**Step 1: Define Subscription Plans**

```csharp
// Create a constants file or configuration
public static class SubscriptionPlans
{
    public static readonly List<SubscriptionPlanDto> Plans = new()
    {
        new SubscriptionPlanDto
        {
            Id = "monthly",
            Name = "Monthly Plan",
            Price = 29.99m,
            Interval = "month",
            StripePriceId = "price_monthly_xxx" // Get from Stripe dashboard
        },
        new SubscriptionPlanDto
        {
            Id = "annual",
            Name = "Annual Plan",
            Price = 299.99m,
            Interval = "year",
            StripePriceId = "price_annual_xxx"
        }
    };
}
```

**Step 2: Implement Get Plans Endpoint**

```csharp
[HttpGet("plans")]
[AllowAnonymous]
public IActionResult GetPlans()
{
    return Ok(SubscriptionPlans.Plans);
}
```

---

### Day 23-24: Stripe Integration

**Step 1: Set Up Stripe Account**
1. Go to https://stripe.com
2. Create account
3. Get API keys from Dashboard → Developers → API keys
4. Create products and prices in Dashboard

**Step 2: Implement Subscription Creation**

```csharp
[HttpPost("subscribe")]
[Authorize]
public async Task<IActionResult> Subscribe([FromBody] SubscribeDto dto)
{
    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var user = await _userService.GetUserByIdAsync(userId);
    
    // Get or create Stripe customer
    var customer = await _stripeService.CreateCustomerAsync(user!.Email, $"{user.FirstName} {user.LastName}");
    
    // Create subscription
    var subscription = await _stripeService.CreateSubscriptionAsync(customer.Id, dto.PaymentMethodId);
    
    // Save to database
    var dbSubscription = new Subscription
    {
        UserId = userId,
        PlanType = dto.PlanId,
        StripeSubscriptionId = subscription.Id,
        StripeCustomerId = customer.Id,
        Status = "active",
        CurrentPeriodStart = DateTime.UtcNow,
        CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1)
    };
    
    _context.Subscriptions.Add(dbSubscription);
    await _context.SaveChangesAsync();
    
    return Ok(new { subscriptionId = subscription.Id });
}
```

**Step 3: Implement Webhook Handler**

```csharp
[HttpPost("webhook")]
[AllowAnonymous]
public async Task<IActionResult> HandleWebhook()
{
    var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
    var signature = Request.Headers["Stripe-Signature"].ToString();
    
    try
    {
        var stripeEvent = await _stripeService.ConstructWebhookEventAsync(json, signature);
        await _stripeService.HandleWebhookEventAsync(stripeEvent);
        return Ok(new { received = true });
    }
    catch (StripeException ex)
    {
        return BadRequest(new { error = ex.Message });
    }
}
```

---

## Testing Checklist

### Unit Tests

**Step 1: Create Test Project**
```bash
cd backend
dotnet new xunit -n Vector.Api.Tests
dotnet sln add Vector.Api.Tests/Vector.Api.Tests.csproj
cd Vector.Api.Tests
dotnet add reference ../Vector.Api/Vector.Api.csproj
dotnet add package Moq
dotnet add package FluentAssertions
```

**Step 2: Write Sample Test**
```csharp
// Tests/Services/AuthServiceTests.cs
public class AuthServiceTests
{
    [Fact]
    public async Task RegisterUserAsync_ShouldCreateUser()
    {
        // Arrange
        var mockContext = new Mock<ApplicationDbContext>();
        var service = new AuthService(mockContext.Object);
        var dto = new RegisterDto { Email = "test@example.com", Password = "Password123!" };
        
        // Act
        var user = await service.RegisterUserAsync(dto);
        
        // Assert
        Assert.NotNull(user);
        Assert.Equal(dto.Email, user.Email);
    }
}
```

---

## Next Steps After Each Implementation

1. **Test the endpoint** using Postman or Swagger
2. **Update the checklist** to mark completed items
3. **Commit changes** to Git with descriptive messages
4. **Move to next task** in the checklist

---

## Quick Reference Commands

```bash
# Run backend
cd backend/Vector.Api
dotnet run

# Run frontend
cd frontend
npm start

# Create migration
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update

# Run tests
dotnet test

# Build project
dotnet build
```

---

This plan provides step-by-step instructions for each checklist item. Follow it sequentially, marking items as complete as you implement them.

