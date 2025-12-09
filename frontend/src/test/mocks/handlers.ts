import { http, HttpResponse } from 'msw';

const API_BASE_URL = 'http://localhost:5000/api';

export const handlers = [
  // Auth endpoints
  http.post(`${API_BASE_URL}/auth/register`, async ({ request }) => {
    const body = await request.json() as { email: string; password: string; firstName?: string; lastName?: string };
    
    if (body.email === 'existing@example.com') {
      return HttpResponse.json(
        { error: 'A user with this email already exists.' },
        { status: 400 }
      );
    }
    
    return HttpResponse.json({
      message: 'Registration successful. Please check your email to verify your account.',
      userId: '123e4567-e89b-12d3-a456-426614174000',
    }, { status: 201 });
  }),

  http.post(`${API_BASE_URL}/auth/login`, async ({ request }) => {
    const body = await request.json() as { email: string; password: string };
    
    if (body.email === 'unverified@example.com') {
      return HttpResponse.json(
        { error: 'Please verify your email before logging in.' },
        { status: 400 }
      );
    }
    
    if (body.email === 'wrong@example.com' || body.password !== 'Password123!') {
      return HttpResponse.json(
        { error: 'Invalid email or password.' },
        { status: 401 }
      );
    }
    
    return HttpResponse.json({
      token: 'mock-jwt-token',
      refreshToken: 'mock-refresh-token',
      expiresIn: 3600,
      tokenType: 'Bearer',
      user: {
        id: '123e4567-e89b-12d3-a456-426614174000',
        email: body.email,
        firstName: 'Test',
        lastName: 'User',
        role: 'student',
        emailVerified: true,
      },
    }, { status: 200 });
  }),

  http.get(`${API_BASE_URL}/auth/me`, () => {
    return HttpResponse.json({
      id: '123e4567-e89b-12d3-a456-426614174000',
      email: 'test@example.com',
      firstName: 'Test',
      lastName: 'User',
      role: 'student',
      emailVerified: true,
      bio: '',
      phoneNumber: '',
      location: '',
    }, { status: 200 });
  }),

  // User endpoints - profile
  http.get(`${API_BASE_URL}/users/me`, () => {
    return HttpResponse.json({
      id: '123e4567-e89b-12d3-a456-426614174000',
      email: 'test@example.com',
      firstName: 'Test',
      lastName: 'User',
      role: 'student',
      emailVerified: true,
      bio: '',
      phoneNumber: '',
      location: '',
    }, { status: 200 });
  }),

  // User endpoints
  http.put(`${API_BASE_URL}/users/profile`, async ({ request }) => {
    const body = await request.json() as { firstName?: string; lastName?: string; bio?: string; phoneNumber?: string; location?: string };
    
    return HttpResponse.json({
      id: '123e4567-e89b-12d3-a456-426614174000',
      email: 'test@example.com',
      firstName: body.firstName || 'Test',
      lastName: body.lastName || 'User',
      bio: body.bio || '',
      phoneNumber: body.phoneNumber || '',
      location: body.location || '',
      role: 'student',
      emailVerified: true,
    }, { status: 200 });
  }),

  // User profile update endpoint (alternative path)
  http.put(`${API_BASE_URL}/users/me`, async ({ request }) => {
    const body = await request.json() as { firstName?: string; lastName?: string; bio?: string; phoneNumber?: string; location?: string };
    
    return HttpResponse.json({
      id: '123e4567-e89b-12d3-a456-426614174000',
      email: 'test@example.com',
      firstName: body.firstName || 'Test',
      lastName: body.lastName || 'User',
      bio: body.bio || '',
      phoneNumber: body.phoneNumber || '',
      location: body.location || '',
      role: 'student',
      emailVerified: true,
    }, { status: 200 });
  }),

  http.put(`${API_BASE_URL}/users/change-password`, async ({ request }) => {
    const body = await request.json() as { currentPassword: string; newPassword: string };
    
    if (body.currentPassword !== 'CurrentPassword123!') {
      return HttpResponse.json(
        { error: 'Current password is incorrect.' },
        { status: 400 }
      );
    }
    
    return HttpResponse.json({
      message: 'Password changed successfully.',
    }, { status: 200 });
  }),

  // Subscription endpoints
  http.get(`${API_BASE_URL}/subscriptions/plans`, () => {
    return HttpResponse.json([
      {
        id: 'free',
        name: 'Free Plan',
        description: 'Get started with basic features.',
        price: 0,
        currency: 'USD',
        billingPeriod: 'free',
        features: ['Limited course access', 'Basic features'],
        isPopular: false,
      },
      {
        id: 'monthly',
        name: 'Monthly Plan',
        description: 'Full access for one month.',
        price: 29.99,
        currency: 'USD',
        billingPeriod: 'monthly',
        features: ['Unlimited courses', 'Mock interviews'],
        isPopular: false,
      },
      {
        id: 'annual',
        name: 'Annual Plan',
        description: 'Full access for one year.',
        price: 299.99,
        currency: 'USD',
        billingPeriod: 'annual',
        features: ['Unlimited courses', 'Mock interviews', 'Priority support'],
        isPopular: true,
      },
    ], { status: 200 });
  }),

  http.get(`${API_BASE_URL}/subscriptions/me`, () => {
    return HttpResponse.json({
      id: 'sub-123',
      planType: 'free',
      status: 'active',
      currentPeriodStart: new Date().toISOString(),
      currentPeriodEnd: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString(),
      price: 0,
      currency: 'USD',
      createdAt: new Date().toISOString(),
      plan: {
        id: 'free',
        name: 'Free Plan',
        description: 'Get started with basic features.',
        price: 0,
        currency: 'USD',
        billingPeriod: 'free',
        features: ['Limited course access', 'Basic features'],
        isPopular: false,
      },
    }, { status: 200 });
  }),

  http.put(`${API_BASE_URL}/subscriptions/update`, async ({ request }) => {
    const body = await request.json() as { planId: string };
    
    return HttpResponse.json({
      id: 'sub-123',
      planType: body.planId,
      status: 'active',
      currentPeriodStart: new Date().toISOString(),
      currentPeriodEnd: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(),
      price: body.planId === 'monthly' ? 29.99 : 299.99,
      currency: 'USD',
      createdAt: new Date().toISOString(),
    }, { status: 200 });
  }),

  // Coach endpoints
  http.get(`${API_BASE_URL}/coach/my-application`, () => {
    return HttpResponse.json(
      { error: 'Not Found' },
      { status: 404 }
    );
  }),

  // Profile picture upload
  http.post(`${API_BASE_URL}/users/me/profile-picture`, () => {
    return HttpResponse.json({
      profilePictureUrl: 'https://example.com/profile.jpg',
    }, { status: 200 });
  }),

  // Change password endpoint (alternative path)
  http.put(`${API_BASE_URL}/users/me/password`, async ({ request }) => {
    const body = await request.json() as { currentPassword: string; newPassword: string; confirmPassword: string };
    
    if (body.currentPassword !== 'CurrentPassword123!') {
      return HttpResponse.json(
        { error: 'Current password is incorrect.' },
        { status: 400 }
      );
    }
    
    return HttpResponse.json({
      message: 'Password changed successfully.',
    }, { status: 200 });
  }),
];

