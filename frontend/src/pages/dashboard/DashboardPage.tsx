import { useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';
import '../../styles/style.css';
import '../../styles/dashboard.css';

export const DashboardPage = () => {
  const { user, isAuthenticated, isLoading, logout } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      navigate(ROUTES.LOGIN);
    }
  }, [isAuthenticated, isLoading, navigate]);

  const handleLogout = () => {
    logout();
    navigate(ROUTES.HOME);
  };

  const getUserInitials = () => {
    if (user?.firstName && user?.lastName) {
      return `${user.firstName[0]}${user.lastName[0]}`.toUpperCase();
    }
    if (user?.email) {
      return user.email.substring(0, 2).toUpperCase();
    }
    return 'U';
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
    <div className="dashboard-page">
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
                <Link to={ROUTES.PROFILE}><i className="fas fa-user"></i> Profile</Link>
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

      {/* Dashboard Content */}
      <section className="dashboard-section">
        <div className="container-wide">
          <div className="dashboard-header">
            <div>
              <h1>Welcome back, {user?.firstName || user?.email?.split('@')[0] || 'User'}!</h1>
              <p>Here's your learning progress</p>
            </div>
            <Link to={ROUTES.DASHBOARD} className="btn-primary">Explore Courses</Link>
          </div>

          {/* Stats Overview */}
          <div className="stats-grid">
            <div className="stat-card">
              <div className="stat-icon">
                <i className="fas fa-graduation-cap"></i>
              </div>
              <div className="stat-info">
                <div className="stat-value">0</div>
                <div className="stat-label">Courses Enrolled</div>
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-icon">
                <i className="fas fa-check-circle"></i>
              </div>
              <div className="stat-info">
                <div className="stat-value">0</div>
                <div className="stat-label">Problems Solved</div>
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-icon">
                <i className="fas fa-video"></i>
              </div>
              <div className="stat-info">
                <div className="stat-value">0</div>
                <div className="stat-label">Mock Interviews</div>
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-icon">
                <i className="fas fa-fire"></i>
              </div>
              <div className="stat-info">
                <div className="stat-value">0</div>
                <div className="stat-label">Day Streak</div>
              </div>
            </div>
          </div>

          {/* Main Dashboard Grid */}
          <div className="dashboard-grid">
            {/* Left Column */}
            <div className="dashboard-main">
              {/* Continue Learning */}
              <div className="dashboard-card">
                <h2>Continue Learning</h2>
                <div className="empty-state">
                  <i className="fas fa-book-open" style={{ fontSize: '3rem', color: 'var(--text-secondary)', marginBottom: 'var(--spacing-md)' }}></i>
                  <p style={{ color: 'var(--text-secondary)' }}>No courses enrolled yet. Start learning today!</p>
                  <Link to={ROUTES.DASHBOARD} className="btn-primary" style={{ marginTop: 'var(--spacing-md)' }}>Browse Courses</Link>
                </div>
              </div>

              {/* Activity Chart */}
              <div className="dashboard-card">
                <h2>Learning Activity</h2>
                <div className="empty-state">
                  <i className="fas fa-chart-line" style={{ fontSize: '3rem', color: 'var(--text-secondary)', marginBottom: 'var(--spacing-md)' }}></i>
                  <p style={{ color: 'var(--text-secondary)' }}>Start learning to see your activity</p>
                </div>
              </div>

              {/* Problem Solving Progress */}
              <div className="dashboard-card">
                <h2>Problem Solving Progress</h2>
                <div className="problem-stats">
                  <div className="problem-stat-item">
                    <div className="problem-stat-header">
                      <span className="difficulty-badge easy">Easy</span>
                      <span>0/234</span>
                    </div>
                    <div className="progress-bar-container">
                      <div className="progress-bar-fill easy" style={{ width: '0%' }}></div>
                    </div>
                  </div>
                  <div className="problem-stat-item">
                    <div className="problem-stat-header">
                      <span className="difficulty-badge medium">Medium</span>
                      <span>0/456</span>
                    </div>
                    <div className="progress-bar-container">
                      <div className="progress-bar-fill medium" style={{ width: '0%' }}></div>
                    </div>
                  </div>
                  <div className="problem-stat-item">
                    <div className="problem-stat-header">
                      <span className="difficulty-badge hard">Hard</span>
                      <span>0/310</span>
                    </div>
                    <div className="progress-bar-container">
                      <div className="progress-bar-fill hard" style={{ width: '0%' }}></div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Right Column */}
            <div className="dashboard-sidebar">
              {/* Upcoming Mock Interviews */}
              <div className="dashboard-card">
                <h2>Upcoming Interviews</h2>
                <div className="empty-state-small">
                  <i className="fas fa-calendar-alt" style={{ fontSize: '2rem', color: 'var(--text-secondary)', marginBottom: 'var(--spacing-sm)' }}></i>
                  <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', textAlign: 'center' }}>No interviews scheduled</p>
                </div>
                <Link to={ROUTES.DASHBOARD} className="btn-outline btn-full">Schedule Interview</Link>
              </div>

              {/* Recent Achievements */}
              <div className="dashboard-card">
                <h2>Recent Achievements</h2>
                <div className="achievements-list">
                  <div className="achievement-item">
                    <div className="achievement-icon">
                      <i className="fas fa-user-check"></i>
                    </div>
                    <div className="achievement-info">
                      <h4>Welcome!</h4>
                      <p>Account created</p>
                    </div>
                  </div>
                </div>
              </div>

              {/* Learning Goals */}
              <div className="dashboard-card">
                <h2>This Week's Goals</h2>
                <div className="goals-list">
                  <div className="goal-item">
                    <div className="goal-checkbox"></div>
                    <span className="goal-text">Complete 10 problems</span>
                  </div>
                  <div className="goal-item">
                    <div className="goal-checkbox"></div>
                    <span className="goal-text">Watch 5 lessons</span>
                  </div>
                  <div className="goal-item">
                    <div className="goal-checkbox"></div>
                    <span className="goal-text">Attend 1 mock interview</span>
                  </div>
                  <div className="goal-item">
                    <div className="goal-checkbox"></div>
                    <span className="goal-text">Review 3 system designs</span>
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
