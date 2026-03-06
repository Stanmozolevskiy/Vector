import { useState, useEffect, useCallback } from 'react';
import { questionService } from '../../services/question.service';
import type { QuestionList } from '../../services/question.service';
import './SystemDesignQuestionModal.css';

interface SystemDesignQuestionModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSelectQuestion: (question: QuestionList) => void;
}

export const SystemDesignQuestionModal = ({
  isOpen,
  onClose,
  onSelectQuestion,
}: SystemDesignQuestionModalProps) => {
  const [questions, setQuestions] = useState<QuestionList[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');

  // Fetch system design questions when modal opens
  useEffect(() => {
    if (isOpen) {
      fetchSystemDesignQuestions();
    }
  }, [isOpen]);

  const fetchSystemDesignQuestions = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const systemDesignQuestions = await questionService.getQuestions({
        questionType: 'System Design',
        isActive: true,
      });
      setQuestions(systemDesignQuestions);
    } catch (err) {
      console.error('Failed to fetch system design questions:', err);
      setError('Failed to load system design questions. Please try again.');
    } finally {
      setLoading(false);
    }
  }, []);

  const handleSelectQuestion = (question: QuestionList) => {
    onSelectQuestion(question);
    onClose();
  };

  const filteredQuestions = questions.filter((q) =>
    q.title.toLowerCase().includes(searchTerm.toLowerCase())
  );

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Select System Design Question</h2>
          <button className="modal-close-button" onClick={onClose} aria-label="Close">
            <i className="fas fa-times"></i>
          </button>
        </div>

        <div className="modal-body">
          <div className="search-container">
            <input
              type="text"
              className="search-input"
              placeholder="Search questions..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
            <i className="fas fa-search search-icon"></i>
          </div>

          {loading && (
            <div className="loading-container">
              <div className="loading-spinner"></div>
              <p>Loading system design questions...</p>
            </div>
          )}

          {error && (
            <div className="error-container">
              <i className="fas fa-exclamation-circle"></i>
              <p>{error}</p>
            </div>
          )}

          {!loading && !error && filteredQuestions.length === 0 && (
            <div className="empty-container">
              <i className="fas fa-inbox"></i>
              <p>No system design questions found.</p>
            </div>
          )}

          {!loading && !error && filteredQuestions.length > 0 && (
            <div className="questions-list">
              {filteredQuestions.map((question) => (
                <div
                  key={question.id}
                  className="question-item"
                  onClick={() => handleSelectQuestion(question)}
                >
                  <div className="question-header">
                    <h3 className="question-title">{question.title}</h3>
                    <span className={`difficulty-badge difficulty-${question.difficulty.toLowerCase()}`}>
                      {question.difficulty}
                    </span>
                  </div>
                  {question.category && (
                    <div className="question-category">
                      <i className="fas fa-tag"></i>
                      <span>{question.category}</span>
                    </div>
                  )}
                  <div className="question-actions">
                    <button className="select-button">
                      <i className="fas fa-check"></i>
                      Select Question
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
