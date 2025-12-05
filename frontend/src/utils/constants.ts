export const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export const ROUTES = {
  HOME: '/',
  LOGIN: '/login',
  REGISTER: '/register',
  DASHBOARD: '/dashboard',
  FORGOT_PASSWORD: '/forgot-password',
  VERIFY_EMAIL: '/verify-email',
  RESET_PASSWORD: '/reset-password',
  RESEND_VERIFICATION: '/resend-verification',
  PROFILE: '/profile',
  COACH_APPLY: '/coach/apply',
  ADMIN: '/admin',
};
