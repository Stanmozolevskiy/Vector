import React from 'react';
import '../styles/NoFeedbackView.css';

interface NoFeedbackViewProps {
  interviewType?: string;
  date?: string;
  onClose: () => void;
}

export const NoFeedbackView: React.FC<NoFeedbackViewProps> = ({
  interviewType = 'Data Structures & Algorithms',
  date,
  onClose,
}) => {
  return (
    <div className="no-feedback-overlay" onClick={onClose}>
      <div className="no-feedback-modal" onClick={(e) => e.stopPropagation()}>
        <button className="no-feedback-close" onClick={onClose}>
          <i className="fas fa-times"></i>
        </button>

        <div className="no-feedback-header">
          <h2>Feedback for your interview</h2>
          <p className="no-feedback-subtitle">
            {interviewType}{date && `, ${date}`}
          </p>
        </div>

        <div className="no-feedback-coaching-section">
          <span>Want more feedback from an expert interviewer?</span>
          <button className="btn-try-coaching">Try coaching</button>
        </div>

        <div className="no-feedback-content">
          <div className="no-feedback-icon">
            <i className="fas fa-comment-slash"></i>
          </div>
          <h3 className="no-feedback-title">Peer feedback not available yet</h3>
          <p className="no-feedback-message">
            Sorry, we're still waiting for your partner to submit feedback. Check back soon!
          </p>
        </div>
      </div>
    </div>
  );
};

