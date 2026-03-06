import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Navbar } from '../../components/layout/Navbar';
import bookmarkService from '../../services/bookmark.service';
import type { BookmarkedQuestion } from '../../services/bookmark.service';
import BookmarkButton from '../../components/questions/BookmarkButton';
import '../../styles/bookmarkedQuestions.css';

const BookmarkedQuestionsPage: React.FC = () => {
  const [questions, setQuestions] = useState<BookmarkedQuestion[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filterDifficulty, setFilterDifficulty] = useState<string>('All');
  const [filterCategory, setFilterCategory] = useState<string>('All');
  const navigate = useNavigate();

  useEffect(() => {
    loadBookmarkedQuestions();
  }, []);

  const loadBookmarkedQuestions = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await bookmarkService.getBookmarkedQuestions();
      setQuestions(data);
    } catch (err: any) {
      console.error('Error loading bookmarked questions:', err);
      setError(err.response?.data?.error || 'Failed to load bookmarked questions');
    } finally {
      setLoading(false);
    }
  };

  const handleBookmarkRemoved = (questionId: string) => {
    setQuestions(prev => prev.filter(q => q.id !== questionId));
  };

  const handleQuestionClick = (questionId: string) => {
    navigate(`/questions/${questionId}`);
  };

  const filteredQuestions = questions.filter(q => {
    if (filterDifficulty !== 'All' && q.difficulty !== filterDifficulty) return false;
    if (filterCategory !== 'All' && q.category !== filterCategory) return false;
    return true;
  });

  const categories = Array.from(new Set(questions.map(q => q.category)));
  const difficulties = ['Easy', 'Medium', 'Hard'];

  const getDifficultyClass = (difficulty: string) => {
    return `difficulty-${difficulty.toLowerCase()}`;
  };

  if (loading) {
    return (
      <div className="bookmarked-questions-page">
        <div className="loading-state">
          <i className="fas fa-spinner fa-spin"></i>
          <p>Loading bookmarked questions...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bookmarked-questions-page">
        <div className="error-state">
          <i className="fas fa-exclamation-circle"></i>
          <p>{error}</p>
          <button onClick={loadBookmarkedQuestions} className="retry-button">
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="bookmarked-questions-page">
      <Navbar />
      <div className="page-header">
        <div className="header-content">
          <h1>
            <i className="fas fa-bookmark"></i>
            Bookmarked Questions
          </h1>
          <p className="subtitle">
            {questions.length} question{questions.length !== 1 ? 's' : ''} saved
          </p>
        </div>
      </div>

      {questions.length === 0 ? (
        <div className="empty-state">
          <i className="far fa-bookmark"></i>
          <h2>No bookmarked questions yet</h2>
          <p>Start bookmarking questions to build your personalized practice list.</p>
          <button onClick={() => navigate('/questions')} className="browse-button">
            <i className="fas fa-search"></i>
            Browse Questions
          </button>
        </div>
      ) : (
        <>
          <div className="filters-container">
            <div className="filter-group">
              <label>Difficulty:</label>
              <select
                value={filterDifficulty}
                onChange={(e) => setFilterDifficulty(e.target.value)}
                className="filter-select"
              >
                <option value="All">All</option>
                {difficulties.map(diff => (
                  <option key={diff} value={diff}>{diff}</option>
                ))}
              </select>
            </div>

            <div className="filter-group">
              <label>Category:</label>
              <select
                value={filterCategory}
                onChange={(e) => setFilterCategory(e.target.value)}
                className="filter-select"
              >
                <option value="All">All</option>
                {categories.map(cat => (
                  <option key={cat} value={cat}>{cat}</option>
                ))}
              </select>
            </div>

            <div className="filter-results">
              Showing {filteredQuestions.length} of {questions.length} questions
            </div>
          </div>

          <div className="questions-grid">
            {filteredQuestions.map(question => (
              <div
                key={question.id}
                className="question-card"
                onClick={() => handleQuestionClick(question.id)}
              >
                <div className="question-header">
                  <h3 className="question-title">{question.title}</h3>
                  <BookmarkButton
                    questionId={question.id}
                    size="small"
                    onBookmarkChange={(isBookmarked) => {
                      if (!isBookmarked) {
                        handleBookmarkRemoved(question.id);
                      }
                    }}
                  />
                </div>

                <div className="question-meta">
                  <span className={`difficulty-badge ${getDifficultyClass(question.difficulty)}`}>
                    {question.difficulty}
                  </span>
                  <span className="category-badge">{question.category}</span>
                  <span className="type-badge">{question.questionType}</span>
                </div>

                <div className="question-tags">
                  {question.tags.slice(0, 3).map((tag, index) => (
                    <span key={index} className="tag">
                      {tag}
                    </span>
                  ))}
                  {question.tags.length > 3 && (
                    <span className="tag more">+{question.tags.length - 3} more</span>
                  )}
                </div>

                <div className="question-stats">
                  <span className="stat">
                    <i className="fas fa-check-circle"></i>
                    {question.acceptanceRate}% Acceptance
                  </span>
                </div>
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
};

export default BookmarkedQuestionsPage;
