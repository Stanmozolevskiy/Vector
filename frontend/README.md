# Vector Frontend

**Version:** 1.0  
**Last Updated:** December 6, 2025

React + TypeScript frontend application for the Vector platform.

## Setup

### Prerequisites
- Node.js 20+ LTS
- npm or yarn

### Installation

```bash
npm install
```

### Development

```bash
npm run dev
```

The application will be available at `http://localhost:5173`

### Build

```bash
npm run build
```

### Environment Variables

Create a `.env` file in the frontend directory:

```
VITE_API_URL=http://localhost:5000/api
```

## Project Structure

```
frontend/src/
├── components/          # Reusable components
│   ├── common/         # Common UI components
│   ├── forms/          # Form components
│   └── layout/        # Layout components
├── pages/              # Page components
│   ├── auth/           # Authentication pages
│   ├── dashboard/      # Dashboard pages
│   └── profile/        # Profile pages
├── hooks/              # Custom React hooks
│   └── useAuth.tsx     # Authentication hook
├── services/           # API services
│   ├── api.ts          # Axios instance
│   └── auth.service.ts # Auth service
├── store/              # State management
├── utils/              # Utility functions
│   └── constants.ts    # Constants
└── App.tsx             # Main app component
```

## Features

- ✅ React 19+ with TypeScript
- ✅ React Router for navigation
- ✅ Tailwind CSS for styling
- ✅ React Hook Form + Zod for form validation
- ✅ Axios for API calls
- ✅ Authentication context with JWT
- ✅ Protected routes
- ✅ Responsive design
- ✅ Reusable Navbar component
- ✅ Profile management with image upload
- ✅ Subscription management UI
- ✅ 25 integration tests (100% passing)

## Testing

```bash
# Run tests
npm test

# Run tests with UI
npm run test:ui

# Run tests with coverage
npm run test:coverage
```

**Test Coverage:**
- 8 Login page tests
- 9 Register page tests
- 8 Profile page tests
- 100% passing

## Available Scripts

- `npm run dev` - Start development server (Vite)
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint
- `npm test` - Run tests (Vitest)
- `npm run test:ui` - Run tests with UI
- `npm run test:coverage` - Run tests with coverage

## Pages

### Authentication
- `/login` - User login
- `/register` - User registration
- `/verify-email` - Email verification (via link)

### Protected Pages
- `/dashboard` - User dashboard
- `/profile` - User profile management
  - General tab - Profile information
  - Privacy tab - Password change
  - Subscription tab - Current subscription
- `/subscriptions` - Subscription plans and selection

## Components

### Layout
- `Navbar` - Reusable navigation bar with user menu

### Forms
- Login form with validation
- Registration form with validation
- Profile update form
- Password change form

## API Integration

The frontend communicates with the backend API via Axios. All API calls are centralized in the `services` directory:

- `api.ts` - Axios instance with interceptors
- `auth.service.ts` - Authentication endpoints
- `user.service.ts` - User management endpoints
- `subscription.service.ts` - Subscription endpoints

## Environment Variables

See [ENVIRONMENT_VARIABLES.md](../ENVIRONMENT_VARIABLES.md) for complete configuration guide.

**Required:**
- `VITE_API_URL` - Backend API base URL

## Documentation

- [API Documentation](../API_DOCUMENTATION.md) - Backend API reference
- [Deployment Guide](../DEPLOYMENT_GUIDE.md) - Deployment procedures
- [Environment Variables](../ENVIRONMENT_VARIABLES.md) - Configuration guide
