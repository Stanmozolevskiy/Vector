import { useEffect, useState } from 'react';
import { useAuth } from '../../hooks/useAuth';
import api from '../../services/api';
import { coachService, type CoachApplication } from '../../services/coach.service';
import { adminService } from '../../services/admin.service';
import '../../styles/admin.css';

interface UserStats {
  totalUsers: number;
  verifiedUsers: number;
  unverifiedUsers: number;
  roleBreakdown: {
    students: number;
    coaches: number;
    admins: number;
  };
  recentUsers: Array<{
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    role: string;
    createdAt: string;
  }>;
}

interface UserData {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  emailVerified: boolean;
  createdAt: string;
  updatedAt: string;
}

const AdminDashboardPage = () => {
  const { user, logout } = useAuth();
  const [activeTab, setActiveTab] = useState<'users' | 'coach-applications'>('users');
  const [stats, setStats] = useState<UserStats | null>(null);
  const [users, setUsers] = useState<UserData[]>([]);
  const [coachApplications, setCoachApplications] = useState<CoachApplication[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [reviewingApp, setReviewingApp] = useState<string | null>(null);
  const [reviewNotes, setReviewNotes] = useState<Record<string, string>>({});
  const [reviewStatus, setReviewStatus] = useState<Record<string, 'approved' | 'rejected'>>({});
  const [updatingUser, setUpdatingUser] = useState<string | null>(null);
  const [deletingUser, setDeletingUser] = useState<string | null>(null);
  const [expandedApplications, setExpandedApplications] = useState<Set<string>>(new Set());

  useEffect(() => {
    fetchStats();
    fetchUsers();
    fetchCoachApplications();
  }, []);

  const fetchStats = async () => {
    try {
      const response = await api.get<UserStats>('/admin/stats');
      setStats(response.data);
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load statistics');
    }
  };

  const fetchUsers = async () => {
    try {
      const response = await api.get<{ users: UserData[] }>('/admin/users');
      setUsers(response.data.users);
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load users');
    } finally {
      setLoading(false);
    }
  };

  const fetchCoachApplications = async () => {
    try {
      const applications = await coachService.getAllApplications();
      setCoachApplications(applications);
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load coach applications');
    }
  };

  const handleReviewApplication = async (applicationId: string) => {
    const notes = reviewNotes[applicationId] || '';
    const status = reviewStatus[applicationId] || 'approved';

    if (status === 'rejected' && !notes.trim()) {
      setError('Please provide notes when rejecting an application');
      return;
    }

    setError('');
    setSuccess('');
    setReviewingApp(applicationId);

    try {
      await coachService.reviewApplication(applicationId, {
        status: status,
        adminNotes: notes || undefined,
      });
      setSuccess(`Application ${status} successfully`);
      // Clear review notes and status for this application
      setReviewNotes(prev => {
        const newNotes = { ...prev };
        delete newNotes[applicationId];
        return newNotes;
      });
      setReviewStatus(prev => {
        const newStatus = { ...prev };
        delete newStatus[applicationId];
        return newStatus;
      });
      setReviewingApp(null);
      await fetchCoachApplications();
      await fetchStats(); // Refresh stats to update coach count
      await fetchUsers(); // Refresh users to see role changes
    } catch (err: unknown) {
      const error = err as { response?: { data?: { error?: string } } };
      setError(error.response?.data?.error || 'Failed to review application');
      setReviewingApp(null);
    }
  };

  const handleUpdateUserRole = async (userId: string, newRole: string) => {
    if (!confirm(`Are you sure you want to change this user's role to ${newRole}?`)) {
      return;
    }

    setError('');
    setSuccess('');
    setUpdatingUser(userId);

    try {
      await adminService.updateUserRole(userId, newRole);
      setSuccess('User role updated successfully');
      await fetchUsers();
      await fetchStats();
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to update user role');
    } finally {
      setUpdatingUser(null);
    }
  };

  const handleDeleteUser = async (userId: string, userEmail: string) => {
    if (!confirm(`Are you sure you want to delete user ${userEmail}? This action cannot be undone.`)) {
      return;
    }

    setError('');
    setSuccess('');
    setDeletingUser(userId);

    try {
      await adminService.deleteUser(userId);
      setSuccess('User deleted successfully');
      await fetchUsers();
      await fetchStats();
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to delete user');
    } finally {
      setDeletingUser(null);
    }
  };

  const handleLogout = async () => {
    await logout();
    window.location.href = '/login';
  };

  const getStatusBadge = (status: string) => {
    const badges: Record<string, { text: string; className: string }> = {
      pending: { text: 'Pending', className: 'status-pending' },
      approved: { text: 'Approved', className: 'status-approved' },
      rejected: { text: 'Rejected', className: 'status-rejected' },
    };
    const badge = badges[status] || badges.pending;
    return <span className={`status-badge ${badge.className}`}>{badge.text}</span>;
  };

  if (loading) {
    return <div className="admin-loading">Loading admin dashboard...</div>;
  }

  return (
    <div className="admin-dashboard">
      {/* Header */}
      <header className="admin-header">
        <div className="admin-header-content">
          <div className="admin-logo">
            <h1>Vector Admin</h1>
          </div>
          <div className="admin-user-menu">
            <span className="admin-user-name">
              {user?.firstName} {user?.lastName}
            </span>
            <span className="admin-user-role">{user?.role}</span>
            <button onClick={handleLogout} className="admin-logout-btn">
              Logout
            </button>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <div className="admin-content">
        <h2 className="admin-title">Admin Dashboard</h2>

        {error && (
          <div className="admin-error">
            {error}
          </div>
        )}

        {success && (
          <div style={{
            background: '#d4edda',
            color: '#155724',
            padding: '1rem',
            borderRadius: '4px',
            marginBottom: '1rem',
          }}>
            {success}
          </div>
        )}

        {/* Statistics Cards */}
        {stats && (
          <div className="admin-stats-grid">
            <div className="admin-stat-card">
              <h3>Total Users</h3>
              <p className="stat-number">{stats.totalUsers}</p>
            </div>
            <div className="admin-stat-card">
              <h3>Verified Users</h3>
              <p className="stat-number">{stats.verifiedUsers}</p>
            </div>
            <div className="admin-stat-card">
              <h3>Students</h3>
              <p className="stat-number">{stats.roleBreakdown.students}</p>
            </div>
            <div className="admin-stat-card">
              <h3>Coaches</h3>
              <p className="stat-number">{stats.roleBreakdown.coaches}</p>
            </div>
            <div className="admin-stat-card">
              <h3>Admins</h3>
              <p className="stat-number">{stats.roleBreakdown.admins}</p>
            </div>
            <div className="admin-stat-card">
              <h3>Unverified</h3>
              <p className="stat-number">{stats.unverifiedUsers}</p>
            </div>
          </div>
        )}

        {/* Tabs */}
        <div style={{ display: 'flex', gap: '1rem', marginBottom: '2rem', borderBottom: '2px solid #eee' }}>
          <button
            onClick={() => setActiveTab('users')}
            style={{
              padding: '0.75rem 1.5rem',
              background: activeTab === 'users' ? '#667eea' : 'transparent',
              color: activeTab === 'users' ? 'white' : '#333',
              border: 'none',
              borderBottom: activeTab === 'users' ? '2px solid #667eea' : '2px solid transparent',
              cursor: 'pointer',
              fontWeight: activeTab === 'users' ? 'bold' : 'normal',
            }}
          >
            Users
          </button>
          <button
            onClick={() => setActiveTab('coach-applications')}
            style={{
              padding: '0.75rem 1.5rem',
              background: activeTab === 'coach-applications' 
                ? '#667eea' 
                : coachApplications.filter(a => a.status === 'pending').length > 0
                  ? '#fbbf24' // Highlight with yellow/orange when there are pending reviews
                  : 'transparent',
              color: activeTab === 'coach-applications' 
                ? 'white' 
                : coachApplications.filter(a => a.status === 'pending').length > 0
                  ? '#92400e' // Dark text on yellow background
                  : '#333',
              border: 'none',
              borderBottom: activeTab === 'coach-applications' 
                ? '2px solid #667eea' 
                : coachApplications.filter(a => a.status === 'pending').length > 0
                  ? '2px solid #fbbf24'
                  : '2px solid transparent',
              cursor: 'pointer',
              fontWeight: activeTab === 'coach-applications' || coachApplications.filter(a => a.status === 'pending').length > 0
                ? 'bold' 
                : 'normal',
            }}
          >
            Coach Applications ({coachApplications.filter(a => a.status === 'pending').length} pending)
          </button>
        </div>

        {/* Users Tab */}
        {activeTab === 'users' && (
          <div className="admin-section">
            <h3>All Users</h3>
            <div className="admin-table-container">
              <table className="admin-table">
                <thead>
                  <tr>
                    <th>Email</th>
                    <th>Name</th>
                    <th>Role</th>
                    <th>Verified</th>
                    <th>Created</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((u) => (
                    <tr key={u.id}>
                      <td>{u.email}</td>
                      <td>{u.firstName} {u.lastName}</td>
                      <td>
                        <span className={`role-badge role-${u.role}`}>
                          {u.role}
                        </span>
                      </td>
                      <td>
                        {u.emailVerified ? (
                          <span className="verified-badge">✓ Verified</span>
                        ) : (
                          <span className="unverified-badge">✗ Not Verified</span>
                        )}
                      </td>
                      <td>{new Date(u.createdAt).toLocaleDateString()}</td>
                      <td>
                        <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
                          {u.role !== 'admin' && (
                            <button
                              onClick={() => handleUpdateUserRole(u.id, 'admin')}
                              disabled={updatingUser === u.id}
                              style={{
                                padding: '0.25rem 0.5rem',
                                background: '#28a745',
                                color: 'white',
                                border: 'none',
                                borderRadius: '4px',
                                cursor: 'pointer',
                                fontSize: '0.85rem',
                              }}
                            >
                              {updatingUser === u.id ? 'Updating...' : 'Make Admin'}
                            </button>
                          )}
                          {u.role !== 'coach' && (
                            <button
                              onClick={() => handleUpdateUserRole(u.id, 'coach')}
                              disabled={updatingUser === u.id}
                              style={{
                                padding: '0.25rem 0.5rem',
                                background: '#17a2b8',
                                color: 'white',
                                border: 'none',
                                borderRadius: '4px',
                                cursor: 'pointer',
                                fontSize: '0.85rem',
                              }}
                            >
                              {updatingUser === u.id ? 'Updating...' : 'Make Coach'}
                            </button>
                          )}
                          {u.role !== 'student' && (
                            <button
                              onClick={() => handleUpdateUserRole(u.id, 'student')}
                              disabled={updatingUser === u.id}
                              style={{
                                padding: '0.25rem 0.5rem',
                                background: '#6c757d',
                                color: 'white',
                                border: 'none',
                                borderRadius: '4px',
                                cursor: 'pointer',
                                fontSize: '0.85rem',
                              }}
                            >
                              {updatingUser === u.id ? 'Updating...' : 'Make Student'}
                            </button>
                          )}
                          <button
                            onClick={() => handleDeleteUser(u.id, u.email)}
                            disabled={deletingUser === u.id || u.id === user?.id}
                            style={{
                              padding: '0.25rem 0.5rem',
                              background: u.id === user?.id ? '#ccc' : '#dc3545',
                              color: 'white',
                              border: 'none',
                              borderRadius: '4px',
                              cursor: u.id === user?.id ? 'not-allowed' : 'pointer',
                              fontSize: '0.85rem',
                            }}
                          >
                            {deletingUser === u.id ? 'Deleting...' : 'Delete'}
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* Coach Applications Tab */}
        {activeTab === 'coach-applications' && (
          <div className="admin-section">
            <h3>Coach Applications</h3>
            {coachApplications.length === 0 ? (
              <p>No coach applications found.</p>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
                {coachApplications.map((app) => {
                  const isExpanded = expandedApplications.has(app.id);
                  return (
                    <div
                      key={app.id}
                      style={{
                        background: 'white',
                        padding: '1.5rem',
                        borderRadius: '8px',
                        border: '1px solid #ddd',
                        boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
                      }}
                    >
                      <div 
                        style={{ 
                          display: 'flex', 
                          justifyContent: 'space-between', 
                          alignItems: 'start',
                          cursor: 'pointer',
                        }}
                        onClick={() => {
                          setExpandedApplications(prev => {
                            const newSet = new Set(prev);
                            if (newSet.has(app.id)) {
                              newSet.delete(app.id);
                            } else {
                              newSet.add(app.id);
                            }
                            return newSet;
                          });
                        }}
                      >
                        <div style={{ flex: 1 }}>
                          <h4 style={{ margin: 0, marginBottom: '0.5rem' }}>{app.userName}</h4>
                          <p style={{ margin: 0, color: '#666', fontSize: '0.9rem' }}>{app.userEmail}</p>
                        </div>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                          {getStatusBadge(app.status)}
                          <i 
                            className={`fas fa-chevron-${isExpanded ? 'up' : 'down'}`}
                            style={{ color: '#666', fontSize: '0.9rem' }}
                          ></i>
                        </div>
                      </div>

                      {isExpanded && (
                        <div style={{ marginTop: '1.5rem', paddingTop: '1.5rem', borderTop: '1px solid #eee' }}>
                          <div style={{ marginBottom: '1rem' }}>
                            <strong>Motivation:</strong>
                            <p style={{ marginTop: '0.5rem', color: '#333', whiteSpace: 'pre-wrap' }}>{app.motivation}</p>
                          </div>

                          {app.experience && (
                            <div style={{ marginBottom: '1rem' }}>
                              <strong>Experience:</strong>
                              <p style={{ marginTop: '0.5rem', color: '#333', whiteSpace: 'pre-wrap' }}>{app.experience}</p>
                            </div>
                          )}

                          {app.specialization && (
                            <div style={{ marginBottom: '1rem' }}>
                              <strong>Specialization:</strong>
                              <p style={{ marginTop: '0.5rem', color: '#333' }}>{app.specialization}</p>
                            </div>
                          )}

                          {app.imageUrls && app.imageUrls.length > 0 && (
                            <div style={{ marginBottom: '1rem' }}>
                              <strong>Portfolio Images:</strong>
                              <div style={{ 
                                display: 'grid', 
                                gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))', 
                                gap: '1rem', 
                                marginTop: '0.5rem' 
                              }}>
                                {app.imageUrls.map((url, index) => (
                                  <div 
                                    key={index}
                                    style={{
                                      position: 'relative',
                                      width: '100%',
                                      paddingTop: '100%',
                                      borderRadius: '8px',
                                      overflow: 'hidden',
                                      border: '1px solid #ddd',
                                      cursor: 'pointer',
                                    }}
                                    onClick={(e) => {
                                      e.stopPropagation();
                                      window.open(url, '_blank');
                                    }}
                                  >
                                    <img
                                      src={url}
                                      alt={`Portfolio ${index + 1}`}
                                      style={{
                                        position: 'absolute',
                                        top: 0,
                                        left: 0,
                                        width: '100%',
                                        height: '100%',
                                        objectFit: 'cover',
                                      }}
                                      onError={(e) => {
                                        const target = e.target as HTMLImageElement;
                                        target.style.display = 'none';
                                        const parent = target.parentElement;
                                        if (parent) {
                                          parent.innerHTML = '<i class="fas fa-image" style="font-size: 2rem; color: #ccc; position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%);"></i>';
                                        }
                                      }}
                                    />
                                  </div>
                                ))}
                              </div>
                            </div>
                          )}

                          {app.adminNotes && (
                            <div style={{ marginBottom: '1rem', padding: '0.75rem', background: '#f5f5f5', borderRadius: '4px' }}>
                              <strong>Admin Notes:</strong>
                              <p style={{ marginTop: '0.5rem', color: '#333', whiteSpace: 'pre-wrap' }}>{app.adminNotes}</p>
                            </div>
                          )}

                          {app.status === 'pending' && (
                            <div style={{ marginTop: '1.5rem', padding: '1rem', background: '#f8f9fa', borderRadius: '4px', border: '1px solid #ddd' }}>
                              <div style={{ marginBottom: '1rem' }}>
                                <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>
                                  Review Notes {reviewStatus[app.id] === 'rejected' && <span style={{ color: '#dc3545' }}>*</span>}:
                                </label>
                                <textarea
                                  value={reviewNotes[app.id] || ''}
                                  onChange={(e) => {
                                    setReviewNotes(prev => ({
                                      ...prev,
                                      [app.id]: e.target.value
                                    }));
                                  }}
                                  placeholder="Add notes for the applicant..."
                                  rows={3}
                                  style={{
                                    width: '100%',
                                    padding: '0.5rem',
                                    border: '1px solid #ddd',
                                    borderRadius: '4px',
                                    fontSize: '0.9rem',
                                    fontFamily: 'inherit',
                                  }}
                                  onClick={(e) => e.stopPropagation()}
                                />
                              </div>
                              <div style={{ display: 'flex', gap: '1rem', marginBottom: '1rem' }}>
                                <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer' }}>
                                  <input
                                    type="radio"
                                    checked={reviewStatus[app.id] === 'approved'}
                                    onChange={() => {
                                      setReviewStatus(prev => ({
                                        ...prev,
                                        [app.id]: 'approved'
                                      }));
                                    }}
                                    onClick={(e) => e.stopPropagation()}
                                  />
                                  Approve
                                </label>
                                <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer' }}>
                                  <input
                                    type="radio"
                                    checked={reviewStatus[app.id] === 'rejected'}
                                    onChange={() => {
                                      setReviewStatus(prev => ({
                                        ...prev,
                                        [app.id]: 'rejected'
                                      }));
                                    }}
                                    onClick={(e) => e.stopPropagation()}
                                  />
                                  Reject
                                </label>
                              </div>
                              <button
                                onClick={(e) => {
                                  e.stopPropagation();
                                  handleReviewApplication(app.id);
                                }}
                                disabled={reviewingApp === app.id}
                                style={{
                                  padding: '0.5rem 1rem',
                                  background: (reviewStatus[app.id] || 'approved') === 'approved' ? '#28a745' : '#dc3545',
                                  color: 'white',
                                  border: 'none',
                                  borderRadius: '4px',
                                  cursor: reviewingApp === app.id ? 'not-allowed' : 'pointer',
                                  fontWeight: 'bold',
                                  opacity: reviewingApp === app.id ? 0.6 : 1,
                                }}
                              >
                                {reviewingApp === app.id ? 'Processing...' : `${(reviewStatus[app.id] || 'approved') === 'approved' ? 'Approve' : 'Reject'} Application`}
                              </button>
                            </div>
                          )}

                          {app.reviewedAt && (
                            <div style={{ marginTop: '1rem', fontSize: '0.85rem', color: '#666' }}>
                              <p style={{ margin: 0 }}>
                                Reviewed on: {new Date(app.reviewedAt).toLocaleDateString()}
                                {app.reviewerName && ` by ${app.reviewerName}`}
                              </p>
                            </div>
                          )}

                          <div style={{ marginTop: '0.5rem', fontSize: '0.85rem', color: '#666' }}>
                            <p style={{ margin: 0 }}>
                              Applied on: {new Date(app.createdAt).toLocaleDateString()}
                            </p>
                          </div>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default AdminDashboardPage;
