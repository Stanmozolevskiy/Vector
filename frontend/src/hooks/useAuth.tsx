import { createContext, useContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';
import api from '../services/api';
import { authService } from '../services/auth.service';
import type { LoginData, RegisterData } from '../services/auth.service';

interface User {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  bio?: string;
  phoneNumber?: string;
  location?: string;
  role: string;
  profilePictureUrl?: string;
  emailVerified: boolean;
  createdAt: string;
}

interface AuthContextType {
  user: User | null;
  login: (data: LoginData) => Promise<void>;
  register: (data: RegisterData) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
  isAuthenticated: boolean;
  isLoading: boolean;
  hasRole: (role: string | string[]) => boolean;
  isAdmin: boolean;
  isCoach: boolean;
  isStudent: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Check if user is already logged in
    const token = localStorage.getItem('accessToken');
    if (token) {
      fetchUser();
    } else {
      setIsLoading(false);
    }
  }, []);

  const fetchUser = async () => {
    try {
      const response = await api.get<User>('/users/me');
      setUser(response.data);
    } catch (error) {
      // Clear invalid tokens on error
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      // Don't redirect here - let the API interceptor handle it
      // This prevents double redirects
    } finally {
      setIsLoading(false);
    }
  };

  const login = async (data: LoginData) => {
    const response = await authService.login(data);
    localStorage.setItem('accessToken', response.accessToken);
    if (response.refreshToken) {
      localStorage.setItem('refreshToken', response.refreshToken);
      // Trigger storage event to start proactive token refresh
      if (typeof window !== 'undefined') {
        window.dispatchEvent(new StorageEvent('storage', {
          key: 'refreshToken',
          newValue: response.refreshToken,
          storageArea: localStorage
        }));
      }
    }
    localStorage.setItem('tokenType', response.tokenType);
    await fetchUser();
  };

  const register = async (data: RegisterData) => {
    await authService.register(data);
  };

  const logout = async () => {
    try {
      await authService.logout();
    } catch {
      // Even if API call fails, clear local storage
    } finally {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      setUser(null);
    }
  };

  const refreshUser = async () => {
    const token = localStorage.getItem('accessToken');
    if (token) {
      await fetchUser();
    }
  };

  // Role checking functions
  const hasRole = (role: string | string[]): boolean => {
    if (!user) return false;
    const roles = Array.isArray(role) ? role : [role];
    return roles.some(r => user.role.toLowerCase() === r.toLowerCase());
  };

  const value = {
    user,
    login,
    register,
    logout,
    refreshUser,
    isAuthenticated: !!user,
    isLoading,
    hasRole,
    isAdmin: user?.role.toLowerCase() === 'admin',
    isCoach: user?.role.toLowerCase() === 'coach',
    isStudent: user?.role.toLowerCase() === 'student',
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};

// eslint-disable-next-line react-refresh/only-export-components
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};

