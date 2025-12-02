import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './hooks/useAuth.tsx';
import { ProtectedRoute } from './components/ProtectedRoute';
import { LoginPage } from './pages/auth/LoginPage';
import { RegisterPage } from './pages/auth/RegisterPage';
import { VerifyEmailPage } from './pages/auth/VerifyEmailPage';
import { ForgotPasswordPage } from './pages/auth/ForgotPasswordPage';
import { ResetPasswordPage } from './pages/auth/ResetPasswordPage';
import { ResendVerificationPage } from './pages/auth/ResendVerificationPage';
import { DashboardPage } from './pages/dashboard/DashboardPage';
import { ProfilePage } from './pages/profile/ProfilePage';
import { ROUTES } from './utils/constants';

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
        {/* Public routes */}
        <Route path="/" element={<Navigate to={ROUTES.LOGIN} replace />} />
        <Route path={ROUTES.VERIFY_EMAIL} element={<VerifyEmailPage />} />
          
          {/* Auth routes (redirect to dashboard if already logged in) */}
          <Route path={ROUTES.LOGIN} element={
            <ProtectedRoute requireUnauth>
              <LoginPage />
            </ProtectedRoute>
          } />
          <Route path={ROUTES.REGISTER} element={
            <ProtectedRoute requireUnauth>
              <RegisterPage />
            </ProtectedRoute>
          } />
          <Route path={ROUTES.FORGOT_PASSWORD} element={
            <ProtectedRoute requireUnauth>
              <ForgotPasswordPage />
            </ProtectedRoute>
          } />
          <Route path={ROUTES.RESET_PASSWORD} element={
            <ProtectedRoute requireUnauth>
              <ResetPasswordPage />
            </ProtectedRoute>
          } />
          <Route path={ROUTES.RESEND_VERIFICATION} element={
            <ProtectedRoute requireUnauth>
              <ResendVerificationPage />
            </ProtectedRoute>
          } />
          
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
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
