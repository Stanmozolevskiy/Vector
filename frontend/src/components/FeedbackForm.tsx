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

const YesNoButton: React.FC<{
  value: string | null;
  onChange: (value: string) => void;
  required?: boolean;
}> = ({ value, onChange, required = false }) => {
  return (
    <div className="yes-no-input">
      <div className="yes-no-buttons">
        <button
          type="button"
          className={`yes-no-btn ${value === 'yes' ? 'selected' : ''}`}
          onClick={() => onChange('yes')}
        >
          Yes
        </button>
        <button
          type="button"
          className={`yes-no-btn ${value === 'no' ? 'selected' : ''}`}
          onClick={() => onChange('no')}
        >
          No
        </button>
      </div>
      {required && !value && (
        <span className="yes-no-required">* Required</span>
      )}
    </div>
  );
};

export const FeedbackForm: React.FC<FeedbackFormProps> = ({
  liveSessionId,
  opponentId,
  opponentName: _opponentName,
  interviewType: _interviewType = 'Data Structures & Algorithms',
  date,
  onComplete,
  onCancel,
}) => {
  const [didSessionHappen, setDidSessionHappen] = useState<string | null>(null);
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
    audioVideoIssues: null,
    codeEditorIssues: null,
    additionalFeedback: '',
    wantEmailIntroduction: null,
  });

  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    // Did session happen is always required
    if (!didSessionHappen) {
      newErrors.didSessionHappen = 'Please indicate if the session happened';
    }

    // If session happened, partner feedback fields are required
    if (didSessionHappen === 'yes') {
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
      // Only submit partner feedback if session happened
      const feedbackToSubmit: SubmitFeedbackRequest = {
        ...formData,
        didSessionHappen: didSessionHappen === 'yes',
        // Clear partner feedback fields if session didn't happen
        ...(didSessionHappen === 'no' ? {
          problemSolvingRating: undefined,
          problemSolvingDescription: '',
          codingSkillsRating: undefined,
          codingSkillsDescription: '',
          communicationRating: undefined,
          communicationDescription: '',
          thingsDidWell: '',
          areasForImprovement: '',
          interviewerPerformanceRating: undefined,
          interviewerPerformanceDescription: '',
        } : {}),
      };
      
      await peerInterviewService.submitFeedback(feedbackToSubmit);
      onComplete();
    } catch (error: any) {
      console.error('Error submitting feedback:', error);
      alert('Failed to submit feedback. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return '';
    try {
      const date = new Date(dateStr);
      const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
      return days[date.getDay()];
    } catch {
      return dateStr;
    }
  };

  return (
    <div className="feedback-form-overlay" onClick={onCancel}>
      <div className="feedback-form-modal" onClick={(e) => e.stopPropagation()}>
        <button className="feedback-form-close" onClick={onCancel}>
          <i className="fas fa-times"></i>
        </button>

        <div className="feedback-form-header">
          <h2>How did your interview {date ? `on ${formatDate(date)}` : 'today'} go?</h2>
          <p className="feedback-form-subtitle">
            Help your partner improve by providing genuine feedback. Your partner will do the same for you.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="feedback-form">
          {/* Did this session happen? */}
          <div className="feedback-form-section">
            <label className="feedback-form-label">
              Did this session happen? <span className="required">*</span>
            </label>
            <YesNoButton
              value={didSessionHappen}
              onChange={(value) => setDidSessionHappen(value)}
              required={true}
            />
            {errors.didSessionHappen && (
              <span className="error-message">{errors.didSessionHappen}</span>
            )}
          </div>

          {/* Partner Feedback - Only show if session happened */}
          {didSessionHappen === 'yes' && (
            <>
              {/* Problem Solving */}
              <div className="feedback-form-section">
                <label className="feedback-form-label">
                  How were your partner's problem solving skills? <span className="required">*</span>
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
              </div>

              {/* Coding Skills */}
              <div className="feedback-form-section">
                <label className="feedback-form-label">
                  How were your partner's coding skills? <span className="required">*</span>
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
              </div>

              {/* Communication */}
              <div className="feedback-form-section">
                <label className="feedback-form-label">
                  How were your partner's communication skills? <span className="required">*</span>
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
              </div>

              {/* Things Done Well */}
              <div className="feedback-form-section">
                <label className="feedback-form-label">
                  What did your partner do well during the session? <span className="required">*</span>
                </label>
                {errors.thingsDidWell && (
                  <span className="error-message">{errors.thingsDidWell}</span>
                )}
                <textarea
                  className="feedback-form-textarea"
                  placeholder="What are your partner's strengths? What impressed you?"
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
                  What could your partner improve? <span className="required">*</span>
                </label>
                {errors.areasForImprovement && (
                  <span className="error-message">{errors.areasForImprovement}</span>
                )}
                <textarea
                  className="feedback-form-textarea"
                  placeholder="What should your partner improve? How would you advise them to get better?"
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
                  How did your partner perform as your interviewer? <span className="required">*</span>
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
              </div>
            </>
          )}

          {/* Audio/Video Issues - Always show */}
          <div className="feedback-form-section">
            <label className="feedback-form-label">
              Did you experience any audio or video issues during today's session?
            </label>
            <YesNoButton
              value={formData.audioVideoIssues || null}
              onChange={(value) =>
                setFormData({ ...formData, audioVideoIssues: value })
              }
            />
          </div>

          {/* Code Editor Issues - Always show */}
          <div className="feedback-form-section">
            <label className="feedback-form-label">
              Did you experience any issues with the code editor during today's session?
            </label>
            <YesNoButton
              value={formData.codeEditorIssues || null}
              onChange={(value) =>
                setFormData({ ...formData, codeEditorIssues: value })
              }
            />
          </div>

          {/* Additional Feedback for Exponent - Always show */}
          <div className="feedback-form-section">
            <label className="feedback-form-label">
              Any additional feedback for Exponent?
            </label>
            <textarea
              className="feedback-form-textarea"
              placeholder="What issues did you encounter? How can we improve?"
              value={formData.additionalFeedback || ''}
              onChange={(e) =>
                setFormData({ ...formData, additionalFeedback: e.target.value })
              }
              rows={4}
            />
          </div>

          {/* Email Introduction - Always show */}
          <div className="feedback-form-section">
            <label className="feedback-form-label">
              Do you want an email introduction to your partner?
              <i className="fas fa-info-circle" style={{ marginLeft: '8px', color: '#6b7280', cursor: 'help' }}></i>
            </label>
            <YesNoButton
              value={formData.wantEmailIntroduction || null}
              onChange={(value) =>
                setFormData({ ...formData, wantEmailIntroduction: value })
              }
            />
          </div>

          <div className="feedback-form-actions">
            <button type="button" className="btn-cancel" onClick={onCancel}>
              Cancel
            </button>
            <button type="submit" className="btn-submit" disabled={submitting}>
              {submitting ? 'Submitting...' : 'Submit'}
            </button>
          </div>

          <div className="feedback-form-footer">
            <p>Got more to say? Let us know at <a href="mailto:practice@tryexponent.com">practice@tryexponent.com</a></p>
          </div>
        </form>
      </div>
    </div>
  );
};
