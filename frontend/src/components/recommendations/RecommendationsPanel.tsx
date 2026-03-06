import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import recommendationService, { type RecommendedQuestion } from '../../services/recommendation.service';
import '../../styles/recommendationsPanel.css';

export const RecommendationsPanel: React.FC = () => {
  const navigate = useNavigate();
  const [recommendations, setRecommendations] = useState<RecommendedQuestion[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchRecommendations();
  }, []);

  const fetchRecommendations = async () => {
    try {
      setLoading(true);
      const data = await recommendationService.getRecommendations(5);
      setRecommendations(data);
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to fetch recommendations');
    } finally {
      setLoading(false);
    }
  };

  const handleQuestionClick = (questionId: string) => {
    navigate(`/questions/${questionId}`);
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

  if (loading) {
    return (
      <div className="recommendations-panel">
        <div className="recommendations-header">
          <h3>
            <i className="fas fa-lightbulb"></i> Recommended for You
          </h3>
        </div>
        <div className="recommendations-loading">
          <i className="fas fa-spinner fa-spin"></i>
        </div>
      </div>
    );
  }

  if (error || recommendations.length === 0) {
    return (
      <div className="recommendations-panel">
        <div className="recommendations-header">
          <h3>
            <i className="fas fa-lightbulb"></i> Recommended for You
          </h3>
        </div>
        <div className="recommendations-empty">
          <p>{error || 'No recommendations available'}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="recommendations-panel">
      <div className="recommendations-header">
        <h3>
          <i className="fas fa-lightbulb"></i> Recommended for You
        </h3>
        <button className="view-all-btn" onClick={() => navigate('/questions')}>
          View All
        </button>
      </div>

      <div className="recommendations-list">
        {recommendations.map((question) => (
          <div
            key={question.id}
            className="recommendation-card"
            onClick={() => handleQuestionClick(question.id)}
          >
            <div className="recommendation-header">
              <div className="recommendation-title">{question.title}</div>
              <span
                className="recommendation-difficulty"
                style={{ color: getDifficultyColor(question.difficulty) }}
              >
                {question.difficulty}
              </span>
            </div>
            <div className="recommendation-meta">
              <span className="recommendation-category">
                <i className="fas fa-tag"></i> {question.category}
              </span>
              {question.acceptanceRate && (
                <span className="recommendation-acceptance">
                  <i className="fas fa-check"></i> {question.acceptanceRate}%
                </span>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
