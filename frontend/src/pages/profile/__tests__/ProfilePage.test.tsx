import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { render } from '../../../test/utils/test-utils';
import { ProfilePage } from '../ProfilePage';
import { server } from '../../../test/mocks/server';
import { http, HttpResponse } from 'msw';

// Mock useNavigate
const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe('ProfilePage', () => {
  beforeEach(() => {
    mockNavigate.mockClear();
    localStorage.clear();
    // Set up authenticated user
    localStorage.setItem('accessToken', 'mock-token');
    localStorage.setItem('refreshToken', 'mock-refresh-token');
  });

  it('renders profile form with all fields', async () => {
    render(<ProfilePage />);

    // Wait for user data to load
    await waitFor(() => {
      expect(screen.getByLabelText(/first name/i)).toBeInTheDocument();
    }, { timeout: 5000 });

    expect(screen.getByLabelText(/last name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/bio/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/phone number/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/location/i)).toBeInTheDocument();
  });

  it('loads user data into form fields', async () => {
    render(<ProfilePage />);

    await waitFor(() => {
      expect(screen.getByLabelText(/first name/i)).toBeInTheDocument();
    }, { timeout: 5000 });

    const firstNameInput = screen.getByLabelText(/first name/i);
    const lastNameInput = screen.getByLabelText(/last name/i);
    
    await waitFor(() => {
      expect(firstNameInput).toHaveValue('Test');
      expect(lastNameInput).toHaveValue('User');
    }, { timeout: 3000 });
  });

  it('updates profile information successfully', async () => {
    const user = userEvent.setup();
    render(<ProfilePage />);

    await waitFor(() => {
      expect(screen.getByLabelText(/first name/i)).toBeInTheDocument();
    }, { timeout: 5000 });

    const firstNameInput = screen.getByLabelText(/first name/i);
    const lastNameInput = screen.getByLabelText(/last name/i);
    const bioInput = screen.getByLabelText(/bio/i);
    const saveButton = screen.getByRole('button', { name: /save changes/i });

    await user.clear(firstNameInput);
    await user.type(firstNameInput, 'Updated');
    await user.clear(lastNameInput);
    await user.type(lastNameInput, 'Name');
    await user.clear(bioInput);
    await user.type(bioInput, 'Updated bio');
    await user.click(saveButton);

    await waitFor(() => {
      // Check for success message (case-insensitive)
      const successMessage = screen.queryByText(/profile updated successfully/i) ||
                            screen.queryByText(/updated successfully/i) ||
                            screen.queryByText(/successfully/i);
      expect(successMessage).toBeInTheDocument();
    }, { timeout: 5000 });
  });

  it('shows password change form in Privacy tab', async () => {
    const user = userEvent.setup();
    render(<ProfilePage />);

    // Wait for page to load
    await waitFor(() => {
      expect(screen.getByLabelText(/first name/i)).toBeInTheDocument();
    }, { timeout: 5000 });

    // Click Privacy tab - find button by text content
    const privacyButtons = screen.getAllByText(/privacy/i);
    const privacyTab = privacyButtons.find(btn => btn.closest('button')) || privacyButtons[0];
    await user.click(privacyTab as HTMLElement);

    await waitFor(() => {
      expect(screen.getByLabelText(/current password/i)).toBeInTheDocument();
    }, { timeout: 3000 });
    
    // Check that password form fields are present (may be multiple due to hidden inputs)
    await waitFor(() => {
      const newPasswordInputs = screen.queryAllByLabelText(/new password/i);
      expect(newPasswordInputs.length).toBeGreaterThan(0);
    }, { timeout: 3000 });
    
    await waitFor(() => {
      const confirmPasswordInputs = screen.queryAllByLabelText(/confirm new password/i);
      expect(confirmPasswordInputs.length).toBeGreaterThan(0);
    }, { timeout: 3000 });
  });

  it('validates password change form - passwords must match', async () => {
    const user = userEvent.setup();
    render(<ProfilePage />);

    // Wait for page to load
    await waitFor(() => {
      expect(screen.getByLabelText(/first name/i)).toBeInTheDocument();
    }, { timeout: 5000 });

    // Click Privacy tab
    const privacyButtons = screen.getAllByText(/privacy/i);
    const privacyTab = privacyButtons.find(btn => btn.closest('button')) || privacyButtons[0];
    await user.click(privacyTab as HTMLElement);

    await waitFor(() => {
      expect(screen.getByLabelText(/current password/i)).toBeInTheDocument();
    }, { timeout: 3000 });

    const currentPasswordInput = screen.getByLabelText(/current password/i);
    const newPasswordInputs = screen.queryAllByLabelText(/new password/i);
    const newPasswordInput = newPasswordInputs.find(input => {
      const element = input as HTMLElement;
      const style = window.getComputedStyle(element);
      return style.display !== 'none' && style.visibility !== 'hidden';
    }) as HTMLInputElement;
    expect(newPasswordInput).toBeDefined();
    const confirmPasswordInputs = screen.queryAllByLabelText(/confirm new password/i);
    const confirmPasswordInput = confirmPasswordInputs.find(input => {
      const element = input as HTMLElement;
      const style = window.getComputedStyle(element);
      return style.display !== 'none' && style.visibility !== 'hidden';
    }) as HTMLInputElement;
    expect(confirmPasswordInput).toBeDefined();
    const updateButton = screen.getByRole('button', { name: /update password/i });

    await user.type(currentPasswordInput, 'CurrentPassword123!');
    await user.type(newPasswordInput, 'NewPassword123!');
    await user.type(confirmPasswordInput, 'DifferentPassword123!');
    await user.click(updateButton);

    await waitFor(() => {
      expect(screen.getByText(/passwords do not match/i)).toBeInTheDocument();
    }, { timeout: 3000 });
  });

  it('successfully changes password with correct current password', async () => {
    const user = userEvent.setup();
    render(<ProfilePage />);

    // Wait for page to load
    await waitFor(() => {
      expect(screen.getByLabelText(/first name/i)).toBeInTheDocument();
    }, { timeout: 5000 });

    // Click Privacy tab
    const privacyButtons = screen.getAllByText(/privacy/i);
    const privacyTab = privacyButtons.find(btn => btn.closest('button')) || privacyButtons[0];
    await user.click(privacyTab as HTMLElement);

    await waitFor(() => {
      expect(screen.getByLabelText(/current password/i)).toBeInTheDocument();
    }, { timeout: 3000 });

    const currentPasswordInput = screen.getByLabelText(/current password/i);
    const newPasswordInputs = screen.queryAllByLabelText(/new password/i);
    const newPasswordInput = newPasswordInputs.find(input => {
      const element = input as HTMLElement;
      const style = window.getComputedStyle(element);
      return style.display !== 'none' && style.visibility !== 'hidden';
    }) as HTMLInputElement;
    expect(newPasswordInput).toBeDefined();
    const confirmPasswordInputs = screen.queryAllByLabelText(/confirm new password/i);
    const confirmPasswordInput = confirmPasswordInputs.find(input => {
      const element = input as HTMLElement;
      const style = window.getComputedStyle(element);
      return style.display !== 'none' && style.visibility !== 'hidden';
    }) as HTMLInputElement;
    expect(confirmPasswordInput).toBeDefined();
    const updateButton = screen.getByRole('button', { name: /update password/i });

    await user.type(currentPasswordInput, 'CurrentPassword123!');
    await user.type(newPasswordInput, 'NewPassword123!');
    await user.type(confirmPasswordInput, 'NewPassword123!');
    await user.click(updateButton);

    await waitFor(() => {
      expect(screen.getByText(/password changed successfully/i)).toBeInTheDocument();
    }, { timeout: 5000 });
  });

  it('shows error when current password is incorrect', async () => {
    const user = userEvent.setup();
    
    server.use(
      http.put('http://localhost:5000/api/users/me/password', () => {
        return HttpResponse.json(
          { error: 'Current password is incorrect.' },
          { status: 400 }
        );
      })
    );

    render(<ProfilePage />);

    // Wait for page to load
    await waitFor(() => {
      expect(screen.getByLabelText(/first name/i)).toBeInTheDocument();
    }, { timeout: 5000 });

    // Click Privacy tab
    const privacyButtons = screen.getAllByText(/privacy/i);
    const privacyTab = privacyButtons.find(btn => btn.closest('button')) || privacyButtons[0];
    await user.click(privacyTab as HTMLElement);

    await waitFor(() => {
      expect(screen.getByLabelText(/current password/i)).toBeInTheDocument();
    }, { timeout: 3000 });

    const currentPasswordInput = screen.getByLabelText(/current password/i);
    const newPasswordInputs = screen.queryAllByLabelText(/new password/i);
    const newPasswordInput = newPasswordInputs.find(input => {
      const element = input as HTMLElement;
      const style = window.getComputedStyle(element);
      return style.display !== 'none' && style.visibility !== 'hidden';
    }) as HTMLInputElement;
    expect(newPasswordInput).toBeDefined();
    const confirmPasswordInputs = screen.queryAllByLabelText(/confirm new password/i);
    const confirmPasswordInput = confirmPasswordInputs.find(input => {
      const element = input as HTMLElement;
      const style = window.getComputedStyle(element);
      return style.display !== 'none' && style.visibility !== 'hidden';
    }) as HTMLInputElement;
    expect(confirmPasswordInput).toBeDefined();
    const updateButton = screen.getByRole('button', { name: /update password/i });

    await user.type(currentPasswordInput, 'WrongPassword123!');
    await user.type(newPasswordInput, 'NewPassword123!');
    await user.type(confirmPasswordInput, 'NewPassword123!');
    await user.click(updateButton);

    await waitFor(() => {
      expect(screen.getByText(/current password is incorrect/i)).toBeInTheDocument();
    }, { timeout: 5000 });
  });

  it('displays subscription information in Subscription tab', {
    timeout: 15000
  }, async () => {
    const user = userEvent.setup();
    render(<ProfilePage />);

    // Wait for page to load
    await waitFor(() => {
      expect(screen.getByLabelText(/first name/i)).toBeInTheDocument();
    }, { timeout: 5000 });

    // Click Subscription tab
    const subscriptionButtons = screen.getAllByText(/subscription/i);
    const subscriptionTab = subscriptionButtons.find(btn => btn.closest('button')) || subscriptionButtons[0];
    await user.click(subscriptionTab as HTMLElement);

    // Wait for subscription data to load
    await waitFor(() => {
      const freePlanTexts = screen.queryAllByText(/free plan/i);
      const currentPlanText = screen.queryByText(/current plan/i);
      expect(freePlanTexts.length > 0 || currentPlanText).toBeTruthy();
    }, { timeout: 10000 });
  });
});

