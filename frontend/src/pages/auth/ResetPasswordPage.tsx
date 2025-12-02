import { useState, useEffect } from 'react';
import { Link, useSearchParams, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ROUTES } from '../../utils/constants';
import { authService } from '../../services/auth.service';

const resetPasswordSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  confirmPassword: z.string().min(8, 'Confirm password is required'),
}).refine((data) => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
});

type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;

export const ResetPasswordPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState(false);
  const token = searchParams.get('token');

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
  });

  useEffect(() => {
    if (!token) {
      setError('Invalid reset link. Please request a new password reset.');
    }
  }, [token]);

  const onSubmit = async (data: ResetPasswordFormData) => {
    if (!token) {
      setError('Invalid reset link. Please request a new password reset.');
      return;
    }

    try {
      setError('');
      await authService.resetPassword(token, data.email, data.password);
      setSuccess(true);
      setTimeout(() => {
        navigate(ROUTES.LOGIN);
      }, 3000);
    } catch (err) {
      const errorMessage = err && typeof err === 'object' && 'response' in err
        ? (err.response as { data?: { error?: string } })?.data?.error
        : undefined;
      setError(errorMessage || 'Failed to reset password. Please try again.');
    }
  };

  if (success) {
    return (
      <div className="auth-container">
        <div className="auth-wrapper simple">
          <div className="auth-left full-width">
            <div className="auth-brand">
              <Link to={ROUTES.HOME}>
                <i className="fas fa-vector-square"></i>
                <span>Vector</span>
              </Link>
            </div>
            <div className="auth-content centered">
              <div className="icon-circle bg-green-100">
                <i className="fas fa-check text-green-600"></i>
              </div>
              <h1>Password Reset Successful!</h1>
              <p>Your password has been reset. You can now log in with your new password.</p>
              <p className="text-sm text-gray-500 mt-4">
                Redirecting to login page...
              </p>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="auth-container">
      <div className="auth-wrapper simple">
        <div className="auth-left full-width">
          <div className="auth-brand">
            <Link to={ROUTES.HOME}>
              <i className="fas fa-vector-square"></i>
              <span>Vector</span>
            </Link>
          </div>
          <div className="auth-content centered">
            <div className="icon-circle">
              <i className="fas fa-key"></i>
            </div>
            <h1>Reset your password</h1>
            <p>Enter your email and new password to reset your account.</p>

            <form className="auth-form" onSubmit={handleSubmit(onSubmit)}>
              {error && (
                <div className="bg-red-50 border-l-4 border-red-400 p-4 rounded mb-4">
                  <div className="flex">
                    <div className="flex-shrink-0">
                      <i className="fas fa-exclamation-circle text-red-400 h-5 w-5"></i>
                    </div>
                    <div className="ml-3">
                      <p className="text-sm text-red-700">{error}</p>
                    </div>
                  </div>
                </div>
              )}

              <div className="form-group">
                <label htmlFor="email">Email Address</label>
                <input
                  {...register('email')}
                  id="email"
                  type="email"
                  autoComplete="email"
                  placeholder="you@example.com"
                  className={errors.email ? 'input-error' : ''}
                />
                {errors.email && (
                  <p className="form-error">{errors.email.message}</p>
                )}
              </div>

              <div className="form-group">
                <label htmlFor="password">New Password</label>
                <input
                  {...register('password')}
                  id="password"
                  type="password"
                  autoComplete="new-password"
                  placeholder="Enter your new password"
                  className={errors.password ? 'input-error' : ''}
                />
                <small className="form-help">Must be at least 8 characters</small>
                {errors.password && (
                  <p className="form-error">{errors.password.message}</p>
                )}
              </div>

              <div className="form-group">
                <label htmlFor="confirmPassword">Confirm New Password</label>
                <input
                  {...register('confirmPassword')}
                  id="confirmPassword"
                  type="password"
                  autoComplete="new-password"
                  placeholder="Re-enter your new password"
                  className={errors.confirmPassword ? 'input-error' : ''}
                />
                {errors.confirmPassword && (
                  <p className="form-error">{errors.confirmPassword.message}</p>
                )}
              </div>

              <button type="submit" className="btn-primary btn-full" disabled={isSubmitting || !token}>
                {isSubmitting ? (
                  <span className="flex items-center justify-center">
                    <i className="fas fa-spinner fa-spin mr-2"></i>
                    Resetting password...
                  </span>
                ) : (
                  'Reset Password'
                )}
              </button>
            </form>

            <p className="auth-footer">
              <Link to={ROUTES.LOGIN}><i className="fas fa-arrow-left"></i> Back to login</Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

