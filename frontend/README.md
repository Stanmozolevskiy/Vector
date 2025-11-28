# Vector Frontend

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

- ✅ React 18+ with TypeScript
- ✅ React Router for navigation
- ✅ Tailwind CSS for styling
- ✅ React Hook Form + Zod for form validation
- ✅ Axios for API calls
- ✅ Authentication context
- ✅ Protected routes
- ✅ Responsive design

## Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint

## Next Steps

1. Implement remaining authentication pages (verify email, reset password)
2. Create profile management pages
3. Create subscription management pages
4. Add more components and pages as needed
