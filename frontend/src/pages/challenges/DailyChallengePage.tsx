import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Navbar } from '../../components/layout/Navbar';
import challengeService, { type DailyChallengeResponse, type UserChallengeAttempt } from '../../services/challenge.service';
import '../../styles/dailyChallenge.css';

export const DailyChallengePage: React.FC = () => {
  const navigate = useNavigate();
  const [challengeData, setChallengeData] = useState<DailyChallengeResponse | null>(null);
  const [history, setHistory] = useState<UserChallengeAttempt[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [stats, setStats] = useState<any>(null);

  useEffect(() => {
    fetchChallengeData();
    fetchHistory();
    fetchStats();
  }, []);

  const fetchChallengeData = async () => {
    try {
      setLoading(true);
      const data = await challengeService.getDailyChallenge();
      setChallengeData(data);
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to fetch daily challenge');
    } finally {
      setLoading(false);
    }
  };

  const fetchHistory = async () => {
    try {
      const historyData = await challengeService.getChallengeHistory(7);
      setHistory(historyData);
    } catch (err) {
      console.error('Error fetching challenge history:', err);
    }
  };

  const fetchStats = async () => {
    try {
      const statsData = await challengeService.getChallengeStats();
      setStats(statsData);
    } catch (err) {
      console.error('Error fetching stats:', err);
    }
  };

  const handleSolveChallenge = async () => {
    if (!challengeData) return;

    try {
      await challengeService.startChallenge(challengeData.challenge.id);
      navigate(`/questions/${challengeData.challenge.questionId}`);
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to start challenge');
    }
  };

  const getDifficultyColor = (difficulty: string) => {
    switch (difficulty?.toLowerCase()) {
      case 'easy':
        return '#00b8a3';
      case 'medium':
        return '#ffc01e';
      case 'hard':
        return '#ff375f';
      default:
        return '#666';
    }
  };

  const formatTime = (seconds?: number) => {
    if (!seconds) return 'N/A';
    const minutes = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${minutes}m ${secs}s`;
  };

  if (loading) {
    return (
      <div className="daily-challenge-page">
        <Navbar />
        <div className="loading-container">
          <i className="fas fa-spinner fa-spin"></i>
          <p>Loading daily challenge...</p>
        </div>
      </div>
    );
  }

  if (error || !challengeData) {
    return (
      <div className="daily-challenge-page">
        <Navbar />
        <div className="error-container">
          <i className="fas fa-exclamation-triangle"></i>
          <p>{error || 'No daily challenge available'}</p>
        </div>
      </div>
    );
  }

  const { challenge, userAttempt } = challengeData;

  return (
    <div className="daily-challenge-page">
      <Navbar />
      
      <div className="challenge-container">
        <div className="challenge-header">
          <h1>
            <i className="fas fa-trophy"></i> Daily Challenge
          </h1>
          <p className="challenge-date">
            {new Date(challenge.date).toLocaleDateString('en-US', {
              weekday: 'long',
              year: 'numeric',
              month: 'long',
              day: 'numeric',
            })}
          </p>
        </div>

        {stats && (
          <div className="stats-grid">
            <div className="stat-card">
              <div className="stat-icon">
                <i className="fas fa-check-circle"></i>
              </div>
              <div className="stat-content">
                <div className="stat-value">{stats.completedChallenges}</div>
                <div className="stat-label">Completed</div>
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-icon">
                <i className="fas fa-fire"></i>
              </div>
              <div className="stat-content">
                <div className="stat-value">{stats.currentStreak}</div>
                <div className="stat-label">Day Streak</div>
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-icon">
                <i className="fas fa-coins"></i>
              </div>
              <div className="stat-content">
                <div className="stat-value">{stats.totalCoinsEarned}</div>
                <div className="stat-label">Coins Earned</div>
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-icon">
                <i className="fas fa-chart-line"></i>
              </div>
              <div className="stat-content">
                <div className="stat-value">{Math.round(stats.completionRate * 100)}%</div>
                <div className="stat-label">Success Rate</div>
              </div>
            </div>
          </div>
        )}

        <div className="challenge-main">
          <div className="challenge-card">
            <div className="challenge-card-header">
              <h2>{challenge.question.title}</h2>
              <div className="challenge-badges">
                <span
                  className="difficulty-badge"
                  style={{ color: getDifficultyColor(challenge.difficulty) }}
                >
                  {challenge.difficulty}
                </span>
                <span className="category-badge">{challenge.category}</span>
              </div>
            </div>

            <div className="challenge-description">
              {challenge.question.description.substring(0, 200)}...
            </div>

            <div className="challenge-stats">
              <div className="challenge-stat">
                <i className="fas fa-users"></i>
                <span>{challenge.attemptCount} attempts</span>
              </div>
              <div className="challenge-stat">
                <i className="fas fa-check"></i>
                <span>{challenge.completionCount} completed</span>
              </div>
              {challenge.question.acceptanceRate && (
                <div className="challenge-stat">
                  <i className="fas fa-percentage"></i>
                  <span>{challenge.question.acceptanceRate}% acceptance</span>
                </div>
              )}
            </div>

            {userAttempt ? (
              <div className="attempt-info">
                {userAttempt.isCompleted ? (
                  <>
                    <div className="attempt-completed">
                      <i className="fas fa-check-circle"></i>
                      <span>Completed!</span>
                    </div>
                    <div className="attempt-details">
                      <span>Time: {formatTime(userAttempt.timeSpentSeconds)}</span>
                      <span>Test Cases: {userAttempt.testCasesPassed}/{userAttempt.totalTestCases}</span>
                      <span>Coins Earned: {userAttempt.coinsEarned}</span>
                    </div>
                  </>
                ) : (
                  <div className="attempt-in-progress">
                    <i className="fas fa-clock"></i>
                    <span>In Progress</span>
                  </div>
                )}
                <button className="btn-secondary" onClick={handleSolveChallenge}>
                  {userAttempt.isCompleted ? 'View Solution' : 'Continue Challenge'}
                </button>
              </div>
            ) : (
              <button className="btn-primary" onClick={handleSolveChallenge}>
                <i className="fas fa-play"></i> Start Challenge
              </button>
            )}
          </div>

          {history.length > 0 && (
            <div className="challenge-history">
              <h3>Recent Challenges</h3>
              <div className="history-list">
                {history.map((attempt) => (
                  <div key={attempt.id} className="history-item">
                    <div className="history-date">
                      {new Date(attempt.challenge.date).toLocaleDateString('en-US', {
                        month: 'short',
                        day: 'numeric',
                      })}
                    </div>
                    <div className="history-details">
                      <div className="history-title">{attempt.challenge.question.title}</div>
                      <span
                        className="history-difficulty"
                        style={{ color: getDifficultyColor(attempt.challenge.difficulty) }}
                      >
                        {attempt.challenge.difficulty}
                      </span>
                    </div>
                    <div className="history-status">
                      {attempt.isCompleted ? (
                        <>
                          <i className="fas fa-check-circle" style={{ color: '#00b8a3' }}></i>
                          <span className="coins">+{attempt.coinsEarned}</span>
                        </>
                      ) : (
                        <span className="incomplete">Not Completed</span>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
