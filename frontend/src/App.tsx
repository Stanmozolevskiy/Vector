import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './hooks/useAuth.tsx';
import ProtectedRoute from './components/ProtectedRoute';
import { IndexPage } from './pages/home/IndexPage';
import { LoginPage } from './pages/auth/LoginPage';
import { RegisterPage } from './pages/auth/RegisterPage';
import { VerifyEmailPage } from './pages/auth/VerifyEmailPage';
import { ForgotPasswordPage } from './pages/auth/ForgotPasswordPage';
import { ResetPasswordPage } from './pages/auth/ResetPasswordPage';
import { ResendVerificationPage } from './pages/auth/ResendVerificationPage';
import { DashboardPage } from './pages/dashboard/DashboardPage';
import { ProfilePage } from './pages/profile/ProfilePage';
import AdminDashboardPage from './pages/admin/AdminDashboardPage';
import CoachApplicationPage from './pages/coach/CoachApplicationPage';
import UnauthorizedPage from './pages/UnauthorizedPage';
import { ROUTES } from './utils/constants';

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
        {/* Public routes - no protection */}
        <Route path={ROUTES.HOME} element={<IndexPage />} />
        <Route path={ROUTES.VERIFY_EMAIL} element={<VerifyEmailPage />} />
        
        {/* Auth routes - accessible to everyone, but redirect if already logged in */}
        <Route path={ROUTES.LOGIN} element={<LoginPage />} />
        <Route path={ROUTES.REGISTER} element={<RegisterPage />} />
        <Route path={ROUTES.FORGOT_PASSWORD} element={<ForgotPasswordPage />} />
        <Route path={ROUTES.RESET_PASSWORD} element={<ResetPasswordPage />} />
        <Route path={ROUTES.RESEND_VERIFICATION} element={<ResendVerificationPage />} />
        
        {/* Protected routes (require authentication) */}
        <Route path={ROUTES.DASHBOARD} element={
          <ProtectedRoute requireAuth>
            <DashboardPage />
          </ProtectedRoute>
        } />
        <Route path={ROUTES.PROFILE} element={
          <ProtectedRoute requireAuth>
            <ProfilePage />
          </ProtectedRoute>
        } />
        <Route path={ROUTES.COACH_APPLY} element={
          <ProtectedRoute requireAuth>
            <CoachApplicationPage />
          </ProtectedRoute>
        } />
        
        {/* Admin routes (require admin role) */}
        <Route path={ROUTES.ADMIN} element={
          <ProtectedRoute requireAuth requiredRole="admin">
            <AdminDashboardPage />
          </ProtectedRoute>
        } />
        
        {/* Unauthorized page */}
        <Route path="/unauthorized" element={<UnauthorizedPage />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
