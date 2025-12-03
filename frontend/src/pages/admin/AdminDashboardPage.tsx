import { useEffect, useState } from 'react';
import { useAuth } from '../../hooks/useAuth';
import api from '../../services/api';
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
  const [stats, setStats] = useState<UserStats | null>(null);
  const [users, setUsers] = useState<UserData[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    fetchStats();
    fetchUsers();
  }, []);

  const fetchStats = async () => {
    try {
      const response = await api.get<UserStats>('/admin/stats');
      setStats(response.data);
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load statistics');
    }
  };

  const fetchUsers = async () => {
    try {
      const response = await api.get<{ users: UserData[] }>('/admin/users');
      setUsers(response.data.users);
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load users');
    } finally {
      setLoading(false);
    }
  };

  const handleLogout = async () => {
    await logout();
    window.location.href = '/login';
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

        {/* Users Table */}
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
                </tr>
              </thead>
              <tbody>
                {users.map((user) => (
                  <tr key={user.id}>
                    <td>{user.email}</td>
                    <td>{user.firstName} {user.lastName}</td>
                    <td>
                      <span className={`role-badge role-${user.role}`}>
                        {user.role}
                      </span>
                    </td>
                    <td>
                      {user.emailVerified ? (
                        <span className="verified-badge">✓ Verified</span>
                      ) : (
                        <span className="unverified-badge">✗ Not Verified</span>
                      )}
                    </td>
                    <td>{new Date(user.createdAt).toLocaleDateString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AdminDashboardPage;

