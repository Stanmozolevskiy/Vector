import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';
import { Navbar } from '../../components/layout/Navbar';
import { analyticsService, type LearningAnalytics } from '../../services/analytics.service';
import { peerInterviewService, type ScheduledInterviewSession } from '../../services/peerInterview.service';
import '../../styles/style.css';
import '../../styles/dashboard.css';

export const DashboardPage = () => {
  const { user, isAuthenticated, isLoading } = useAuth();
  const navigate = useNavigate();
  const [analytics, setAnalytics] = useState<LearningAnalytics | null>(null);
  const [analyticsLoading, setAnalyticsLoading] = useState(true);
  const [upcomingInterviews, setUpcomingInterviews] = useState<ScheduledInterviewSession[]>([]);
  const [interviewsLoading, setInterviewsLoading] = useState(true);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      navigate(ROUTES.LOGIN);
    }
  }, [isAuthenticated, isLoading, navigate]);

  useEffect(() => {
    const loadDashboardData = async () => {
      if (!isAuthenticated) return;
      
      try {
        setAnalyticsLoading(true);
        setInterviewsLoading(true);
        
        console.log('[Dashboard] Loading dashboard data...');
        
        // First, rebuild analytics from existing data
        try {
          console.log('[Dashboard] Rebuilding analytics...');
          await analyticsService.rebuildAnalytics();
          console.log('[Dashboard] Analytics rebuilt successfully');
        } catch (rebuildError) {
          console.error('[Dashboard] Error rebuilding analytics:', rebuildError);
          // Continue even if rebuild fails
        }
        
        // Load analytics and upcoming interviews in parallel
        const [analyticsData, interviewsData] = await Promise.all([
          analyticsService.getUserAnalytics(),
          peerInterviewService.getUpcomingSessions()
        ]);
        
        console.log('[Dashboard] Analytics data:', analyticsData);
        console.log('[Dashboard] Interviews data:', interviewsData);
        
        setAnalytics(analyticsData);
        setUpcomingInterviews(interviewsData);
      } catch (err) {
        console.error('Error loading dashboard data:', err);
      } finally {
        setAnalyticsLoading(false);
        setInterviewsLoading(false);
      }
    };

    loadDashboardData();
  }, [isAuthenticated]);

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
      <Navbar />

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
                <div className="stat-value">{analyticsLoading ? '...' : (analytics?.questionsSolved || 0)}</div>
                <div className="stat-label">Problems Solved</div>
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-icon">
                <i className="fas fa-video"></i>
              </div>
              <div className="stat-info">
                <div className="stat-value">{analyticsLoading ? '...' : (analytics?.mockInterviewsCompleted || 0)}</div>
                <div className="stat-label">Mock Interviews</div>
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-icon">
                <i className="fas fa-fire"></i>
              </div>
              <div className="stat-info">
                <div className="stat-value">{analyticsLoading ? '...' : (analytics?.currentStreak || 0)}</div>
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
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
                  <h2>Problem Solving Progress</h2>
                  <Link to={ROUTES.PROGRESS} className="btn-outline" style={{ fontSize: '0.875rem', padding: '0.5rem 1rem' }}>
                    View Details
                  </Link>
                </div>
                <div className="problem-stats">
                  {['Easy', 'Medium', 'Hard'].map((difficulty) => {
                    const solved = analytics?.questionsByDifficulty?.[difficulty] || 0;
                    const total = analytics?.totalQuestionsByDifficulty?.[difficulty] || 0;
                    const percentage = total > 0 ? (solved / total) * 100 : 0;
                    const colorClass = difficulty.toLowerCase();
                    
                    return (
                      <div key={difficulty} className="problem-stat-item">
                        <div className="problem-stat-header">
                          <span className={`difficulty-badge ${colorClass}`}>{difficulty}</span>
                          <span>{analyticsLoading ? '...' : `${solved}/${total}`}</span>
                        </div>
                        <div className="progress-bar-container">
                          <div 
                            className={`progress-bar-fill ${colorClass}`} 
                            style={{ 
                              width: analyticsLoading ? '0%' : `${percentage}%`,
                              transition: 'width 0.3s ease'
                            }}
                          ></div>
                        </div>
                      </div>
                    );
                  })}
                </div>
                {analytics && analytics.successRate > 0 && (
                  <div style={{ marginTop: '1rem', paddingTop: '1rem', borderTop: '1px solid #e5e7eb' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <span style={{ fontSize: '0.875rem', color: '#6b7280' }}>Success Rate</span>
                      <span style={{ fontSize: '1rem', fontWeight: 600, color: '#111827' }}>
                        {analytics.successRate.toFixed(1)}%
                      </span>
                    </div>
                  </div>
                )}
              </div>
            </div>

            {/* Right Column */}
            <div className="dashboard-sidebar">
              {/* Watch Mock Interview */}
              <div className="dashboard-card">
                <h2>Watch Mock Interview</h2>
                <div className="video-container">
                  <video 
                    controls 
                    style={{ width: '100%', borderRadius: '8px', marginBottom: '1rem' }}
                    poster=""
                  >
                    <source 
                      src="https://dev-vector-user-uploads.s3.us-east-1.amazonaws.com/videos/mock-interviews/what-is-exponent.mp4" 
                      type="video/mp4" 
                    />
                    Your browser does not support the video tag.
                  </video>
                  <h4 style={{ margin: '0 0 0.5rem 0', fontSize: '1rem', color: 'var(--text-primary)' }}>
                    What Is Exponent? - Introduction to Mock Interviews
                  </h4>
                  <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', margin: '0' }}>
                    Learn how to prepare for technical interviews effectively with this introduction to mock interviews.
                  </p>
                </div>
              </div>

              {/* Upcoming Mock Interviews */}
              <div className="dashboard-card">
                <h2>Upcoming Interviews</h2>
                {interviewsLoading ? (
                  <div className="empty-state-small">
                    <i className="fas fa-spinner fa-spin" style={{ fontSize: '2rem', color: 'var(--text-secondary)', marginBottom: 'var(--spacing-sm)' }}></i>
                    <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', textAlign: 'center' }}>Loading...</p>
                  </div>
                ) : upcomingInterviews.length > 0 ? (
                  <div style={{ marginBottom: '1rem' }}>
                    {upcomingInterviews.slice(0, 3).map((interview) => (
                      <div key={interview.id} style={{ 
                        padding: '0.75rem', 
                        marginBottom: '0.5rem', 
                        border: '1px solid #e5e7eb', 
                        borderRadius: '6px',
                        cursor: 'pointer',
                        transition: 'all 0.2s'
                      }}
                      onClick={() => navigate(`/interview/${interview.liveSessionId || interview.id}`)}
                      onMouseOver={(e) => e.currentTarget.style.backgroundColor = '#f9fafb'}
                      onMouseOut={(e) => e.currentTarget.style.backgroundColor = 'transparent'}
                      >
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                          <div>
                            <div style={{ fontSize: '0.875rem', fontWeight: 500, color: '#111827', marginBottom: '0.25rem' }}>
                              {interview.interviewType === 'PracticeWithFriend' ? '👥 Practice with Friend' : 
                               interview.interviewType === 'MockInterview' ? '🎯 Mock Interview' : 
                               '💼 ' + interview.interviewType}
                            </div>
                            <div style={{ fontSize: '0.75rem', color: '#6b7280' }}>
                              {new Date(interview.scheduledStartAt).toLocaleString('en-US', {
                                month: 'short',
                                day: 'numeric',
                                hour: 'numeric',
                                minute: '2-digit'
                              })}
                            </div>
                          </div>
                          <span style={{ 
                            fontSize: '0.75rem', 
                            padding: '0.25rem 0.5rem', 
                            borderRadius: '4px',
                            backgroundColor: interview.status === 'Scheduled' ? '#dbeafe' : '#d1fae5',
                            color: interview.status === 'Scheduled' ? '#1e40af' : '#065f46'
                          }}>
                            {interview.status}
                          </span>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="empty-state-small">
                    <i className="fas fa-calendar-alt" style={{ fontSize: '2rem', color: 'var(--text-secondary)', marginBottom: 'var(--spacing-sm)' }}></i>
                    <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', textAlign: 'center' }}>No interviews scheduled</p>
                  </div>
                )}
                <Link to={ROUTES.FIND_PEER} className="btn-outline btn-full">Schedule Interview</Link>
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
                {analyticsLoading ? (
                  <div style={{ textAlign: 'center', padding: '1rem', color: 'var(--text-secondary)' }}>
                    <i className="fas fa-spinner fa-spin"></i>
                  </div>
                ) : (
                  <div className="goals-list">
                    <div className="goal-item">
                      <div className={`goal-checkbox ${(analytics?.questionsSolved || 0) >= 5 ? 'checked' : ''}`}>
                        {(analytics?.questionsSolved || 0) >= 5 && <i className="fas fa-check"></i>}
                      </div>
                      <span className="goal-text">Complete 5 problems ({Math.min(analytics?.questionsSolved || 0, 5)}/5)</span>
                    </div>
                    <div className="goal-item">
                      <div className="goal-checkbox">
                      </div>
                      <span className="goal-text">Watch 2 lessons</span>
                    </div>
                    <div className="goal-item">
                      <div className={`goal-checkbox ${(analytics?.mockInterviewsCompleted || 0) >= 3 ? 'checked' : ''}`}>
                        {(analytics?.mockInterviewsCompleted || 0) >= 3 && <i className="fas fa-check"></i>}
                      </div>
                      <span className="goal-text">Attend 3 mock interviews ({Math.min(analytics?.mockInterviewsCompleted || 0, 3)}/3)</span>
                    </div>
                  </div>
                )}
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
