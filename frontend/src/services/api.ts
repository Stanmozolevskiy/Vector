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

    // Suppress console errors for expected 404s on coach application endpoint
    if (error.response?.status === 404 && 
        error.config?.url?.includes('/coach/my-application')) {
      error.isExpected404 = true;
      if (error.config?.suppress404Logging !== false) {
        error.suppressConsoleError = true;
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
        
        // Update the original request with new token
        originalRequest.headers.Authorization = `Bearer ${response.accessToken}`;
        
        // Process queued requests
        processQueue(null, response.accessToken);
        isRefreshing = false;
        
        // Retry the original request
        return api(originalRequest);
      } catch (refreshError) {
        // Refresh failed - logout user and redirect to login with return URL
        processQueue(refreshError, null);
        isRefreshing = false;
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
        window.location.href = `/login?returnUrl=${returnUrl}`;
        return Promise.reject(refreshError);
      }
    }
    
    return Promise.reject(error);
  }
);

export default api;

