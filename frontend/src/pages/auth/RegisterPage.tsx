import { useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuth } from '../../hooks/useAuth.tsx';
import { ROUTES } from '../../utils/constants';
import { PasswordInput } from '../../components/common/PasswordInput';

function nameContainsDigit(s: string): boolean {
  if (/[0-9]/.test(s)) return true;
  try {
    return /\p{Nd}/u.test(s);
  } catch {
    return false;
  }
}

function validateNameValue(raw: unknown, label: string): string | undefined {
  const s = typeof raw === 'string' ? raw : String(raw ?? '');
  const t = s.trim();
  if (!t) return `${label} is required.`;
  if (t.length > 100) return `${label} must be at most 100 characters.`;
  if (nameContainsDigit(t)) return `${label} must not contain numbers.`;
  return undefined;
}

const nameField = (label: string) =>
  z
    .string()
    .trim()
    .min(1, `${label} is required.`)
    .max(100, `${label} must be at most 100 characters.`)
    .regex(/^[^\p{Nd}]+$/u, `${label} must not contain numbers.`);

const registerSchema = z
  .object({
    firstName: nameField('First name'),
    lastName: nameField('Last name'),
    email: z
      .string()
      .trim()
      .min(1, 'Email is required.')
      .email('Invalid email address.')
      .max(254, 'Email must be at most 254 characters.'),
    password: z
      .string()
      .min(8, 'Password must be at least 8 characters.')
      .max(256, 'Password must be at most 256 characters.'),
    confirmPassword: z.string().min(1, 'Please confirm your password.'),
    terms: z.boolean().refine((val) => val === true, {
      message: 'You must agree to the terms.',
    }),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords don't match.",
    path: ['confirmPassword'],
  });

type RegisterFormData = z.infer<typeof registerSchema>;

function fieldErrorStyle(): { color: string; fontSize: string; display: string; marginTop: string } {
  return { color: '#ef4444', fontSize: '0.875rem', display: 'block', marginTop: '0.25rem' };
}

export const RegisterPage = () => {
  const { register: registerUser } = useAuth();
  const [bannerError, setBannerError] = useState<string>('');
  const [success, setSuccess] = useState(false);
  /** Names read from the DOM on submit (RHF state can desync from visible inputs). */
  const namesFromFormRef = useRef({ firstName: '', lastName: '' });

  const {
    register,
    handleSubmit,
    setError: setFieldError,
    clearErrors,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      password: '',
      confirmPassword: '',
      terms: false,
    },
  });

  const onSubmit = async (data: RegisterFormData) => {
    const fn = namesFromFormRef.current.firstName;
    const ln = namesFromFormRef.current.lastName;
    const fe = validateNameValue(fn, 'First name');
    const le = validateNameValue(ln, 'Last name');
    if (fe) setFieldError('firstName', { type: 'manual', message: fe });
    if (le) setFieldError('lastName', { type: 'manual', message: le });
    if (fe || le) return;

    try {
      setBannerError('');
      await registerUser({
        email: data.email,
        password: data.password,
        firstName: fn,
        lastName: ln,
      });
      setSuccess(true);
    } catch (err) {
      const res =
        err && typeof err === 'object' && 'response' in err
          ? (err as { response?: { data?: unknown; status?: number } }).response
          : undefined;
      const data = res?.data;
      if (data && typeof data === 'object') {
        const d = data as Record<string, unknown>;
        if (typeof d.error === 'string') {
          setBannerError(d.error);
          return;
        }
        if (d.errors && typeof d.errors === 'object') {
          const errs = d.errors as Record<string, string[] | string>;
          const first = Object.values(errs).flat()[0];
          if (typeof first === 'string') {
            setBannerError(first);
            return;
          }
        }
      }
      setBannerError('Registration failed. Please try again.');
    }
  };

  const handleSocialSignup = (provider: 'google' | 'linkedin') => {
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
              <h1>Check your email</h1>
              <p>We've sent you a verification link. Please check your inbox and verify your email address before logging in.</p>
              <p className="auth-footer" style={{ marginTop: '2rem' }}>
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

            <form
              className="auth-form"
              noValidate
              onSubmit={(e) => {
                e.preventDefault();
                setBannerError('');
                const form = e.currentTarget;
                const fd = new FormData(form);
                const firstName = String(fd.get('firstName') ?? '').trim();
                const lastName = String(fd.get('lastName') ?? '').trim();
                namesFromFormRef.current = { firstName, lastName };

                const nameErrFirst = validateNameValue(firstName, 'First name');
                const nameErrLast = validateNameValue(lastName, 'Last name');
                clearErrors(['firstName', 'lastName']);
                if (nameErrFirst) {
                  setFieldError('firstName', { type: 'manual', message: nameErrFirst });
                }
                if (nameErrLast) {
                  setFieldError('lastName', { type: 'manual', message: nameErrLast });
                }
                if (nameErrFirst || nameErrLast) {
                  return;
                }
                void handleSubmit(onSubmit)(e);
              }}
            >
              {bannerError && (
                <div
                  style={{
                    background: '#fee2e2',
                    borderLeft: '4px solid #ef4444',
                    padding: '1rem',
                    borderRadius: '0.5rem',
                    color: '#991b1b',
                    fontSize: '0.875rem',
                  }}
                >
                  {bannerError}
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
                    autoComplete="given-name"
                    aria-invalid={!!errors.firstName}
                    aria-describedby={errors.firstName ? 'firstName-error' : undefined}
                  />
                  {errors.firstName && (
                    <span id="firstName-error" style={fieldErrorStyle()} role="alert">
                      {errors.firstName.message}
                    </span>
                  )}
                </div>
                <div className="form-group">
                  <label htmlFor="lastName">Last Name</label>
                  <input
                    {...register('lastName')}
                    id="lastName"
                    type="text"
                    placeholder="Doe"
                    autoComplete="family-name"
                    aria-invalid={!!errors.lastName}
                    aria-describedby={errors.lastName ? 'lastName-error' : undefined}
                  />
                  {errors.lastName && (
                    <span id="lastName-error" style={fieldErrorStyle()} role="alert">
                      {errors.lastName.message}
                    </span>
                  )}
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
                  aria-invalid={!!errors.email}
                  aria-describedby={errors.email ? 'email-error' : undefined}
                />
                {errors.email && (
                  <span id="email-error" style={fieldErrorStyle()} role="alert">
                    {errors.email.message}
                  </span>
                )}
              </div>

              <PasswordInput
                label="Password"
                id="password"
                placeholder="Create a strong password"
                autoComplete="new-password"
                error={errors.password?.message}
                {...register('password')}
              />
              <small className="form-help">Must be at least 8 characters</small>

              <PasswordInput
                label="Confirm Password"
                id="confirmPassword"
                placeholder="Re-enter your password"
                autoComplete="new-password"
                error={errors.confirmPassword?.message}
                {...register('confirmPassword')}
              />

              <label className="checkbox-label">
                <input {...register('terms')} type="checkbox" />
                <span>
                  I agree to the <Link to={ROUTES.TERMS}>Terms of Service</Link> and <Link to={ROUTES.PRIVACY}>Privacy Policy</Link>
                </span>
              </label>
              {errors.terms && (
                <span style={{ ...fieldErrorStyle(), marginTop: '-0.5rem' }} role="alert">
                  {errors.terms.message}
                </span>
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
              <li>
                <i className="fas fa-check"></i> Access to 100+ expert-led courses
              </li>
              <li>
                <i className="fas fa-check"></i> 1000+ interview questions with solutions
              </li>
              <li>
                <i className="fas fa-check"></i> Live mock interviews with professionals
              </li>
              <li>
                <i className="fas fa-check"></i> Personalized learning path
              </li>
              <li>
                <i className="fas fa-check"></i> Progress tracking and analytics
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
};
