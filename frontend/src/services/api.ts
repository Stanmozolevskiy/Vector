import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
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

// Track if we're currently refreshing to avoid infinite loops
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: unknown) => void;
  reject: (reason?: unknown) => void;
}> = [];

// Proactive token refresh - refresh token before it expires
let tokenRefreshInterval: ReturnType<typeof setInterval> | null = null;

const startProactiveTokenRefresh = () => {
  // Clear existing interval if any
  if (tokenRefreshInterval) {
    clearInterval(tokenRefreshInterval);
  }

  // Refresh token every 14 minutes (tokens typically expire in 15 minutes)
  // This ensures token is refreshed before expiration
  tokenRefreshInterval = setInterval(async () => {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) {
      return; // No refresh token, can't refresh
    }

    try {
      const { authService } = await import('./auth.service');
      const response = await authService.refreshToken();
      localStorage.setItem('accessToken', response.accessToken);
      if (response.refreshToken) {
        localStorage.setItem('refreshToken', response.refreshToken);
      }
      console.log('Token refreshed proactively');
    } catch (error) {
      // If refresh fails, clear tokens and stop interval
      console.error('Proactive token refresh failed:', error);
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      if (tokenRefreshInterval) {
        clearInterval(tokenRefreshInterval);
        tokenRefreshInterval = null;
      }
    }
  }, 14 * 60 * 1000); // 14 minutes
};

// Start proactive refresh if user is logged in
if (localStorage.getItem('refreshToken')) {
  startProactiveTokenRefresh();
}

// Listen for storage changes to start/stop proactive refresh
window.addEventListener('storage', (e) => {
  if (e.key === 'refreshToken') {
    if (e.newValue) {
      startProactiveTokenRefresh();
    } else {
      if (tokenRefreshInterval) {
        clearInterval(tokenRefreshInterval);
        tokenRefreshInterval = null;
      }
    }
  }
});

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

// Add response interceptor for error handling and token refresh
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // Suppress console errors for expected 404s on coach application and code-drafts endpoints
    if (error.response?.status === 404 && 
        (error.config?.url?.includes('/coach/my-application') ||
         error.config?.url?.includes('/code-drafts'))) {
      error.isExpected404 = true;
      error.suppressConsoleError = true;
      // Prevent axios from logging this error
      if (error.config) {
        error.config.suppressErrorLog = true;
      }
      return Promise.reject(error);
    }

    // Handle 401 Unauthorized - attempt token refresh
    // Skip refresh if this is already a refresh token request to avoid infinite loops
    if (error.response?.status === 401 && !originalRequest._retry && 
        !originalRequest.url?.includes('/auth/refresh')) {
      if (isRefreshing) {
        // If already refreshing, queue this request
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return api(originalRequest);
          })
          .catch((err) => {
            return Promise.reject(err);
          });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = localStorage.getItem('refreshToken');
      
      if (!refreshToken) {
        // No refresh token available - logout user and redirect to login with return URL
        processQueue(new Error('No refresh token'), null);
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
        window.location.href = `/login?returnUrl=${returnUrl}`;
        return Promise.reject(error);
      }

      try {
        const { authService } = await import('./auth.service');
        const response = await authService.refreshToken();
        
        localStorage.setItem('accessToken', response.accessToken);
        localStorage.setItem('refreshToken', response.refreshToken);
        
        // Restart proactive refresh if it was stopped
        if (!tokenRefreshInterval && response.refreshToken) {
          startProactiveTokenRefresh();
        }
        
        // Update the original request with new token
        originalRequest.headers.Authorization = `Bearer ${response.accessToken}`;
        
        // Process queued requests
        processQueue(null, response.accessToken);
        isRefreshing = false;
        
        // Retry the original request
        return api(originalRequest);
      } catch (refreshError: any) {
        // Check if refresh failed due to expired refresh token (401) or invalid token (400)
        // Only redirect if the refresh token itself is invalid/expired
        const isRefreshTokenExpired = refreshError?.response?.status === 401 || 
                                     refreshError?.response?.status === 400 ||
                                     refreshError?.message?.includes('expired') ||
                                     refreshError?.message?.includes('invalid');
        
        if (isRefreshTokenExpired) {
          // Refresh token expired - logout user and redirect to login with return URL
          processQueue(refreshError, null);
          isRefreshing = false;
          localStorage.removeItem('accessToken');
          localStorage.removeItem('refreshToken');
          const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
          window.location.href = `/login?returnUrl=${returnUrl}`;
          return Promise.reject(refreshError);
        } else {
          // Network error or other issue - retry the original request with current token
          // Don't redirect, let the user continue working
          processQueue(null, null);
          isRefreshing = false;
          console.warn('Token refresh failed but not due to expiration, retrying original request');
          // Retry original request - it might work if token is still valid
          return api(originalRequest);
        }
      }
    }
    
    return Promise.reject(error);
  }
);

export default api;

