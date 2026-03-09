import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
});

// Track if we're currently refreshing to avoid infinite loops
let isRefreshing = false;
let refreshPromise: Promise<string> | null = null;
let failedQueue: Array<{
  resolve: (value?: unknown) => void;
  reject: (reason?: unknown) => void;
}> = [];

// Proactive token refresh - refresh token before access token expires
let tokenRefreshTimeout: ReturnType<typeof setTimeout> | null = null;
let backgroundRefreshInterval: ReturnType<typeof setInterval> | null = null;
let consecutiveRefreshFailures = 0;

const getJwtExpiryMs = (jwt: string | null): number | null => {
  if (!jwt) return null;
  const parts = jwt.split('.');
  if (parts.length < 2) return null;
  try {
    const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
    const exp = typeof payload?.exp === 'number' ? payload.exp : null;
    if (!exp) return null;
    return exp * 1000;
  } catch {
    return null;
  }
};

const stopProactiveTokenRefresh = () => {
  if (tokenRefreshTimeout) {
    clearTimeout(tokenRefreshTimeout);
    tokenRefreshTimeout = null;
  }
  if (backgroundRefreshInterval) {
    clearInterval(backgroundRefreshInterval);
    backgroundRefreshInterval = null;
  }
  consecutiveRefreshFailures = 0;
};

const refreshAccessToken = async (): Promise<string> => {
  const refreshToken = localStorage.getItem('refreshToken');
  if (!refreshToken) {
    console.error('[TokenRefresh] No refresh token in localStorage');
    throw new Error('No refresh token');
  }

  console.log('[TokenRefresh] Refreshing access token...');
  
  try {
    const { authService } = await import('./auth.service');
    const response = await authService.refreshToken();

    console.log('[TokenRefresh] Refresh successful, updating tokens in localStorage');
    
    // Update tokens atomically to prevent race conditions
    localStorage.setItem('accessToken', response.accessToken);
    if (response.refreshToken) {
      localStorage.setItem('refreshToken', response.refreshToken);
    }

    // Re-arm the proactive scheduler based on the new token
    startProactiveTokenRefresh();

    return response.accessToken;
  } catch (error: any) {
    console.error('[TokenRefresh] Refresh failed:', {
      status: error?.response?.status,
      message: error?.message,
      data: error?.response?.data
    });
    throw error;
  }
};

const ensureFreshAccessToken = async (minValidityMs: number = 2 * 60 * 1000): Promise<string | null> => {
  const refreshToken = localStorage.getItem('refreshToken');
  if (!refreshToken) return null;

  const accessToken = localStorage.getItem('accessToken');
  const expMs = getJwtExpiryMs(accessToken);
  const now = Date.now();

  // If we can't parse expiry, fall back to best-effort: keep current token.
  if (!expMs || !accessToken) return accessToken;

  const remainingMs = expMs - now;
  
  // Log token status for debugging
  const remainingMinutes = Math.floor(remainingMs / 60000);
  if (remainingMs < minValidityMs) {
    console.log(`[TokenRefresh] Token expiring in ${remainingMinutes} minutes, refreshing proactively`);
  }
  
  if (remainingMs > minValidityMs) return accessToken;

  // Deduplicate refresh across requests/timers
  if (refreshPromise) {
    try {
      return await refreshPromise;
    } catch {
      return localStorage.getItem('accessToken');
    }
  }

  refreshPromise = (async () => {
    try {
      const newToken = await refreshAccessToken();
      console.log('[TokenRefresh] Successfully refreshed token');
      return newToken;
    } catch (error) {
      console.error('[TokenRefresh] Failed to refresh token:', error);
      throw error;
    } finally {
      refreshPromise = null;
    }
  })();

  try {
    return await refreshPromise;
  } catch {
    return localStorage.getItem('accessToken');
  }
};

// Add request interceptor to include JWT token (and refresh on-demand if near expiry).
api.interceptors.request.use(async (config) => {
  const url = String(config.url || '');
  // Never attempt pre-refresh for auth endpoints
  const isAuthEndpoint =
    url.includes('/auth/login') ||
    url.includes('/auth/register') ||
    url.includes('/auth/refresh') ||
    url.includes('/auth/logout');

  if (!isAuthEndpoint) {
    await ensureFreshAccessToken(2 * 60 * 1000);
  }

  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  // For FormData (file uploads): let the browser set multipart/form-data with boundary.
  // For everything else: default to application/json.
  if (!(config.data instanceof FormData)) {
    if (!config.headers['Content-Type']) {
      config.headers['Content-Type'] = 'application/json';
    }
  }

  return config;
});

const startProactiveTokenRefresh = () => {
  // Clear existing timer if any
  stopProactiveTokenRefresh();

  const scheduleNext = () => {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) return;

    const accessToken = localStorage.getItem('accessToken');
    const expMs = getJwtExpiryMs(accessToken);

    // Default: check again in 1 minute if we can't parse expiry
    const defaultDelayMs = 1 * 60 * 1000;

    // Refresh 2 minutes before access token expiry (was 60s - too close)
    const refreshLeadMs = 2 * 60 * 1000;
    const now = Date.now();
    const delayMs = expMs ? Math.max(5_000, expMs - now - refreshLeadMs) : defaultDelayMs;

    tokenRefreshTimeout = setTimeout(async () => {
      const currentRefreshToken = localStorage.getItem('refreshToken');
      if (!currentRefreshToken) return;

      try {
        await refreshAccessToken();
        consecutiveRefreshFailures = 0;
        scheduleNext();
      } catch (error: any) {
        // Only force logout when refresh token is actually invalid/expired.
        const status = error?.response?.status;
        const isRefreshTokenExpired = status === 401 || status === 400;

        if (isRefreshTokenExpired) {
          console.debug('Proactive token refresh failed (refresh token expired)');
          localStorage.removeItem('accessToken');
          localStorage.removeItem('refreshToken');
          stopProactiveTokenRefresh();
          return;
        }

        // Transient failures (network/server) should NOT log the user out.
        consecutiveRefreshFailures += 1;
        // More aggressive retry - max 2 minutes instead of 5
        const backoffMs = Math.min(2 * 60 * 1000, 15_000 * consecutiveRefreshFailures);
        console.warn(`[TokenRefresh] Proactive refresh failed (attempt ${consecutiveRefreshFailures}), will retry in ${backoffMs/1000}s:`, error);

        tokenRefreshTimeout = setTimeout(() => {
          scheduleNext();
        }, backoffMs);
      }
    }, delayMs);
  };

  scheduleNext();

  // Background refresh fallback: browsers throttle timers in inactive tabs.
  // This runs a light check every 2 minutes and refreshes if token is near expiry.
  // Reduced interval to catch token expiry more quickly for inactive users.
  backgroundRefreshInterval = setInterval(() => {
    const rt = localStorage.getItem('refreshToken');
    if (!rt) {
      console.log('[TokenRefresh] No refresh token found in background check');
      return;
    }
    // Keep at least 3 minutes of validity when possible.
    ensureFreshAccessToken(3 * 60 * 1000).catch((error) => {
      console.error('[TokenRefresh] Background refresh failed:', error);
    });
  }, 2 * 60 * 1000);
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
      stopProactiveTokenRefresh();
    }
  }
});

// Wake-up refresh: when user returns to a background tab, refresh if token is near expiry.
const wakeRefresh = () => {
  const refreshToken = localStorage.getItem('refreshToken');
  if (!refreshToken) return;
  
  console.log('[TokenRefresh] Wake event triggered, checking token validity');
  
  // More aggressive: refresh if less than 10 minutes remaining
  ensureFreshAccessToken(10 * 60 * 1000).catch((error) => {
    console.error('[TokenRefresh] Wake refresh failed:', error);
  });
};

document.addEventListener('visibilitychange', () => {
  if (document.visibilityState !== 'visible') return;
  wakeRefresh();
});

window.addEventListener('focus', () => wakeRefresh());
window.addEventListener('online', () => wakeRefresh());

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
            if (!token || typeof token !== 'string') {
              return Promise.reject(new Error('Token refresh failed'));
            }
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
        console.warn('[Auth] No refresh token found, redirecting to login');
        processQueue(new Error('No refresh token'), null);
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
        window.location.href = `/login?returnUrl=${returnUrl}`;
        return Promise.reject(error);
      }
      
      console.log('[Auth] Attempting token refresh due to 401 response');

      try {
        const { authService } = await import('./auth.service');
        const response = await authService.refreshToken();
        
        console.log('[Auth] Token refresh successful');
        localStorage.setItem('accessToken', response.accessToken);
        localStorage.setItem('refreshToken', response.refreshToken);
        
        // Restart proactive refresh if it was stopped
        if (!tokenRefreshTimeout && response.refreshToken) {
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
          console.warn('[Auth] Refresh token expired or invalid, redirecting to login', {
            status: refreshError?.response?.status,
            message: refreshError?.message
          });
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
          const currentToken = localStorage.getItem('accessToken');
          if (currentToken) {
            processQueue(null, currentToken);
          } else {
            processQueue(refreshError, null);
          }
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

