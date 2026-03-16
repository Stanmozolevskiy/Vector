import { forwardRef, useState } from 'react';

type PasswordInputProps = React.ComponentPropsWithoutRef<'input'> & {
  label?: string;
  error?: string;
};

/**
 * Password input with an always-visible show/hide toggle.
 * Fixes BUG-Login-01: browser-native reveal controls can disappear on blur/focus;
 * this custom toggle remains visible regardless of focus state.
 */
export const PasswordInput = forwardRef<HTMLInputElement, PasswordInputProps>(
  ({ label, error, id, className, ...inputProps }, ref) => {
    const [showPassword, setShowPassword] = useState(false);

    return (
      <div className="form-group">
        {label != null && (
          <label htmlFor={id}>{label}</label>
        )}
        <div className="password-input-wrapper">
          <input
            ref={ref}
            id={id}
            type={showPassword ? 'text' : 'password'}
            className={className}
            aria-describedby={error ? `${id}-error` : undefined}
            {...inputProps}
          />
          <button
            type="button"
            className="password-toggle"
            onClick={() => setShowPassword((prev) => !prev)}
            tabIndex={-1}
            aria-label={showPassword ? 'Hide password' : 'Show password'}
            title={showPassword ? 'Hide password' : 'Show password'}
          >
            <i className={showPassword ? 'fas fa-eye-slash' : 'fas fa-eye'} aria-hidden />
          </button>
        </div>
        {error != null && error !== '' && (
          <span id={id ? `${id}-error` : undefined} style={{ color: '#ef4444', fontSize: '0.875rem' }}>
            {error}
          </span>
        )}
      </div>
    );
  }
);

PasswordInput.displayName = 'PasswordInput';
