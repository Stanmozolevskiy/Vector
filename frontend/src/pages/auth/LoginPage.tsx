import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuth } from '../../hooks/useAuth.tsx';
import { ROUTES } from '../../utils/constants';

const loginSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(1, 'Password is required'),
  remember: z.boolean().optional(),
});

type LoginFormData = z.infer<typeof loginSchema>;

export const LoginPage = () => {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [error, setError] = useState<string>('');

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginFormData) => {
    try {
      setError('');
      await login({ email: data.email, password: data.password });
      navigate('/dashboard');
    } catch (err) {
      const errorMessage = err && typeof err === 'object' && 'response' in err
        ? (err.response as { data?: { error?: string } })?.data?.error
        : undefined;
      setError(errorMessage || 'Login failed. Please try again.');
    }
  };

  const handleSocialLogin = (provider: 'google' | 'linkedin') => {
    // TODO: Implement social login
    console.log(`${provider} login clicked`);
  };

  return (
    <div className="auth-container">
      <div className="auth-wrapper">
        <div className="auth-left">
          <div className="auth-brand">
            <Link to="/">
              <i className="fas fa-vector-square"></i>
              <span>Vector</span>
            </Link>
          </div>
          <div className="auth-content">
            <h1>Welcome back!</h1>
            <p>Log in to continue your interview preparation journey</p>
            
            <form className="auth-form" onSubmit={handleSubmit(onSubmit)}>
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
              
              <div className="form-group">
                <label htmlFor="password">Password</label>
                <input
                  {...register('password')}
                  id="password"
                  type="password"
                  autoComplete="current-password"
                  placeholder="Enter your password"
                  required
                />
                {errors.password && (
                  <span style={{ color: '#ef4444', fontSize: '0.875rem' }}>{errors.password.message}</span>
                )}
              </div>
              
              <div className="form-options">
                <label className="checkbox-label">
                  <input {...register('remember')} type="checkbox" name="remember" />
                  <span>Remember me</span>
                </label>
                <Link to={ROUTES.FORGOT_PASSWORD} className="forgot-link">Forgot password?</Link>
              </div>
              
              <button type="submit" className="btn-primary btn-full" disabled={isSubmitting}>
                {isSubmitting ? 'Logging in...' : 'Log In'}
              </button>
            </form>
            
            <div className="divider">
              <span>or continue with</span>
            </div>
            
            <div className="social-auth">
              <button type="button" className="social-btn google-btn" onClick={() => handleSocialLogin('google')}>
                <i className="fab fa-google"></i>
                <span>Google</span>
              </button>
              <button type="button" className="social-btn linkedin-btn" onClick={() => handleSocialLogin('linkedin')}>
                <i className="fab fa-linkedin"></i>
                <span>LinkedIn</span>
              </button>
            </div>
            
            <p className="auth-footer">
              Don't have an account? <Link to={ROUTES.REGISTER}>Sign up</Link>
            </p>
          </div>
        </div>
        
        <div className="auth-right">
          <div className="auth-testimonial">
            <i className="fas fa-quote-left"></i>
            <p>"Vector helped me land my dream job at Google. The mock interviews were incredibly realistic and the feedback was invaluable!"</p>
            <div className="testimonial-author">
              <div className="author-avatar">JD</div>
              <div className="author-info">
                <div className="author-name">Jessica Davis</div>
                <div className="author-title">Software Engineer at Google</div>
              </div>
            </div>
          </div>
          <div className="auth-stats">
            <div className="stat-item">
              <div className="stat-number">50K+</div>
              <div className="stat-label">Students</div>
            </div>
            <div className="stat-item">
              <div className="stat-number">10K+</div>
              <div className="stat-label">Job Offers</div>
            </div>
            <div className="stat-item">
              <div className="stat-number">4.9/5</div>
              <div className="stat-label">Rating</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
