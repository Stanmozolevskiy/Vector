import { createContext, useContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';
import api from '../services/api';
import { authService } from '../services/auth.service';
import type { LoginData, RegisterData } from '../services/auth.service';
import { tokenStorage } from '../utils/tokenStorage';

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
  notifyInterviewReminders?: boolean;
  notifyWeeklyProgress?: boolean;
  notifyNewQuestions?: boolean;
}

interface AuthContextType {
  user: User | null;
  login: (data: LoginData & { remember?: boolean }) => Promise<void>;
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
    const token = tokenStorage.getAccessToken();
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
      tokenStorage.clearTokens();
      // Don't redirect here - let the API interceptor handle it
    } finally {
      setIsLoading(false);
    }
  };

  const login = async (data: LoginData & { remember?: boolean }) => {
    const response = await authService.login({ email: data.email, password: data.password });
    const remember = data.remember ?? true;
    tokenStorage.setTokens(
      response.accessToken,
      response.refreshToken,
      response.tokenType,
      remember
    );
    if (response.refreshToken && typeof window !== 'undefined') {
      const storage = remember ? localStorage : sessionStorage;
      window.dispatchEvent(new StorageEvent('storage', {
        key: 'refreshToken',
        newValue: response.refreshToken,
        storageArea: storage
      }));
    }
    await fetchUser();
  };

  const register = async (data: RegisterData) => {
    await authService.register(data);
  };

  const logout = async () => {
    try {
      await authService.logout();
    } catch {
      // Even if API call fails, clear tokens
    } finally {
      tokenStorage.clearTokens();
      setUser(null);
    }
  };

  const refreshUser = async () => {
    if (tokenStorage.getAccessToken()) {
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

