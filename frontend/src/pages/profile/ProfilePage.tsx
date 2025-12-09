import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';
import { Navbar } from '../../components/layout/Navbar';
import api from '../../services/api';
import { coachService } from '../../services/coach.service';
import subscriptionService from '../../services/subscription.service';
import type { Subscription } from '../../services/subscription.service';
import '../../styles/style.css';
import '../../styles/dashboard.css';
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
  const { user, isAuthenticated, isLoading, logout, refreshUser } = useAuth();
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
  const [profilePicture, setProfilePicture] = useState<File | null>(null);
  const [profilePicturePreview, setProfilePicturePreview] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState('');
  const [errorMessage, setErrorMessage] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [hasCoachApplication, setHasCoachApplication] = useState(false);
  const [coachApplication, setCoachApplication] = useState<{ status: string; adminNotes?: string } | null>(null);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [currentSubscription, setCurrentSubscription] = useState<Subscription | null>(null);
  const [subscriptionLoading, setSubscriptionLoading] = useState(false);

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
      
      // Check if user has a coach application (only for students, silently handle 404)
      if (user.role === 'student') {
        coachService.getMyApplication()
          .then(app => {
            setHasCoachApplication(!!app);
            if (app) {
              setCoachApplication({
                status: app.status,
                adminNotes: app.adminNotes
              });
            } else {
              setCoachApplication(null);
            }
          })
          .catch((err) => {
            // 404 is expected when no application exists - don't log as error
            // The error is already marked as isExpected404 in the API interceptor
            if (!err?.isExpected404 && err?.response?.status !== 404) {
              console.error('Failed to fetch coach application status:', err);
            }
            setHasCoachApplication(false);
            setCoachApplication(null);
          });
      } else {
        setHasCoachApplication(false);
        setCoachApplication(null);
      }

      // Fetch current subscription
      if (isAuthenticated) {
        setSubscriptionLoading(true);
        subscriptionService.getCurrentSubscription()
          .then(sub => {
            setCurrentSubscription(sub);
          })
          .catch(err => {
            console.error('Failed to fetch subscription:', err);
            setCurrentSubscription(null);
          })
          .finally(() => {
            setSubscriptionLoading(false);
          });
      }
    }
  }, [user, isAuthenticated]);

  const getUserInitials = () => {
    if (user?.firstName && user?.lastName) {
      return `${user.firstName[0]}${user.lastName[0]}`.toUpperCase();
    }
    if (user?.email) {
      return user.email.substring(0, 2).toUpperCase();
    }
    return 'U';
  };

  const handleDeleteAccount = async () => {
    setIsDeleting(true);
    setErrorMessage('');
    
    try {
      await api.delete('/users/me');
      setShowDeleteModal(false);
      // Clear local storage and redirect
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      await logout();
      navigate(ROUTES.HOME);
    } catch (err: unknown) {
      const error = err as { response?: { data?: { error?: string } } };
      setErrorMessage(error.response?.data?.error || 'Failed to delete account. Please try again.');
      setIsDeleting(false);
    }
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
      // Upload profile picture first if selected
      if (profilePicture) {
        try {
          const formData = new FormData();
          formData.append('file', profilePicture);
          
          const response = await api.post('/users/me/profile-picture', formData, {
            headers: { 
              'Content-Type': 'multipart/form-data'
            }
          });
          
          if (response.data?.profilePictureUrl) {
            setSuccessMessage('Profile picture updated successfully!');
            // Update user context if available
            if (user) {
              user.profilePictureUrl = response.data.profilePictureUrl;
            }
            setProfilePicture(null);
            setProfilePicturePreview(null);
          }
        } catch (uploadErr: unknown) {
          console.error('Failed to upload profile picture:', uploadErr);
          const error = uploadErr as { response?: { data?: { error?: string } } };
          setErrorMessage(error.response?.data?.error || 'Profile picture upload failed. Please try again.');
          setIsSaving(false);
          return; // Stop if image upload fails
        }
      }

      // Update profile data
      await api.put('/users/me', profileData);
      setSuccessMessage('Profile updated successfully!');

      // Refresh user context to get updated data
      await refreshUser();
    } catch (err) {
      const errorMsg = err && typeof err === 'object' && 'response' in err
        ? (err.response as { data?: { error?: string } })?.data?.error
        : undefined;
      setErrorMessage(errorMsg || 'Failed to update profile');
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
      <Navbar />

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
                      <label htmlFor="role">Role</label>
                      <div style={{ 
                        display: 'flex', 
                        alignItems: 'center', 
                        gap: '0.5rem',
                        padding: '0.75rem',
                        background: 'var(--bg-gray-50)',
                        borderRadius: 'var(--radius-md)',
                        border: '1px solid var(--border-color)'
                      }}>
                        <span style={{
                          padding: '0.25rem 0.75rem',
                          borderRadius: 'var(--radius-sm)',
                          fontSize: '0.875rem',
                          fontWeight: '600',
                          textTransform: 'uppercase',
                          backgroundColor: user?.role === 'admin' ? '#fbbf24' :
                                         user?.role === 'coach' ? '#3b82f6' : '#10b981',
                          color: 'white'
                        }}>
                          {user?.role === 'admin' ? 'Admin' :
                           user?.role === 'coach' ? 'Coach' : 'Student'}
                        </span>
                        {user?.role === 'coach' && (
                          <i className="fas fa-chalkboard-teacher" style={{ color: '#3b82f6' }}></i>
                        )}
                        {user?.role === 'admin' && (
                          <i className="fas fa-shield-alt" style={{ color: '#fbbf24' }}></i>
                        )}
                        {user?.role === 'student' && (
                          <i className="fas fa-user-graduate" style={{ color: '#10b981' }}></i>
                        )}
                      </div>
                      <small className="form-help">Your current role on the platform</small>
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

                {/* Coach Application Status or Apply Button */}
                {user?.role === 'student' && (
                  <div className="profile-card" style={{ marginTop: '2rem' }}>
                    {hasCoachApplication && coachApplication ? (
                      <>
                        <h3 style={{ marginBottom: '1rem' }}>
                          <i className="fas fa-chalkboard-teacher" style={{ marginRight: '0.5rem', color: '#667eea' }}></i>
                          Coach Application Status
                        </h3>
                        <div style={{ marginBottom: '1rem' }}>
                          <strong>Status: </strong>
                          <span style={{
                            padding: '0.25rem 0.75rem',
                            borderRadius: '4px',
                            fontSize: '0.9rem',
                            fontWeight: 'bold',
                            backgroundColor: coachApplication.status === 'approved' ? '#d4edda' : 
                                           coachApplication.status === 'rejected' ? '#f8d7da' : '#fff3cd',
                            color: coachApplication.status === 'approved' ? '#155724' : 
                                  coachApplication.status === 'rejected' ? '#721c24' : '#856404'
                          }}>
                            {coachApplication.status === 'approved' ? '✓ Approved' : 
                             coachApplication.status === 'rejected' ? '✗ Rejected' : 
                             '⏳ Pending Review'}
                          </span>
                        </div>
                        {coachApplication.adminNotes && (
                          <div style={{ 
                            marginTop: '1rem', 
                            padding: '1rem', 
                            background: '#f5f5f5', 
                            borderRadius: '4px',
                            border: '1px solid #ddd'
                          }}>
                            <strong>Admin Notes:</strong>
                            <p style={{ marginTop: '0.5rem', color: '#333', whiteSpace: 'pre-wrap' }}>
                              {coachApplication.adminNotes}
                            </p>
                          </div>
                        )}
                        {coachApplication.status === 'rejected' && (
                          <div style={{ marginTop: '1rem' }}>
                            <Link 
                              to={ROUTES.COACH_APPLY}
                              className="btn-primary"
                              style={{ display: 'inline-block', textDecoration: 'none' }}
                            >
                              <i className="fas fa-redo" style={{ marginRight: '0.5rem' }}></i>
                              Reapply for Coaching
                            </Link>
                          </div>
                        )}
                      </>
                    ) : (
                      <>
                        <h3 style={{ color: '#667eea', marginBottom: '0.5rem' }}>
                          <i className="fas fa-chalkboard-teacher" style={{ marginRight: '0.5rem' }}></i>
                          Become a Coach
                        </h3>
                        <p style={{ color: '#666', marginBottom: '1rem', fontSize: '0.9rem' }}>
                          Share your expertise and help students prepare for technical interviews. Apply to become a coach on Vector.
                        </p>
                        <Link 
                          to={ROUTES.COACH_APPLY}
                          className="btn-primary"
                          style={{ display: 'inline-block', textDecoration: 'none' }}
                        >
                          <i className="fas fa-paper-plane" style={{ marginRight: '0.5rem' }}></i>
                          Apply to Become a Coach
                        </Link>
                      </>
                    )}
                  </div>
                )}
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
                  {subscriptionLoading ? (
                    <div style={{ textAlign: 'center', padding: '2rem' }}>
                      <i className="fas fa-spinner fa-spin" style={{ fontSize: '2rem', color: 'var(--primary)' }}></i>
                      <p style={{ marginTop: '1rem', color: 'var(--text-secondary)' }}>Loading subscription...</p>
                    </div>
                  ) : currentSubscription ? (
                    <div className="current-plan">
                      <div className={`plan-badge ${currentSubscription.planType === 'free' ? 'free' : 'premium'}`}>
                        <i className={currentSubscription.planType === 'free' ? 'fas fa-user' : 'fas fa-crown'}></i>
                        <span>{currentSubscription.plan?.name || 'Current Plan'}</span>
                      </div>
                      <div className="plan-details">
                        <div className="plan-info">
                          <h4>{currentSubscription.plan?.name || 'Current Plan'}</h4>
                          <p>
                            {currentSubscription.plan?.price === 0 
                              ? '$0.00 / month' 
                              : `$${currentSubscription.plan?.price.toFixed(2) || '0.00'} / ${currentSubscription.plan?.billingPeriod || 'month'}`}
                          </p>
                          {currentSubscription.planType === 'free' ? (
                            <p className="renewal-date">Upgrade to unlock premium features</p>
                          ) : (
                            <p className="renewal-date">
                              {currentSubscription.currentPeriodEnd && new Date(currentSubscription.currentPeriodEnd) < new Date('2099-12-31')
                                ? `Renews on ${new Date(currentSubscription.currentPeriodEnd).toLocaleDateString()}`
                                : 'Active subscription'}
                            </p>
                          )}
                        </div>
                        <div className="plan-actions">
                          {currentSubscription.planType === 'free' ? (
                            <Link to={ROUTES.SUBSCRIPTION_PLANS} className="btn-primary">Upgrade Plan</Link>
                          ) : (
                            <Link to={ROUTES.SUBSCRIPTION_PLANS} className="btn-secondary">Change Plan</Link>
                          )}
                        </div>
                      </div>
                    </div>
                  ) : (
                    <div style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-secondary)' }}>
                      <p>Unable to load subscription information</p>
                    </div>
                  )}
                </div>

                {/* Subscription Management Actions */}
                <div className="profile-card" style={{ marginTop: '24px' }}>
                  <h3>Subscription Management</h3>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '1rem', background: 'var(--bg-gray-50)', borderRadius: 'var(--radius-md)' }}>
                      <div>
                        <h4 style={{ marginBottom: '0.25rem', fontSize: '1rem', fontWeight: 600 }}>View Subscription Details</h4>
                        <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>See your current subscription status and billing information</p>
                      </div>
                      <Link to={ROUTES.SUBSCRIPTION_PLANS} className="btn-secondary">
                        <i className="fas fa-eye"></i> View Details
                      </Link>
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '1rem', background: 'var(--bg-gray-50)', borderRadius: 'var(--radius-md)' }}>
                      <div>
                        <h4 style={{ marginBottom: '0.25rem', fontSize: '1rem', fontWeight: 600 }}>Update Subscription</h4>
                        <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Upgrade, downgrade, or change your subscription plan</p>
                      </div>
                      <Link to={ROUTES.SUBSCRIPTION_PLANS} className="btn-secondary">
                        <i className="fas fa-edit"></i> Update Plan
                      </Link>
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '1rem', background: 'var(--bg-gray-50)', borderRadius: 'var(--radius-md)' }}>
                      <div>
                        <h4 style={{ marginBottom: '0.25rem', fontSize: '1rem', fontWeight: 600 }}>Payment Methods</h4>
                        <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Manage your payment methods and billing information</p>
                      </div>
                      <Link to={ROUTES.SUBSCRIPTION_PLANS} className="btn-secondary">
                        <i className="fas fa-credit-card"></i> Manage Payment
                      </Link>
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '1rem', background: 'var(--bg-gray-50)', borderRadius: 'var(--radius-md)' }}>
                      <div>
                        <h4 style={{ marginBottom: '0.25rem', fontSize: '1rem', fontWeight: 600 }}>Billing History</h4>
                        <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>View and download your past invoices</p>
                      </div>
                      <Link to={ROUTES.SUBSCRIPTION_PLANS} className="btn-secondary">
                        <i className="fas fa-file-invoice"></i> View Invoices
                      </Link>
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '1rem', background: '#fef2f2', borderRadius: 'var(--radius-md)', border: '1px solid #fecaca' }}>
                      <div>
                        <h4 style={{ marginBottom: '0.25rem', fontSize: '1rem', fontWeight: 600, color: '#991b1b' }}>Cancel Subscription</h4>
                        <p style={{ fontSize: '0.875rem', color: '#b91c1c' }}>Cancel your recurring subscription. Access continues until the end of billing period.</p>
                      </div>
                      <button 
                        className="btn-secondary" 
                        style={{ borderColor: '#ef4444', color: '#ef4444' }}
                        onClick={() => {
                          if (window.confirm('Are you sure you want to cancel your subscription? You will continue to have access until the end of your billing period.')) {
                            // TODO: Implement cancel subscription API call
                            alert('Cancel subscription functionality will be available once backend endpoints are implemented.');
                          }
                        }}
                      >
                        <i className="fas fa-times-circle"></i> Cancel
                      </button>
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

                {/* Change Password */}
                <div className="profile-card" style={{ marginTop: '24px' }}>
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

                {/* Two-Factor Authentication */}
                <div className="profile-card" style={{ marginTop: '24px' }}>
                  <h3>Two-Factor Authentication</h3>
                  <p style={{ marginBottom: '1rem', color: 'var(--text-secondary)' }}>
                    Enhance your account security. Add an extra layer of protection by enabling two-factor authentication.
                  </p>
                  <button
                    type="button"
                    className="btn-primary"
                    onClick={() => {
                      // TODO: Implement 2FA setup flow
                      alert('Two-factor authentication setup will be available soon.');
                    }}
                    disabled={isSaving}
                  >
                    <i className="fas fa-shield-alt"></i> Enable 2FA
                  </button>
                </div>

                <div className="profile-card danger-zone">
                  <h3>Danger Zone</h3>
                  <div className="danger-actions">
                    <div className="danger-item">
                      <div className="danger-info">
                        <h4>Delete Account</h4>
                        <p>Permanently delete your account and all associated data. This action cannot be undone.</p>
                      </div>
                      <button 
                        className="btn-danger" 
                        onClick={() => setShowDeleteModal(true)}
                        disabled={isDeleting}
                      >
                        {isDeleting ? 'Deleting...' : 'Delete Account'}
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Delete Account Confirmation Modal */}
      {showDeleteModal && (
        <div style={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundColor: 'rgba(0, 0, 0, 0.5)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 1000
        }}>
          <div style={{
            background: 'white',
            borderRadius: '12px',
            padding: '2rem',
            maxWidth: '500px',
            width: '90%',
            boxShadow: '0 10px 40px rgba(0,0,0,0.2)'
          }}>
            <h2 style={{ margin: '0 0 1rem 0', color: '#dc3545', fontSize: '1.5rem' }}>
              <i className="fas fa-exclamation-triangle" style={{ marginRight: '0.5rem' }}></i>
              Delete Account
            </h2>
            <p style={{ marginBottom: '1rem', color: '#666', lineHeight: '1.6' }}>
              <strong>Warning: This action cannot be undone!</strong>
            </p>
            <p style={{ marginBottom: '1.5rem', color: '#666', lineHeight: '1.6' }}>
              All your data will be permanently deleted, including:
            </p>
            <ul style={{ marginBottom: '1.5rem', paddingLeft: '1.5rem', color: '#666' }}>
              <li>Your profile and account information</li>
              <li>All your progress and achievements</li>
              <li>Your coach application (if any)</li>
              <li>All associated data and records</li>
            </ul>
            <p style={{ marginBottom: '1.5rem', color: '#dc3545', fontWeight: 'bold' }}>
              There is no way to recover your data after deletion.
            </p>
            {errorMessage && (
              <div style={{
                marginBottom: '1rem',
                padding: '0.75rem',
                background: '#fee',
                color: '#c33',
                borderRadius: '4px'
              }}>
                {errorMessage}
              </div>
            )}
            <div style={{ display: 'flex', gap: '1rem', justifyContent: 'flex-end' }}>
              <button
                onClick={() => setShowDeleteModal(false)}
                disabled={isDeleting}
                style={{
                  padding: '0.75rem 1.5rem',
                  border: '1px solid #ddd',
                  borderRadius: '8px',
                  background: 'white',
                  color: '#333',
                  cursor: isDeleting ? 'not-allowed' : 'pointer',
                  fontWeight: '600'
                }}
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteAccount}
                disabled={isDeleting}
                style={{
                  padding: '0.75rem 1.5rem',
                  border: 'none',
                  borderRadius: '8px',
                  background: isDeleting ? '#999' : '#dc3545',
                  color: 'white',
                  cursor: isDeleting ? 'not-allowed' : 'pointer',
                  fontWeight: '600'
                }}
              >
                {isDeleting ? (
                  <>
                    <i className="fas fa-spinner fa-spin" style={{ marginRight: '0.5rem' }}></i>
                    Deleting...
                  </>
                ) : (
                  'Yes, Delete My Account'
                )}
              </button>
            </div>
          </div>
        </div>
      )}

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
