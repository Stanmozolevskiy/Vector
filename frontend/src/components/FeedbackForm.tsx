import React, { useState } from 'react';
import { peerInterviewService } from '../services/peerInterview.service';
import type { SubmitFeedbackRequest } from '../services/peerInterview.service';
import '../styles/FeedbackForm.css';

interface FeedbackFormProps {
  liveSessionId: string;
  opponentId: string;
  opponentName?: string;
  interviewType?: string;
  date?: string;
  onComplete: () => void;
  onCancel: () => void;
}

const StarRatingInput: React.FC<{
  rating: number;
  onRatingChange: (rating: number) => void;
  required?: boolean;
}> = ({ rating, onRatingChange, required = false }) => {
  return (
    <div className="star-rating-input">
      {[1, 2, 3, 4, 5].map((star) => (
        <button
          key={star}
          type="button"
          className={`star-btn ${star <= rating ? 'filled' : ''}`}
          onClick={() => onRatingChange(star)}
          aria-label={`${star} star${star !== 1 ? 's' : ''}`}
        >
          <i className="fas fa-star"></i>
        </button>
      ))}
      {required && rating === 0 && (
        <span className="star-required">* Required</span>
      )}
    </div>
  );
};

export const FeedbackForm: React.FC<FeedbackFormProps> = ({
  liveSessionId,
  opponentId,
  opponentName,
  interviewType = 'Data Structures & Algorithms',
  date,
  onComplete,
  onCancel,
}) => {
  const [formData, setFormData] = useState<SubmitFeedbackRequest>({
    liveSessionId,
    revieweeId: opponentId,
    problemSolvingRating: 0,
    problemSolvingDescription: '',
    codingSkillsRating: 0,
    codingSkillsDescription: '',
    communicationRating: 0,
    communicationDescription: '',
    thingsDidWell: '',
    areasForImprovement: '',
    interviewerPerformanceRating: 0,
    interviewerPerformanceDescription: '',
  });

  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.problemSolvingRating || formData.problemSolvingRating < 1) {
      newErrors.problemSolvingRating = 'Problem solving rating is required';
    }
    if (!formData.codingSkillsRating || formData.codingSkillsRating < 1) {
      newErrors.codingSkillsRating = 'Coding skills rating is required';
    }
    if (!formData.communicationRating || formData.communicationRating < 1) {
      newErrors.communicationRating = 'Communication rating is required';
    }
    if (!formData.thingsDidWell || formData.thingsDidWell.trim().length === 0) {
      newErrors.thingsDidWell = 'This field is required';
    }
    if (!formData.areasForImprovement || formData.areasForImprovement.trim().length === 0) {
      newErrors.areasForImprovement = 'This field is required';
    }
    if (!formData.interviewerPerformanceRating || formData.interviewerPerformanceRating < 1) {
      newErrors.interviewerPerformanceRating = 'Interviewer performance rating is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    setSubmitting(true);
    try {
      await peerInterviewService.submitFeedback(formData);
      onComplete();
    } catch (error: any) {
      console.error('Error submitting feedback:', error);
      alert('Failed to submit feedback. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="feedback-form-overlay" onClick={onCancel}>
      <div className="feedback-form-modal" onClick={(e) => e.stopPropagation()}>
        <button className="feedback-form-close" onClick={onCancel}>
          <i className="fas fa-times"></i>
        </button>

        <div className="feedback-form-header">
          <h2>Feedback for your interview</h2>
          <p className="feedback-form-subtitle">
            {interviewType}{date && `, ${date}`}
          </p>
          {opponentName && (
            <p className="feedback-form-opponent">
              Providing feedback for: <strong>{opponentName}</strong>
            </p>
          )}
        </div>

        <form onSubmit={handleSubmit} className="feedback-form">
          {/* Problem Solving */}
          <div className="feedback-form-section">
            <label className="feedback-form-label">
              Problem solving <span className="required">*</span>
            </label>
            <StarRatingInput
              rating={formData.problemSolvingRating || 0}
              onRatingChange={(rating) =>
                setFormData({ ...formData, problemSolvingRating: rating })
              }
              required
            />
            {errors.problemSolvingRating && (
              <span className="error-message">{errors.problemSolvingRating}</span>
            )}
            <textarea
              className="feedback-form-textarea"
              placeholder="Describe their problem-solving approach..."
              value={formData.problemSolvingDescription || ''}
              onChange={(e) =>
                setFormData({ ...formData, problemSolvingDescription: e.target.value })
              }
              rows={3}
            />
          </div>

          {/* Coding Skills */}
          <div className="feedback-form-section">
            <label className="feedback-form-label">
              Coding skills <span className="required">*</span>
            </label>
            <StarRatingInput
              rating={formData.codingSkillsRating || 0}
              onRatingChange={(rating) =>
                setFormData({ ...formData, codingSkillsRating: rating })
              }
              required
            />
            {errors.codingSkillsRating && (
              <span className="error-message">{errors.codingSkillsRating}</span>
            )}
            <textarea
              className="feedback-form-textarea"
              placeholder="Describe their coding skills..."
              value={formData.codingSkillsDescription || ''}
              onChange={(e) =>
                setFormData({ ...formData, codingSkillsDescription: e.target.value })
              }
              rows={3}
            />
          </div>

          {/* Communication */}
          <div className="feedback-form-section">
            <label className="feedback-form-label">
              Communication <span className="required">*</span>
            </label>
            <StarRatingInput
              rating={formData.communicationRating || 0}
              onRatingChange={(rating) =>
                setFormData({ ...formData, communicationRating: rating })
              }
              required
            />
            {errors.communicationRating && (
              <span className="error-message">{errors.communicationRating}</span>
            )}
            <textarea
              className="feedback-form-textarea"
              placeholder="Describe their communication skills..."
              value={formData.communicationDescription || ''}
              onChange={(e) =>
                setFormData({ ...formData, communicationDescription: e.target.value })
              }
              rows={3}
            />
          </div>

          {/* Things Done Well */}
          <div className="feedback-form-section">
            <label className="feedback-form-label">
              Things you did well <span className="required">*</span>
            </label>
            {errors.thingsDidWell && (
              <span className="error-message">{errors.thingsDidWell}</span>
            )}
            <textarea
              className="feedback-form-textarea"
              placeholder="What did they do well during the interview?"
              value={formData.thingsDidWell || ''}
              onChange={(e) =>
                setFormData({ ...formData, thingsDidWell: e.target.value })
              }
              rows={4}
              required
            />
          </div>

          {/* Areas for Improvement */}
          <div className="feedback-form-section">
            <label className="feedback-form-label">
              Areas where you could improve <span className="required">*</span>
            </label>
            {errors.areasForImprovement && (
              <span className="error-message">{errors.areasForImprovement}</span>
            )}
            <textarea
              className="feedback-form-textarea"
              placeholder="What areas could they improve?"
              value={formData.areasForImprovement || ''}
              onChange={(e) =>
                setFormData({ ...formData, areasForImprovement: e.target.value })
              }
              rows={4}
              required
            />
          </div>

          {/* Interviewer Performance */}
          <div className="feedback-form-section">
            <label className="feedback-form-label">
              Interviewer performance <span className="required">*</span>
            </label>
            <StarRatingInput
              rating={formData.interviewerPerformanceRating || 0}
              onRatingChange={(rating) =>
                setFormData({ ...formData, interviewerPerformanceRating: rating })
              }
              required
            />
            {errors.interviewerPerformanceRating && (
              <span className="error-message">{errors.interviewerPerformanceRating}</span>
            )}
            <textarea
              className="feedback-form-textarea"
              placeholder="How was their performance as an interviewer?"
              value={formData.interviewerPerformanceDescription || ''}
              onChange={(e) =>
                setFormData({ ...formData, interviewerPerformanceDescription: e.target.value })
              }
              rows={3}
            />
          </div>

          <div className="feedback-form-actions">
            <button type="button" className="btn-cancel" onClick={onCancel}>
              Cancel
            </button>
            <button type="submit" className="btn-submit" disabled={submitting}>
              {submitting ? 'Submitting...' : 'Submit Feedback'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

