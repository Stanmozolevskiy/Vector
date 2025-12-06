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

// Add response interceptor for error handling
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Token expired or invalid
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      window.location.href = '/login';
    }
    
    // Suppress console errors for expected 404s on coach application endpoint
    // This prevents console noise when new users (without applications) visit the profile page
    if (error.response?.status === 404 && 
        error.config?.url?.includes('/coach/my-application')) {
      // This is expected for users without applications - mark it so it won't log to console
      error.isExpected404 = true;
      // Suppress the browser console error by preventing the default error logging
      // Note: Browser will still show the network request in DevTools, but won't show as error
      if (error.config?.suppress404Logging !== false) {
        error.suppressConsoleError = true;
      }
    }
    
    return Promise.reject(error);
  }
);

export default api;

