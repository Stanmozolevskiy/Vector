import { useEffect, useState } from 'react';
import { Navbar } from '../../components/layout/Navbar';
import coinsService, { type LeaderboardEntry, type AchievementDefinition } from '../../services/coins.service';
import '../../styles/style.css';
import '../../styles/dashboard.css';

export const LeaderboardAndEarnPage = () => {
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntry[]>([]);
  const [achievements, setAchievements] = useState<AchievementDefinition[]>([]);
  const [loading, setLoading] = useState(true);
  const [achievementsLoading, setAchievementsLoading] = useState(true);
  const [myRank, setMyRank] = useState<number | null>(null);
  const [myCoins, setMyCoins] = useState<number>(0);
  const [error, setError] = useState<string | null>(null);
  const [achievementsError, setAchievementsError] = useState<string | null>(null);
  const [timeFilter, setTimeFilter] = useState<'all-time' | 'last-30-days'>('all-time');

  useEffect(() => {
    loadMyCoins();
    loadLeaderboard();
    loadMyRank();
    loadAchievements();
  }, []);

  const loadMyCoins = async () => {
    try {
      const data = await coinsService.getMyCoins();
      setMyCoins(data.totalCoins);
    } catch (err) {
      console.error('Failed to load coins:', err);
    }
  };

  const loadLeaderboard = async () => {
    try {
      setLoading(true);
      const data = await coinsService.getLeaderboard(200);
      setLeaderboard(data);
      setError(null);
    } catch (err) {
      console.error('Failed to load leaderboard:', err);
      setError('Failed to load leaderboard. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const loadMyRank = async () => {
    try {
      const { rank } = await coinsService.getMyRank();
      setMyRank(rank);
    } catch (err) {
      console.error('Failed to load rank:', err);
    }
  };

  const loadAchievements = async () => {
    try {
      setAchievementsLoading(true);
      const data = await coinsService.getAchievements();
      setAchievements(data);
      setAchievementsError(null);
    } catch (err) {
      console.error('Failed to load achievements:', err);
      setAchievementsError('Failed to load achievements. Please try again.');
    } finally {
      setAchievementsLoading(false);
    }
  };

  const getRankIcon = (rank: number) => {
    if (rank === 1) return '🥇';
    if (rank === 2) return '🥈';
    if (rank === 3) return '🥉';
    return null;
  };

  const formatCoins = (coins: number) => {
    return coins.toLocaleString();
  };

  return (
    <div className="page-container">
      <Navbar />

      {/* Main Content */}
      <section className="main-content">
        <div className="container-wide">
          {/* Header with Star and Total Points */}
          <div style={{
            textAlign: 'center',
            marginBottom: '2rem',
            padding: '2rem',
            background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
            borderRadius: '12px',
            color: 'white'
          }}>
            <div style={{ fontSize: '4rem', marginBottom: '0.5rem' }}>⭐</div>
            <h1 style={{ 
              fontSize: '2rem', 
              fontWeight: 700, 
              marginBottom: '0.5rem',
              color: 'white'
            }}>
              You've earned {formatCoins(myCoins)} points!
            </h1>
            <p style={{ 
              fontSize: '1rem', 
              opacity: 0.9,
              maxWidth: '600px',
              margin: '0 auto',
              color: 'white'
            }}>
              Points are a measurement of your contributions to the Vector community. The better your contributions, the more points you'll receive.
            </p>
          </div>

          {/* Split Layout: Leaderboard (Left) | How to Earn (Right) */}
          <div style={{ 
            display: 'grid', 
            gridTemplateColumns: '1fr 1fr', 
            gap: '2rem',
            alignItems: 'start'
          }}>
            
            {/* LEFT SIDE: Leaderboard */}
            <div>
              <div style={{ marginBottom: '1.5rem' }}>
                <h2 style={{ 
                  fontSize: '1.5rem', 
                  fontWeight: 700, 
                  marginBottom: '0.5rem'
                }}>
                  Leaderboard
                </h2>
                {myRank && (
                  <p style={{ color: '#6b7280', fontSize: '0.95rem', marginBottom: '1rem' }}>
                    Your all-time rank is <strong style={{ color: '#6366f1' }}>#{myRank}</strong>
                  </p>
                )}

                {/* Tabs for Last 30 days / All-time */}
                <div style={{ 
                  display: 'flex', 
                  gap: '1rem', 
                  borderBottom: '2px solid #e5e7eb',
                  marginBottom: '1rem'
                }}>
                  <button
                    onClick={() => setTimeFilter('last-30-days')}
                    style={{
                      padding: '0.75rem 1rem',
                      background: 'none',
                      border: 'none',
                      borderBottom: timeFilter === 'last-30-days' ? '3px solid #6366f1' : '3px solid transparent',
                      color: timeFilter === 'last-30-days' ? '#6366f1' : '#6b7280',
                      fontWeight: timeFilter === 'last-30-days' ? 600 : 400,
                      cursor: 'pointer',
                      fontSize: '0.95rem',
                      transition: 'all 0.2s'
                    }}
                  >
                    Last 30 days
                  </button>
                  <button
                    onClick={() => setTimeFilter('all-time')}
                    style={{
                      padding: '0.75rem 1rem',
                      background: 'none',
                      border: 'none',
                      borderBottom: timeFilter === 'all-time' ? '3px solid #6366f1' : '3px solid transparent',
                      color: timeFilter === 'all-time' ? '#6366f1' : '#6b7280',
                      fontWeight: timeFilter === 'all-time' ? 600 : 400,
                      cursor: 'pointer',
                      fontSize: '0.95rem',
                      transition: 'all 0.2s'
                    }}
                  >
                    All-time
                  </button>
                </div>
              </div>

              {/* Loading State */}
              {loading && (
                <div className="text-center" style={{ padding: '3rem' }}>
                  <i className="fas fa-spinner fa-spin text-4xl text-blue-600 mb-4"></i>
                  <p className="text-gray-600">Loading leaderboard...</p>
                </div>
              )}

              {/* Error State */}
              {error && (
                <div className="alert alert-error" style={{ marginBottom: '1rem' }}>
                  <i className="fas fa-exclamation-triangle"></i>
                  {error}
                </div>
              )}

              {/* Leaderboard List */}
              {!loading && !error && (
                <div style={{ 
                  maxHeight: '600px', 
                  overflowY: 'auto',
                  background: 'white',
                  borderRadius: '8px',
                  border: '1px solid #e5e7eb',
                  scrollbarWidth: 'none', /* Firefox */
                  msOverflowStyle: 'none'  /* IE and Edge */
                } as React.CSSProperties & { scrollbarWidth?: string; msOverflowStyle?: string }}>
                  <style>{`
                    .leaderboard-list::-webkit-scrollbar {
                      display: none;
                    }
                  `}</style>
                  {leaderboard.length === 0 ? (
                    <div style={{ textAlign: 'center', padding: '3rem' }}>
                      <i className="fas fa-trophy text-4xl text-gray-300 mb-4"></i>
                      <p className="text-gray-500">No users on leaderboard yet</p>
                    </div>
                  ) : (
                    leaderboard.map((entry, index) => (
                      <div
                        key={entry.userId}
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          padding: '1rem',
                          borderBottom: index < leaderboard.length - 1 ? '1px solid #f3f4f6' : 'none',
                          transition: 'background 0.2s',
                          cursor: 'pointer'
                        }}
                        onMouseEnter={(e) => e.currentTarget.style.background = '#f9fafb'}
                        onMouseLeave={(e) => e.currentTarget.style.background = 'transparent'}
                      >
                        {/* Rank */}
                        <div style={{ 
                          width: '50px', 
                          textAlign: 'center', 
                          fontWeight: 600,
                          fontSize: '0.9rem',
                          color: '#374151'
                        }}>
                          {getRankIcon(entry.rank) || `#${entry.rank}`}
                        </div>

                        {/* User Info */}
                        <div style={{ 
                          flex: 1, 
                          display: 'flex', 
                          alignItems: 'center', 
                          gap: '0.75rem' 
                        }}>
                          {entry.profilePictureUrl ? (
                            <img
                              src={entry.profilePictureUrl}
                              alt={`${entry.firstName} ${entry.lastName}`}
                              style={{
                                width: '40px',
                                height: '40px',
                                borderRadius: '50%',
                                objectFit: 'cover',
                              }}
                            />
                          ) : (
                            <div
                              style={{
                                width: '40px',
                                height: '40px',
                                borderRadius: '50%',
                                backgroundColor: '#6366f1',
                                color: 'white',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                fontWeight: 600,
                                fontSize: '0.85rem',
                              }}
                            >
                              {entry.firstName[0]}
                              {entry.lastName[0]}
                            </div>
                          )}
                          <span style={{ fontWeight: 500, fontSize: '0.95rem', color: '#111827' }}>
                            {entry.firstName} {entry.lastName}
                          </span>
                        </div>

                        {/* Points */}
                        <div style={{ 
                          display: 'flex', 
                          alignItems: 'center', 
                          gap: '0.5rem',
                          color: '#f59e0b',
                          fontWeight: 600,
                          fontSize: '0.9rem'
                        }}>
                          <span>🪙</span>
                          <span>{entry.displayCoins}</span>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              )}
            </div>

            {/* RIGHT SIDE: How to Earn Points */}
            <div>
              <div style={{ marginBottom: '1.5rem' }}>
                <h2 style={{ 
                  fontSize: '1.5rem', 
                  fontWeight: 700, 
                  marginBottom: '0.5rem'
                }}>
                  How to earn points
                </h2>
              </div>

              {/* Loading State */}
              {achievementsLoading && (
                <div className="text-center" style={{ padding: '3rem' }}>
                  <i className="fas fa-spinner fa-spin text-4xl text-blue-600 mb-4"></i>
                  <p className="text-gray-600">Loading achievements...</p>
                </div>
              )}

              {/* Error State */}
              {achievementsError && (
                <div className="alert alert-error" style={{ marginBottom: '1rem' }}>
                  <i className="fas fa-exclamation-triangle"></i>
                  {achievementsError}
                </div>
              )}

              {/* Achievement List */}
              {!achievementsLoading && !achievementsError && (
                <div style={{ 
                  display: 'flex', 
                  flexDirection: 'column', 
                  gap: '0.75rem'
                }}>
                  {achievements.length === 0 ? (
                    <div className="card" style={{ padding: '3rem', textAlign: 'center' }}>
                      <p className="text-gray-500">No achievements available yet</p>
                    </div>
                  ) : (
                    achievements.map((achievement) => (
                      <div
                        key={achievement.activityType}
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          padding: '1rem',
                          background: 'white',
                          borderRadius: '8px',
                          border: '1px solid #e5e7eb',
                          gap: '1rem',
                        }}
                      >
                        {/* Icon */}
                        <div style={{ fontSize: '2rem', flexShrink: 0 }}>
                          {achievement.icon || '🪙'}
                        </div>

                        {/* Content */}
                        <div style={{ flex: 1 }}>
                          <h3 style={{ 
                            fontSize: '0.95rem', 
                            fontWeight: 600, 
                            marginBottom: '0.25rem', 
                            color: '#111827' 
                          }}>
                            {achievement.displayName}
                          </h3>
                          {achievement.description && (
                            <p style={{ 
                              fontSize: '0.8rem', 
                              color: '#6b7280', 
                              marginBottom: 0 
                            }}>
                              {achievement.description}
                            </p>
                          )}
                        </div>

                        {/* Points Badge */}
                        <div
                          style={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: '4px',
                            color: '#f59e0b',
                            fontWeight: 700,
                            fontSize: '0.9rem',
                            flexShrink: 0,
                          }}
                        >
                          <span>🪙</span>
                          <span>{achievement.coinsAwarded}</span>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      </section>
    </div>
  );
};
