import React from 'react';
import './FeedbackView.css';

export interface FeedbackData {
  interviewType: string;
  date: string;
  problemSolving: {
    rating: number;
    description: string;
  };
  codingSkills: {
    rating: number;
    description: string;
  };
  communication: {
    rating: number;
    description: string;
  };
  thingsDoneWell: string;
  areasForImprovement: string;
  interviewerPerformance: {
    rating: number;
    description: string;
  };
}

interface FeedbackViewProps {
  feedback: FeedbackData;
  onClose: () => void;
}

const StarRating: React.FC<{ rating: number }> = ({ rating }) => {
  return (
    <div className="star-rating">
      {[1, 2, 3, 4, 5].map((star) => (
        <i
          key={star}
          className={`fas fa-star ${star <= rating ? 'filled' : ''}`}
        ></i>
      ))}
    </div>
  );
};

const FeedbackSection: React.FC<{
  title: string;
  rating: number;
  description: string;
}> = ({ title, rating, description }) => {
  return (
    <div className="feedback-section-item">
      <div className="feedback-section-header">
        <span className="feedback-section-title">{title}:</span>
        <StarRating rating={rating} />
      </div>
      <p className="feedback-section-description">{description}</p>
    </div>
  );
};

export const FeedbackView: React.FC<FeedbackViewProps> = ({ feedback, onClose }) => {
  return (
    <div className="feedback-modal-overlay" onClick={onClose}>
      <div className="feedback-modal" onClick={(e) => e.stopPropagation()}>
        <button className="feedback-modal-close" onClick={onClose}>
          <i className="fas fa-times"></i>
        </button>

        <div className="feedback-modal-header">
          <h2>Feedback for your interview</h2>
          <p className="feedback-interview-details">
            {feedback.interviewType}, {feedback.date}
          </p>
        </div>

        <div className="feedback-coaching-section">
          <span>Want more feedback from an expert interviewer?</span>
          <button className="btn-try-coaching">Try coaching</button>
        </div>

        <div className="feedback-content">
          <FeedbackSection
            title="Problem solving"
            rating={feedback.problemSolving.rating}
            description={feedback.problemSolving.description}
          />

          <FeedbackSection
            title="Coding skills"
            rating={feedback.codingSkills.rating}
            description={feedback.codingSkills.description}
          />

          <FeedbackSection
            title="Communication"
            rating={feedback.communication.rating}
            description={feedback.communication.description}
          />

          <div className="feedback-section-item">
            <h3 className="feedback-section-title">Things you did well:</h3>
            <p className="feedback-section-text">{feedback.thingsDoneWell}</p>
          </div>

          <div className="feedback-section-item">
            <h3 className="feedback-section-title">Areas where you could improve:</h3>
            <p className="feedback-section-text">{feedback.areasForImprovement}</p>
          </div>

          <FeedbackSection
            title="Interviewer performance"
            rating={feedback.interviewerPerformance.rating}
            description={feedback.interviewerPerformance.description}
          />
        </div>
      </div>
    </div>
  );
};

