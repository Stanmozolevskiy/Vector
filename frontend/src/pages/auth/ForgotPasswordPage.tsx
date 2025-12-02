import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ROUTES } from '../../utils/constants';
import { authService } from '../../services/auth.service';

const forgotPasswordSchema = z.object({
  email: z.string().email('Invalid email address'),
});

type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;

export const ForgotPasswordPage = () => {
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ForgotPasswordFormData>({
    resolver: zodResolver(forgotPasswordSchema),
  });

  const onSubmit = async (data: ForgotPasswordFormData) => {
    try {
      setError('');
      setIsSubmitting(true);
      await authService.forgotPassword(data.email);
      setSuccess(true);
    } catch (err) {
      const errorMessage = err && typeof err === 'object' && 'response' in err
        ? (err.response as { data?: { error?: string } })?.data?.error
        : undefined;
      setError(errorMessage || 'Failed to send reset link. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (success) {
    return (
      <div className="auth-container">
        <div className="auth-wrapper simple">
          <div className="auth-left full-width">
            <div className="auth-brand">
              <Link to="/">
                <i className="fas fa-vector-square"></i>
                <span>Vector</span>
              </Link>
            </div>
            <div className="auth-content centered">
              <div className="icon-circle" style={{ background: '#d1fae5', margin: '0 auto 2rem' }}>
                <i className="fas fa-check" style={{ color: '#10b981', fontSize: '2.5rem' }}></i>
              </div>
              <h1 style={{ textAlign: 'center', marginBottom: '1rem' }}>Check your email</h1>
              <p style={{ textAlign: 'center', marginBottom: '2rem', color: '#64748b' }}>
                We've sent you a password reset link. Please check your inbox and follow the instructions.
              </p>
              <p className="auth-footer" style={{ marginTop: '1.5rem', textAlign: 'center' }}>
                <Link to={ROUTES.LOGIN}>
                  <i className="fas fa-arrow-left"></i> Back to login
                </Link>
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
            <Link to="/">
              <i className="fas fa-vector-square"></i>
              <span>Vector</span>
            </Link>
          </div>
          <div className="auth-content centered">
            <div className="icon-circle" style={{ margin: '0 auto 2rem' }}>
              <i className="fas fa-lock"></i>
            </div>
            <h1 style={{ textAlign: 'center', marginBottom: '1rem', width: '100%' }}>Forgot password?</h1>
            <p style={{ textAlign: 'center', marginBottom: '2rem', color: '#64748b', width: '100%' }}>
              No worries! Enter your email and we'll send you reset instructions.
            </p>
            
            <form className="auth-form" onSubmit={handleSubmit(onSubmit)} style={{ width: '100%', maxWidth: '100%', boxSizing: 'border-box' }}>
              {error && (
                <div style={{ 
                  background: '#fee2e2', 
                  borderLeft: '4px solid #ef4444', 
                  padding: '1rem', 
                  borderRadius: '0.5rem',
                  color: '#991b1b',
                  fontSize: '0.875rem'
                }}>
                  {error}
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
                  required
                />
                {errors.email && (
                  <span style={{ color: '#ef4444', fontSize: '0.875rem' }}>{errors.email.message}</span>
                )}
              </div>
              
              <button type="submit" className="btn-primary btn-full" disabled={isSubmitting}>
                {isSubmitting ? 'Sending...' : 'Send Reset Link'}
              </button>
            </form>
            
            <p className="auth-footer" style={{ textAlign: 'center', width: '100%' }}>
              <Link to={ROUTES.LOGIN}>
                <i className="fas fa-arrow-left"></i> Back to login
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};
