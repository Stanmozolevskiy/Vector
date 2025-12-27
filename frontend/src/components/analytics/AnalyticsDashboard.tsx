import { useEffect, useState } from 'react';
import { analyticsService, type LearningAnalytics } from '../../services/analytics.service';
import { ProgressChart } from './ProgressChart';

export const AnalyticsDashboard = () => {
  const [analytics, setAnalytics] = useState<LearningAnalytics | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadAnalytics = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await analyticsService.getUserAnalytics();
      setAnalytics(data);
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to load analytics');
      console.error('Error loading analytics:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadAnalytics();
    
    // Listen for solution submission events
    const handleSolutionSubmitted = () => {
      loadAnalytics();
    };
    
    window.addEventListener('solutionSubmitted', handleSolutionSubmitted);
    window.addEventListener('storage', (e) => {
      if (e.key === 'solutionSubmitted') {
        loadAnalytics();
      }
    });
    
    return () => {
      window.removeEventListener('solutionSubmitted', handleSolutionSubmitted);
    };
  }, []);

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '400px' }}>
        <div style={{ textAlign: 'center' }}>
          <i className="fas fa-spinner fa-spin" style={{ fontSize: '2rem', color: '#3b82f6', marginBottom: '1rem' }}></i>
          <p style={{ color: '#6b7280' }}>Loading analytics...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '400px' }}>
        <div style={{ textAlign: 'center' }}>
          <i className="fas fa-exclamation-triangle" style={{ fontSize: '2rem', color: '#ef4444', marginBottom: '1rem' }}></i>
          <p style={{ color: '#6b7280' }}>{error}</p>
        </div>
      </div>
    );
  }

  if (!analytics) {
    return null;
  }

  // Prepare chart data
  const categoryData = Object.entries(analytics.questionsByCategory || {})
    .map(([label, value]) => ({ label, value }))
    .sort((a, b) => b.value - a.value)
    .slice(0, 8);

  const difficultyData = Object.entries(analytics.questionsByDifficulty || {})
    .map(([label, value]) => ({
      label,
      value,
      color: label === 'Easy' ? '#10b981' : label === 'Medium' ? '#f59e0b' : '#ef4444',
    }));

  const languageData = Object.entries(analytics.solutionsByLanguage || {})
    .map(([label, value]) => ({ label, value }))
    .sort((a, b) => b.value - a.value);

  // Identify weak areas (categories with fewer solved questions)
  const weakAreas = Object.entries(analytics.questionsByCategory || {})
    .map(([label, value]) => ({ label, value }))
    .sort((a, b) => a.value - b.value)
    .slice(0, 3)
    .filter(item => item.value === 0 || (categoryData.length > 0 && item.value < Math.max(...categoryData.map(d => d.value)) / 2));

  return (
    <div className="analytics-dashboard" style={{ padding: '1.5rem' }}>
      {/* Stats Overview */}
      <div className="analytics-stats-grid" style={{ 
        display: 'grid', 
        gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', 
        gap: '1rem', 
        marginBottom: '2rem' 
      }}>
        <div className="stat-card" style={{
          backgroundColor: '#fff',
          borderRadius: '8px',
          padding: '1.5rem',
          boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
            <div style={{
              width: '48px',
              height: '48px',
              borderRadius: '8px',
              backgroundColor: '#dbeafe',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}>
              <i className="fas fa-check-circle" style={{ fontSize: '1.5rem', color: '#3b82f6' }}></i>
            </div>
            <div>
              <div style={{ fontSize: '2rem', fontWeight: 700, color: '#111827' }}>
                {analytics.questionsSolved}
              </div>
              <div style={{ fontSize: '0.875rem', color: '#6b7280' }}>Problems Solved</div>
            </div>
          </div>
        </div>

        <div className="stat-card" style={{
          backgroundColor: '#fff',
          borderRadius: '8px',
          padding: '1.5rem',
          boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
            <div style={{
              width: '48px',
              height: '48px',
              borderRadius: '8px',
              backgroundColor: '#dcfce7',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}>
              <i className="fas fa-percentage" style={{ fontSize: '1.5rem', color: '#10b981' }}></i>
            </div>
            <div>
              <div style={{ fontSize: '2rem', fontWeight: 700, color: '#111827' }}>
                {analytics.successRate.toFixed(1)}%
              </div>
              <div style={{ fontSize: '0.875rem', color: '#6b7280' }}>Success Rate</div>
            </div>
          </div>
        </div>

        <div className="stat-card" style={{
          backgroundColor: '#fff',
          borderRadius: '8px',
          padding: '1.5rem',
          boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
            <div style={{
              width: '48px',
              height: '48px',
              borderRadius: '8px',
              backgroundColor: '#fef3c7',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}>
              <i className="fas fa-fire" style={{ fontSize: '1.5rem', color: '#f59e0b' }}></i>
            </div>
            <div>
              <div style={{ fontSize: '2rem', fontWeight: 700, color: '#111827' }}>
                {analytics.currentStreak}
              </div>
              <div style={{ fontSize: '0.875rem', color: '#6b7280' }}>Day Streak</div>
            </div>
          </div>
        </div>

        <div className="stat-card" style={{
          backgroundColor: '#fff',
          borderRadius: '8px',
          padding: '1.5rem',
          boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
            <div style={{
              width: '48px',
              height: '48px',
              borderRadius: '8px',
              backgroundColor: '#ede9fe',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}>
              <i className="fas fa-code" style={{ fontSize: '1.5rem', color: '#8b5cf6' }}></i>
            </div>
            <div>
              <div style={{ fontSize: '2rem', fontWeight: 700, color: '#111827' }}>
                {analytics.totalSubmissions}
              </div>
              <div style={{ fontSize: '0.875rem', color: '#6b7280' }}>Total Submissions</div>
            </div>
          </div>
        </div>
      </div>

      {/* Charts Grid */}
      <div className="analytics-charts-grid" style={{ 
        display: 'grid', 
        gridTemplateColumns: 'repeat(auto-fit, minmax(400px, 1fr))', 
        gap: '1.5rem',
        marginBottom: '2rem'
      }}>
        {/* Problems by Category */}
        {categoryData.length > 0 && (
          <div className="chart-card" style={{
            backgroundColor: '#fff',
            borderRadius: '8px',
            padding: '1.5rem',
            boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
          }}>
            <ProgressChart
              data={categoryData}
              type="bar"
              title="Problems by Category"
              height={300}
            />
          </div>
        )}

        {/* Problems by Difficulty */}
        {difficultyData.length > 0 && (
          <div className="chart-card" style={{
            backgroundColor: '#fff',
            borderRadius: '8px',
            padding: '1.5rem',
            boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
          }}>
            <ProgressChart
              data={difficultyData}
              type="bar"
              title="Problems by Difficulty"
              height={300}
            />
          </div>
        )}

        {/* Solutions by Language */}
        {languageData.length > 0 && (
          <div className="chart-card" style={{
            backgroundColor: '#fff',
            borderRadius: '8px',
            padding: '1.5rem',
            boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
          }}>
            <ProgressChart
              data={languageData}
              type="pie"
              title="Solutions by Language"
              height={300}
            />
          </div>
        )}
      </div>

      {/* Weak Areas & Additional Info */}
      <div className="analytics-additional-info" style={{ 
        display: 'grid', 
        gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', 
        gap: '1.5rem' 
      }}>
        {/* Weak Areas */}
        {weakAreas.length > 0 && (
          <div className="info-card" style={{
            backgroundColor: '#fff',
            borderRadius: '8px',
            padding: '1.5rem',
            boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
          }}>
            <h3 style={{ fontSize: '1rem', fontWeight: 600, marginBottom: '1rem', color: '#111827' }}>
              <i className="fas fa-exclamation-circle" style={{ marginRight: '0.5rem', color: '#f59e0b' }}></i>
              Areas to Improve
            </h3>
            <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
              {weakAreas.map((area) => (
                <li key={area.label} style={{ 
                  padding: '0.75rem 0', 
                  borderBottom: '1px solid #e5e7eb',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center'
                }}>
                  <span style={{ color: '#374151' }}>{area.label}</span>
                  <span style={{ 
                    fontSize: '0.875rem', 
                    color: '#6b7280',
                    fontWeight: 500
                  }}>
                    {area.value} solved
                  </span>
                </li>
              ))}
            </ul>
          </div>
        )}

        {/* Performance Metrics */}
        <div className="info-card" style={{
          backgroundColor: '#fff',
          borderRadius: '8px',
          padding: '1.5rem',
          boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
        }}>
          <h3 style={{ fontSize: '1rem', fontWeight: 600, marginBottom: '1rem', color: '#111827' }}>
            <i className="fas fa-tachometer-alt" style={{ marginRight: '0.5rem', color: '#3b82f6' }}></i>
            Performance Metrics
          </h3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span style={{ color: '#6b7280', fontSize: '0.875rem' }}>Avg Execution Time</span>
              <span style={{ fontWeight: 600, color: '#111827' }}>
                {analytics.averageExecutionTime > 0 
                  ? `${analytics.averageExecutionTime.toFixed(0)} ms`
                  : 'N/A'}
              </span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span style={{ color: '#6b7280', fontSize: '0.875rem' }}>Avg Memory Used</span>
              <span style={{ fontWeight: 600, color: '#111827' }}>
                {analytics.averageMemoryUsed > 0 
                  ? `${(analytics.averageMemoryUsed / 1024).toFixed(2)} MB`
                  : 'N/A'}
              </span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span style={{ color: '#6b7280', fontSize: '0.875rem' }}>Longest Streak</span>
              <span style={{ fontWeight: 600, color: '#111827' }}>{analytics.longestStreak} days</span>
            </div>
            {analytics.lastActivityDate && (
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ color: '#6b7280', fontSize: '0.875rem' }}>Last Activity</span>
                <span style={{ fontWeight: 600, color: '#111827', fontSize: '0.875rem' }}>
                  {new Date(analytics.lastActivityDate).toLocaleDateString()}
                </span>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

