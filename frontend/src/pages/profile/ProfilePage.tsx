import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';
import api from '../../services/api';
import '../../styles/profile.css';

interface ProfileFormData {
  firstName: string;
  lastName: string;
  bio: string;
  phoneNumber: string;
  location: string;
}

interface PasswordFormData {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export const ProfilePage = () => {
  const { user, isAuthenticated, isLoading, logout } = useAuth();
  const navigate = useNavigate();
  const [activeSection, setActiveSection] = useState('personal');
  const [profileData, setProfileData] = useState<ProfileFormData>({
    firstName: '',
    lastName: '',
    bio: '',
    phoneNumber: '',
    location: '',
  });
  const [passwordData, setPasswordData] = useState<PasswordFormData>({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });
  const [, setProfilePicture] = useState<File | null>(null);
  const [profilePicturePreview, setProfilePicturePreview] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState('');
  const [errorMessage, setErrorMessage] = useState('');
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      navigate(ROUTES.LOGIN);
    }
  }, [isAuthenticated, isLoading, navigate]);

  useEffect(() => {
    if (user) {
      setProfileData({
        firstName: user.firstName || '',
        lastName: user.lastName || '',
        bio: user.bio || '',
        phoneNumber: user.phoneNumber || '',
        location: user.location || '',
      });
    }
  }, [user]);

  const getUserInitials = () => {
    if (user?.firstName && user?.lastName) {
      return `${user.firstName[0]}${user.lastName[0]}`.toUpperCase();
    }
    if (user?.email) {
      return user.email.substring(0, 2).toUpperCase();
    }
    return 'U';
  };

  const handleLogout = () => {
    logout();
    navigate(ROUTES.HOME);
  };

  const handleProfileInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setProfileData({
      ...profileData,
      [e.target.name]: e.target.value,
    });
  };

  const handlePasswordInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setPasswordData({
      ...passwordData,
      [e.target.name]: e.target.value,
    });
  };

  const handleProfilePictureChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      if (!file.type.startsWith('image/')) {
        setErrorMessage('Please select an image file');
        return;
      }

      if (file.size > 5 * 1024 * 1024) {
        setErrorMessage('Image size must be less than 5MB');
        return;
      }

      setProfilePicture(file);

      const reader = new FileReader();
      reader.onloadend = () => {
        setProfilePicturePreview(reader.result as string);
      };
      reader.readAsDataURL(file);
      setErrorMessage('');
    }
  };

  const handleSaveProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSaving(true);
    setErrorMessage('');
    setSuccessMessage('');

    try {
      await api.put('/users/me', profileData);
      setSuccessMessage('Profile updated successfully!');

      setTimeout(() => {
        window.location.reload();
      }, 1500);
    } catch (err) {
      const errorMsg = err && typeof err === 'object' && 'response' in err
        ? (err.response as { data?: { error?: string } })?.data?.error
        : undefined;
      setErrorMessage(errorMsg || 'Failed to update profile');
    } finally {
      setIsSaving(false);
    }
  };

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();

    if (passwordData.newPassword !== passwordData.confirmPassword) {
      setErrorMessage('New passwords do not match');
      return;
    }

    if (passwordData.newPassword.length < 8) {
      setErrorMessage('New password must be at least 8 characters');
      return;
    }

    setIsSaving(true);
    setErrorMessage('');
    setSuccessMessage('');

    try {
      await api.put('/users/me/password', {
        currentPassword: passwordData.currentPassword,
        newPassword: passwordData.newPassword,
        confirmPassword: passwordData.confirmPassword,
      });

      setSuccessMessage('Password changed successfully!');
      setPasswordData({
        currentPassword: '',
        newPassword: '',
        confirmPassword: '',
      });
    } catch (err) {
      const errorMsg = err && typeof err === 'object' && 'response' in err
        ? (err.response as { data?: { error?: string } })?.data?.error
        : undefined;
      setErrorMessage(errorMsg || 'Failed to change password');
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <i className="fas fa-spinner fa-spin text-4xl text-blue-600 mb-4"></i>
          <p className="text-gray-600">Loading...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="profile-page">
      {/* Navigation */}
      <nav className="navbar">
        <div className="container">
          <div className="nav-brand">
            <Link to={ROUTES.HOME}>
              <i className="fas fa-vector-square"></i>
              <span>Vector</span>
            </Link>
          </div>
          <div className="nav-menu">
            <div className="user-menu">
              <div className="user-avatar">{getUserInitials()}</div>
              <span>{user?.firstName || user?.email?.split('@')[0] || 'User'}</span>
              <i className="fas fa-chevron-down"></i>
              <div className="dropdown-menu">
                <Link to={ROUTES.DASHBOARD}><i className="fas fa-tachometer-alt"></i> Dashboard</Link>
                <Link to={ROUTES.PROFILE} className="active"><i className="fas fa-user"></i> Profile</Link>
                <button onClick={handleLogout} style={{
                  width: '100%',
                  background: 'none',
                  border: 'none',
                  textAlign: 'left',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  gap: 'var(--spacing-sm)',
                  padding: 'var(--spacing-sm) var(--spacing-md)',
                  color: 'var(--text-secondary)',
                  transition: 'var(--transition)',
                  font: 'inherit'
                }}>
                  <i className="fas fa-sign-out-alt"></i> Logout
                </button>
              </div>
            </div>
          </div>
        </div>
      </nav>

      {/* Profile Content */}
      <section className="profile-section">
        <div className="container-wide">
          <div className="profile-header">
            <h1>Profile Settings</h1>
            <p>Manage your account settings and preferences</p>
          </div>

          {/* Success/Error Messages */}
          {successMessage && (
            <div style={{
              marginBottom: 'var(--spacing-lg)',
              background: '#d1fae5',
              borderLeft: '4px solid #10b981',
              padding: 'var(--spacing-md)',
              borderRadius: 'var(--radius-md)',
              color: '#065f46'
            }}>
              <i className="fas fa-check-circle" style={{ marginRight: '0.5rem' }}></i>
              {successMessage}
            </div>
          )}

          {errorMessage && (
            <div style={{
              marginBottom: 'var(--spacing-lg)',
              background: '#fee2e2',
              borderLeft: '4px solid #ef4444',
              padding: 'var(--spacing-md)',
              borderRadius: 'var(--radius-md)',
              color: '#991b1b'
            }}>
              <i className="fas fa-exclamation-circle" style={{ marginRight: '0.5rem' }}></i>
              {errorMessage}
            </div>
          )}

          <div className="profile-layout">
            {/* Sidebar Navigation */}
            <aside className="profile-sidebar">
              <nav className="profile-nav">
                <button
                  className={`profile-nav-item ${activeSection === 'personal' ? 'active' : ''}`}
                  onClick={() => setActiveSection('personal')}
                >
                  <i className="fas fa-user"></i>
                  <span>Personal Information</span>
                </button>
                <button
                  className={`profile-nav-item ${activeSection === 'security' ? 'active' : ''}`}
                  onClick={() => setActiveSection('security')}
                >
                  <i className="fas fa-lock"></i>
                  <span>Security</span>
                </button>
                <button
                  className={`profile-nav-item ${activeSection === 'subscription' ? 'active' : ''}`}
                  onClick={() => setActiveSection('subscription')}
                >
                  <i className="fas fa-credit-card"></i>
                  <span>Subscription</span>
                </button>
                <button
                  className={`profile-nav-item ${activeSection === 'notifications' ? 'active' : ''}`}
                  onClick={() => setActiveSection('notifications')}
                >
                  <i className="fas fa-bell"></i>
                  <span>Notifications</span>
                </button>
                <button
                  className={`profile-nav-item ${activeSection === 'privacy' ? 'active' : ''}`}
                  onClick={() => setActiveSection('privacy')}
                >
                  <i className="fas fa-shield-alt"></i>
                  <span>Privacy</span>
                </button>
              </nav>
            </aside>

            {/* Main Content */}
            <div className="profile-content">
              {/* Personal Information Section */}
              <div className={`profile-section-content ${activeSection === 'personal' ? 'active' : ''}`}>
                <div className="section-header">
                  <h2>Personal Information</h2>
                  <p>Update your personal details and profile picture</p>
                </div>

                {/* Profile Picture */}
                <div className="profile-card">
                  <h3>Profile Picture</h3>
                  <div className="profile-picture-section">
                    <div className="profile-picture-preview">
                      <div className="profile-avatar-large">
                        {profilePicturePreview ? (
                          <img src={profilePicturePreview} alt="Profile preview" />
                        ) : user?.profilePictureUrl ? (
                          <img src={user.profilePictureUrl} alt="Profile" />
                        ) : (
                          getUserInitials()
                        )}
                      </div>
                    </div>
                    <div className="profile-picture-actions">
                      <input
                        type="file"
                        id="pictureUpload"
                        accept="image/*"
                        style={{ display: 'none' }}
                        onChange={handleProfilePictureChange}
                      />
                      <button
                        className="btn-primary"
                        onClick={() => document.getElementById('pictureUpload')?.click()}
                      >
                        <i className="fas fa-upload"></i> Upload New Picture
                      </button>
                      <p className="helper-text">JPG, PNG or GIF. Max size 5MB.</p>
                    </div>
                  </div>
                </div>

                {/* Basic Information */}
                <div className="profile-card">
                  <h3>Basic Information</h3>
                  <form onSubmit={handleSaveProfile}>
                    <div className="form-row">
                      <div className="form-group">
                        <label htmlFor="firstName">First Name</label>
                        <input
                          type="text"
                          id="firstName"
                          name="firstName"
                          value={profileData.firstName}
                          onChange={handleProfileInputChange}
                        />
                      </div>
                      <div className="form-group">
                        <label htmlFor="lastName">Last Name</label>
                        <input
                          type="text"
                          id="lastName"
                          name="lastName"
                          value={profileData.lastName}
                          onChange={handleProfileInputChange}
                        />
                      </div>
                    </div>

                    <div className="form-group">
                      <label htmlFor="email">Email Address</label>
                      <input
                        type="email"
                        id="email"
                        value={user?.email || ''}
                        disabled
                        style={{ background: 'var(--bg-gray-50)', cursor: 'not-allowed' }}
                      />
                      <small className="form-help">Your email is used for login and notifications</small>
                    </div>

                    <div className="form-group">
                      <label htmlFor="bio">Bio</label>
                      <textarea
                        id="bio"
                        name="bio"
                        rows={4}
                        value={profileData.bio}
                        onChange={handleProfileInputChange}
                        placeholder="Tell us about yourself..."
                        maxLength={500}
                      />
                      <small className="form-help">{profileData.bio.length}/500 characters</small>
                    </div>

                    <div className="form-row">
                      <div className="form-group">
                        <label htmlFor="phoneNumber">Phone Number</label>
                        <input
                          type="tel"
                          id="phoneNumber"
                          name="phoneNumber"
                          value={profileData.phoneNumber}
                          onChange={handleProfileInputChange}
                          placeholder="+1 (555) 123-4567"
                        />
                      </div>
                      <div className="form-group">
                        <label htmlFor="location">Location</label>
                        <input
                          type="text"
                          id="location"
                          name="location"
                          value={profileData.location}
                          onChange={handleProfileInputChange}
                          placeholder="City, Country"
                        />
                      </div>
                    </div>

                    <div className="form-actions">
                      <button
                        type="button"
                        className="btn-outline"
                        onClick={() => {
                          if (user) {
                            setProfileData({
                              firstName: user.firstName || '',
                              lastName: user.lastName || '',
                              bio: user.bio || '',
                              phoneNumber: user.phoneNumber || '',
                              location: user.location || '',
                            });
                          }
                        }}
                        disabled={isSaving}
                      >
                        Cancel
                      </button>
                      <button type="submit" className="btn-primary" disabled={isSaving}>
                        {isSaving ? 'Saving...' : 'Save Changes'}
                      </button>
                    </div>
                  </form>
                </div>
              </div>

              {/* Security Section */}
              <div className={`profile-section-content ${activeSection === 'security' ? 'active' : ''}`}>
                <div className="section-header">
                  <h2>Security Settings</h2>
                  <p>Manage your password and security preferences</p>
                </div>

                {/* Change Password */}
                <div className="profile-card">
                  <h3>Change Password</h3>
                  <form onSubmit={handleChangePassword}>
                    <div className="form-group">
                      <label htmlFor="currentPassword">Current Password</label>
                      <input
                        type="password"
                        id="currentPassword"
                        name="currentPassword"
                        value={passwordData.currentPassword}
                        onChange={handlePasswordInputChange}
                        placeholder="Enter current password"
                        required
                      />
                    </div>

                    <div className="form-group">
                      <label htmlFor="newPassword">New Password</label>
                      <input
                        type="password"
                        id="newPassword"
                        name="newPassword"
                        value={passwordData.newPassword}
                        onChange={handlePasswordInputChange}
                        placeholder="Enter new password"
                        required
                        minLength={8}
                      />
                      <small className="form-help">Must be at least 8 characters with letters and numbers</small>
                    </div>

                    <div className="form-group">
                      <label htmlFor="confirmPassword">Confirm New Password</label>
                      <input
                        type="password"
                        id="confirmPassword"
                        name="confirmPassword"
                        value={passwordData.confirmPassword}
                        onChange={handlePasswordInputChange}
                        placeholder="Confirm new password"
                        required
                        minLength={8}
                      />
                    </div>

                    <div className="form-actions">
                      <button
                        type="button"
                        className="btn-outline"
                        onClick={() => {
                          setPasswordData({
                            currentPassword: '',
                            newPassword: '',
                            confirmPassword: '',
                          });
                        }}
                        disabled={isSaving}
                      >
                        Cancel
                      </button>
                      <button type="submit" className="btn-primary" disabled={isSaving}>
                        {isSaving ? 'Updating...' : 'Update Password'}
                      </button>
                    </div>
                  </form>
                </div>

                {/* Active Sessions */}
                <div className="profile-card">
                  <h3>Active Sessions</h3>
                  <div className="sessions-list">
                    <div className="session-item">
                      <div className="session-icon">
                        <i className="fas fa-desktop"></i>
                      </div>
                      <div className="session-info">
                        <h4>Current Browser</h4>
                        <p>Last active: Now</p>
                      </div>
                      <span className="session-badge current">Current Session</span>
                    </div>
                  </div>
                </div>
              </div>

              {/* Subscription Section */}
              <div className={`profile-section-content ${activeSection === 'subscription' ? 'active' : ''}`}>
                <div className="section-header">
                  <h2>Subscription & Billing</h2>
                  <p>Manage your subscription plan and payment methods</p>
                </div>

                {/* Current Plan */}
                <div className="profile-card">
                  <h3>Current Plan</h3>
                  <div className="current-plan">
                    <div className="plan-badge free">
                      <i className="fas fa-user"></i>
                      <span>Free Plan</span>
                    </div>
                    <div className="plan-details">
                      <div className="plan-info">
                        <h4>Vector Free</h4>
                        <p>$0.00 / month</p>
                        <p className="renewal-date">Upgrade to unlock premium features</p>
                      </div>
                      <div className="plan-actions">
                        <Link to={ROUTES.DASHBOARD} className="btn-primary">Upgrade Plan</Link>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              {/* Notifications Section */}
              <div className={`profile-section-content ${activeSection === 'notifications' ? 'active' : ''}`}>
                <div className="section-header">
                  <h2>Notification Preferences</h2>
                  <p>Choose what notifications you want to receive</p>
                </div>

                <div className="profile-card">
                  <h3>Email Notifications</h3>
                  <div className="notification-settings">
                    <div className="notification-item">
                      <div className="notification-info">
                        <h4>Course Updates</h4>
                        <p>Get notified when new lessons are added to your enrolled courses</p>
                      </div>
                      <label className="toggle-switch">
                        <input type="checkbox" defaultChecked />
                        <span className="toggle-slider"></span>
                      </label>
                    </div>
                    <div className="notification-item">
                      <div className="notification-info">
                        <h4>Mock Interview Reminders</h4>
                        <p>Receive reminders 24 hours before your scheduled mock interviews</p>
                      </div>
                      <label className="toggle-switch">
                        <input type="checkbox" defaultChecked />
                        <span className="toggle-slider"></span>
                      </label>
                    </div>
                    <div className="notification-item">
                      <div className="notification-info">
                        <h4>Weekly Progress Report</h4>
                        <p>Get a summary of your learning progress every week</p>
                      </div>
                      <label className="toggle-switch">
                        <input type="checkbox" defaultChecked />
                        <span className="toggle-slider"></span>
                      </label>
                    </div>
                    <div className="notification-item">
                      <div className="notification-info">
                        <h4>New Question Alerts</h4>
                        <p>Get notified when new interview questions are added</p>
                      </div>
                      <label className="toggle-switch">
                        <input type="checkbox" />
                        <span className="toggle-slider"></span>
                      </label>
                    </div>
                    <div className="notification-item">
                      <div className="notification-info">
                        <h4>Marketing Emails</h4>
                        <p>Receive updates about new features, tips, and special offers</p>
                      </div>
                      <label className="toggle-switch">
                        <input type="checkbox" />
                        <span className="toggle-slider"></span>
                      </label>
                    </div>
                  </div>
                </div>
              </div>

              {/* Privacy Section */}
              <div className={`profile-section-content ${activeSection === 'privacy' ? 'active' : ''}`}>
                <div className="section-header">
                  <h2>Privacy Settings</h2>
                  <p>Control your data and privacy preferences</p>
                </div>

                <div className="profile-card">
                  <h3>Profile Visibility</h3>
                  <div className="privacy-settings">
                    <div className="privacy-item">
                      <div className="privacy-info">
                        <h4>Public Profile</h4>
                        <p>Allow others to view your profile and progress</p>
                      </div>
                      <label className="toggle-switch">
                        <input type="checkbox" defaultChecked />
                        <span className="toggle-slider"></span>
                      </label>
                    </div>
                    <div className="privacy-item">
                      <div className="privacy-info">
                        <h4>Show Learning Stats</h4>
                        <p>Display your solved problems and course completions publicly</p>
                      </div>
                      <label className="toggle-switch">
                        <input type="checkbox" defaultChecked />
                        <span className="toggle-slider"></span>
                      </label>
                    </div>
                  </div>
                </div>

                <div className="profile-card danger-zone">
                  <h3>Danger Zone</h3>
                  <div className="danger-actions">
                    <div className="danger-item">
                      <div className="danger-info">
                        <h4>Download Your Data</h4>
                        <p>Request a copy of all your data including progress, notes, and account information</p>
                      </div>
                      <button className="btn-outline">Request Data</button>
                    </div>
                    <div className="danger-item">
                      <div className="danger-info">
                        <h4>Delete Account</h4>
                        <p>Permanently delete your account and all associated data. This action cannot be undone.</p>
                      </div>
                      <button className="btn-danger">Delete Account</button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="footer">
        <div className="container">
          <div className="footer-grid">
            <div className="footer-col">
              <div className="footer-brand">
                <i className="fas fa-vector-square"></i>
                <span>Vector</span>
              </div>
              <p>Master your interview skills.</p>
            </div>
            <div className="footer-col">
              <h4>Product</h4>
              <ul>
                <li><a href="#courses">Courses</a></li>
                <li><a href="#questions">Questions</a></li>
                <li><a href="#interviews">Mock Interviews</a></li>
              </ul>
            </div>
            <div className="footer-col">
              <h4>Company</h4>
              <ul>
                <li><a href="#about">About</a></li>
                <li><a href="#careers">Careers</a></li>
              </ul>
            </div>
            <div className="footer-col">
              <h4>Support</h4>
              <ul>
                <li><a href="#help">Help Center</a></li>
                <li><a href="#terms">Terms</a></li>
              </ul>
            </div>
          </div>
          <div className="footer-bottom">
            <p>&copy; 2025 Vector. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  );
};
