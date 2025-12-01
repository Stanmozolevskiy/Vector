import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuth } from '../../hooks/useAuth.tsx';
import { ROUTES } from '../../utils/constants';

const registerSchema = z.object({
  firstName: z.string().optional(),
  lastName: z.string().optional(),
  email: z.string().email('Invalid email address'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  confirmPassword: z.string().min(8, 'Please confirm your password'),
  terms: z.boolean().refine(val => val === true, 'You must agree to the terms'),
}).refine((data) => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ["confirmPassword"],
});

type RegisterFormData = z.infer<typeof registerSchema>;

export const RegisterPage = () => {
  const navigate = useNavigate();
  const { register: registerUser } = useAuth();
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  });

  const onSubmit = async (data: RegisterFormData) => {
    try {
      setError('');
      await registerUser({
        email: data.email,
        password: data.password,
        firstName: data.firstName,
        lastName: data.lastName,
      });
      setSuccess(true);
      setTimeout(() => {
        navigate(ROUTES.LOGIN);
      }, 3000);
    } catch (err) {
      const errorMessage = err && typeof err === 'object' && 'response' in err
        ? (err.response as { data?: { error?: string } })?.data?.error
        : undefined;
      setError(errorMessage || 'Registration failed. Please try again.');
    }
  };

  const handleSocialSignup = (provider: 'google' | 'linkedin') => {
    // TODO: Implement social signup
    console.log(`${provider} signup clicked`);
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
              <div className="icon-circle" style={{ background: '#d1fae5' }}>
                <i className="fas fa-check" style={{ color: '#10b981', fontSize: '2.5rem' }}></i>
              </div>
              <h1>Registration Successful!</h1>
              <p>Please check your email to verify your account before logging in.</p>
              <p style={{ fontSize: '0.875rem', color: '#64748b', marginTop: '1rem' }}>
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
      <div className="auth-wrapper">
        <div className="auth-left">
          <div className="auth-brand">
            <Link to="/">
              <i className="fas fa-vector-square"></i>
              <span>Vector</span>
            </Link>
          </div>
          <div className="auth-content">
            <h1>Start your journey</h1>
            <p>Create your account and ace your next interview</p>
            
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

              <div className="form-row">
                <div className="form-group">
                  <label htmlFor="firstName">First Name</label>
                  <input
                    {...register('firstName')}
                    id="firstName"
                    type="text"
                    placeholder="John"
                  />
                </div>
                <div className="form-group">
                  <label htmlFor="lastName">Last Name</label>
                  <input
                    {...register('lastName')}
                    id="lastName"
                    type="text"
                    placeholder="Doe"
                  />
                </div>
              </div>
              
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
                  autoComplete="new-password"
                  placeholder="Create a strong password"
                  required
                />
                <small className="form-help">Must be at least 8 characters</small>
                {errors.password && (
                  <span style={{ color: '#ef4444', fontSize: '0.875rem' }}>{errors.password.message}</span>
                )}
              </div>
              
              <div className="form-group">
                <label htmlFor="confirmPassword">Confirm Password</label>
                <input
                  {...register('confirmPassword')}
                  id="confirmPassword"
                  type="password"
                  autoComplete="new-password"
                  placeholder="Re-enter your password"
                  required
                />
                {errors.confirmPassword && (
                  <span style={{ color: '#ef4444', fontSize: '0.875rem' }}>{errors.confirmPassword.message}</span>
                )}
              </div>
              
              <label className="checkbox-label">
                <input {...register('terms')} type="checkbox" name="terms" required />
                <span>I agree to the <a href="#">Terms of Service</a> and <a href="#">Privacy Policy</a></span>
              </label>
              {errors.terms && (
                <span style={{ color: '#ef4444', fontSize: '0.875rem', marginTop: '-1rem' }}>{errors.terms.message}</span>
              )}
              
              <button type="submit" className="btn-primary btn-full" disabled={isSubmitting}>
                {isSubmitting ? 'Creating account...' : 'Create Account'}
              </button>
            </form>
            
            <div className="divider">
              <span>or sign up with</span>
            </div>
            
            <div className="social-auth">
              <button type="button" className="social-btn google-btn" onClick={() => handleSocialSignup('google')}>
                <i className="fab fa-google"></i>
                <span>Google</span>
              </button>
              <button type="button" className="social-btn linkedin-btn" onClick={() => handleSocialSignup('linkedin')}>
                <i className="fab fa-linkedin"></i>
                <span>LinkedIn</span>
              </button>
            </div>
            
            <p className="auth-footer">
              Already have an account? <Link to={ROUTES.LOGIN}>Log in</Link>
            </p>
          </div>
        </div>
        
        <div className="auth-right">
          <div className="auth-testimonial">
            <i className="fas fa-quote-left"></i>
            <p>"The system design course was phenomenal. I went from nervous to confident and got offers from 3 top companies."</p>
            <div className="testimonial-author">
              <div className="author-avatar">MP</div>
              <div className="author-info">
                <div className="author-name">Michael Park</div>
                <div className="author-title">Senior Engineer at Meta</div>
              </div>
            </div>
          </div>
          <div className="auth-benefits">
            <h3>What you'll get:</h3>
            <ul>
              <li><i className="fas fa-check"></i> Access to 100+ expert-led courses</li>
              <li><i className="fas fa-check"></i> 1000+ interview questions with solutions</li>
              <li><i className="fas fa-check"></i> Live mock interviews with professionals</li>
              <li><i className="fas fa-check"></i> Personalized learning path</li>
              <li><i className="fas fa-check"></i> Progress tracking and analytics</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
};
