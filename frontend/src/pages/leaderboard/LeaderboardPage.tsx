import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Navbar } from '../../components/layout/Navbar';
import coinsService, { type LeaderboardEntry } from '../../services/coins.service';
import '../../styles/style.css';
import '../../styles/dashboard.css';

export const LeaderboardPage = () => {
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [myRank, setMyRank] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadLeaderboard();
    loadMyRank();
  }, []);

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

  const getRankIcon = (rank: number) => {
    if (rank === 1) return '🥇';
    if (rank === 2) return '🥈';
    if (rank === 3) return '🥉';
    return null;
  };

  return (
    <div className="page-container">
      <Navbar />

      {/* Leaderboard Content */}
      <section className="main-content">
        <div className="container-wide">
          {/* Header */}
          <div className="page-header">
            <h1>🏆 Leaderboard</h1>
            <p className="subtitle">Top 200 contributors with the most karma points</p>
            {myRank && (
              <p className="my-rank">
                Your all-time rank is <strong>#{myRank}</strong>
              </p>
            )}
          </div>

          {/* Link to How to Earn */}
          <div className="info-banner" style={{ marginBottom: '2rem' }}>
            <i className="fas fa-info-circle"></i>
            <span>
              Want to earn more karma?{' '}
              <Link to="/how-to-earn" style={{ color: '#6366f1', fontWeight: 600 }}>
                See all ways to earn points
              </Link>
            </span>
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
            <div className="alert alert-error">
              <i className="fas fa-exclamation-triangle"></i>
              {error}
            </div>
          )}

          {/* Leaderboard Table */}
          {!loading && !error && (
            <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
              <table className="data-table">
                <thead>
                  <tr>
                    <th style={{ width: '80px', textAlign: 'center' }}>Rank</th>
                    <th>User</th>
                    <th style={{ width: '150px', textAlign: 'right' }}>Karma Points</th>
                  </tr>
                </thead>
                <tbody>
                  {leaderboard.length === 0 ? (
                    <tr>
                      <td colSpan={3} style={{ textAlign: 'center', padding: '3rem' }}>
                        <i className="fas fa-trophy text-4xl text-gray-300 mb-4"></i>
                        <p className="text-gray-500">No users on leaderboard yet</p>
                      </td>
                    </tr>
                  ) : (
                    leaderboard.map((entry) => (
                      <tr key={entry.userId} className="hover-row">
                        <td style={{ textAlign: 'center', fontWeight: 600 }}>
                          {getRankIcon(entry.rank) || `#${entry.rank}`}
                        </td>
                        <td>
                          <Link
                            to={`/profile/${entry.userId}`}
                            style={{
                              display: 'flex',
                              alignItems: 'center',
                              textDecoration: 'none',
                              color: 'inherit',
                            }}
                          >
                            {entry.profilePictureUrl ? (
                              <img
                                src={entry.profilePictureUrl}
                                alt={`${entry.firstName} ${entry.lastName}`}
                                style={{
                                  width: '40px',
                                  height: '40px',
                                  borderRadius: '50%',
                                  marginRight: '12px',
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
                                  marginRight: '12px',
                                }}
                              >
                                {entry.firstName[0]}
                                {entry.lastName[0]}
                              </div>
                            )}
                            <span style={{ fontWeight: 500 }}>
                              {entry.firstName} {entry.lastName}
                            </span>
                          </Link>
                        </td>
                        <td style={{ textAlign: 'right' }}>
                          <div
                            style={{
                              display: 'inline-flex',
                              alignItems: 'center',
                              gap: '6px',
                              padding: '4px 12px',
                              backgroundColor: '#f3f4f6',
                              borderRadius: '20px',
                            }}
                          >
                            <span style={{ fontSize: '1.1rem' }}>🪙</span>
                            <span style={{ fontWeight: 600, color: '#374151' }}>
                              {entry.displayCoins}
                            </span>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </section>
    </div>
  );
};
