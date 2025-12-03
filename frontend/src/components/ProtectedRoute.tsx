import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

interface ProtectedRouteProps {
  children: ReactNode;
  requiredRole?: string | string[];
  requireAuth?: boolean;
}

/**
 * Protected Route Component
 * Handles authentication and role-based access control
 * 
 * @param children - Child components to render if authorized
 * @param requiredRole - Required role(s) to access this route (optional)
 * @param requireAuth - Require authentication (default: true)
 */
const ProtectedRoute = ({ 
  children, 
  requiredRole,
  requireAuth = true 
}: ProtectedRouteProps) => {
  const { isAuthenticated, isLoading, hasRole } = useAuth();

  // Show loading state while checking auth
  if (isLoading) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh' 
      }}>
        <div className="loading-spinner">Loading...</div>
      </div>
    );
  }

  // Check authentication
  if (requireAuth && !isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  // Check role authorization
  if (requiredRole && !hasRole(requiredRole)) {
    return <Navigate to="/unauthorized" replace />;
  }

  return <>{children}</>;
};

export default ProtectedRoute;
