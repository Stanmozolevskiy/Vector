import { useEffect, useState } from 'react';
import { Navbar } from '../../components/layout/Navbar';
import coinsService, { type AchievementDefinition } from '../../services/coins.service';
import '../../styles/style.css';
import '../../styles/dashboard.css';

export const HowToEarnPage = () => {
  const [achievements, setAchievements] = useState<AchievementDefinition[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadAchievements();
  }, []);

  const loadAchievements = async () => {
    try {
      setLoading(true);
      const data = await coinsService.getAchievements();
      setAchievements(data);
      setError(null);
    } catch (err) {
      console.error('Failed to load achievements:', err);
      setError('Failed to load achievements. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="page-container">
      <Navbar />

      {/* How to Earn Content */}
      <section className="main-content">
        <div className="container">
                 {/* Header */}
                 <div className="page-header">
                   <h1>💡 How to earn points</h1>
                   <p className="subtitle" style={{ maxWidth: '700px', margin: '0 auto' }}>
                     Points are a measurement of your contributions to the Vector community.
                     Complete interviews, help others, and contribute quality content to earn more points.
                   </p>
                 </div>

          {/* Loading State */}
          {loading && (
            <div className="text-center" style={{ padding: '3rem' }}>
              <i className="fas fa-spinner fa-spin text-4xl text-blue-600 mb-4"></i>
              <p className="text-gray-600">Loading achievements...</p>
            </div>
          )}

          {/* Error State */}
          {error && (
            <div className="alert alert-error">
              <i className="fas fa-exclamation-triangle"></i>
              {error}
            </div>
          )}

          {/* Achievement List */}
          {!loading && !error && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem', maxWidth: '900px', margin: '0 auto' }}>
              {achievements.map((achievement) => (
                <div
                  key={achievement.activityType}
                  className="card"
                  style={{
                    display: 'flex',
                    alignItems: 'start',
                    padding: '1.5rem',
                    gap: '1rem',
                  }}
                >
                  {/* Icon */}
                  <div style={{ fontSize: '2.5rem', flexShrink: 0 }}>
                    {achievement.icon || '🪙'}
                  </div>

                  {/* Content */}
                  <div style={{ flex: 1 }}>
                    <h3 style={{ fontSize: '1.1rem', fontWeight: 600, marginBottom: '0.5rem', color: '#111827' }}>
                      {achievement.displayName}
                    </h3>
                    {achievement.description && (
                      <p style={{ fontSize: '0.9rem', color: '#6b7280', marginBottom: 0 }}>
                        {achievement.description}
                      </p>
                    )}
                  </div>

                  {/* Points Badge */}
                  <div
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '6px',
                      padding: '6px 16px',
                      backgroundColor: '#ede9fe',
                      borderRadius: '20px',
                      flexShrink: 0,
                    }}
                  >
                    <span style={{ fontSize: '1.2rem' }}>🪙</span>
                    <span style={{ fontWeight: 700, color: '#7c3aed', fontSize: '0.95rem' }}>
                      {achievement.coinsAwarded}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </section>
    </div>
  );
};
