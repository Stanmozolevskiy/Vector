import React, { useState, useEffect } from 'react';
import './InterviewSurvey.css';
import { peerInterviewService } from '../services/peerInterview.service';
import { useAuth } from '../hooks/useAuth';

interface InterviewSurveyProps {
  sessionId: string;
  onComplete: () => void;
}

export const InterviewSurvey: React.FC<InterviewSurveyProps> = ({ sessionId, onComplete }) => {
  const { user } = useAuth();
  const [formData, setFormData] = useState({
    problemSolvingRating: 0,
    problemSolvingDescription: '',
    codingSkillsRating: 0,
    codingSkillsDescription: '',
    communicationRating: 0,
    communicationDescription: '',
    thingsDidWell: '',
    areasForImprovement: '',
    interviewerPerformanceRating: 0,
    interviewerPerformanceDescription: ''
  });

  const [submitting, setSubmitting] = useState(false);
  const [opponentId, setOpponentId] = useState<string | null>(null);

  // Load session to get opponent ID
  useEffect(() => {
    const loadSession = async () => {
      try {
        const sessionData = await peerInterviewService.getSession(sessionId);
        // Find the opponent (the other participant)
        // Check both interviewerId and intervieweeId to find opponent
        if (user?.id) {
          if (sessionData.interviewerId && sessionData.interviewerId !== user.id) {
            setOpponentId(sessionData.interviewerId);
          } else if (sessionData.intervieweeId && sessionData.intervieweeId !== user.id) {
            setOpponentId(sessionData.intervieweeId);
          }
        }
      } catch (error) {
        console.error('Error loading session:', error);
      }
    };
    loadSession();
  }, [sessionId, user?.id]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    // Validate required fields
    if (!formData.problemSolvingRating || !formData.codingSkillsRating || !formData.communicationRating) {
      alert('Please provide ratings for Problem solving, Coding skills, and Communication (1-5 stars)');
      return;
    }

    if (!formData.thingsDidWell.trim() || !formData.areasForImprovement.trim()) {
      alert('Please fill in both "Things you did well" and "Areas where you could improve" fields');
      return;
    }

    if (!opponentId) {
      alert('Unable to identify your interview partner. Please try again.');
      return;
    }

    setSubmitting(true);
    try {
      // Submit feedback to backend
      await peerInterviewService.submitFeedback({
        liveSessionId: sessionId,
        revieweeId: opponentId,
        problemSolvingRating: formData.problemSolvingRating,
        problemSolvingDescription: formData.problemSolvingDescription || undefined,
        codingSkillsRating: formData.codingSkillsRating,
        codingSkillsDescription: formData.codingSkillsDescription || undefined,
        communicationRating: formData.communicationRating,
        communicationDescription: formData.communicationDescription || undefined,
        thingsDidWell: formData.thingsDidWell,
        areasForImprovement: formData.areasForImprovement,
        interviewerPerformanceRating: formData.interviewerPerformanceRating || undefined,
        interviewerPerformanceDescription: formData.interviewerPerformanceDescription || undefined
      });
      
      onComplete();
    } catch (error) {
      console.error('Error submitting feedback:', error);
      alert('Failed to submit feedback. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleChange = (field: string, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const StarRating: React.FC<{ 
    rating: number; 
    onChange: (rating: number) => void;
    label: string;
  }> = ({ rating, onChange, label }) => {
    return (
      <div className="star-rating-input">
        <label>{label} *</label>
        <div className="star-rating-buttons">
          {[1, 2, 3, 4, 5].map((star) => (
            <button
              key={star}
              type="button"
              className={`star-btn ${star <= rating ? 'selected' : ''}`}
              onClick={() => onChange(star)}
            >
              ★
            </button>
          ))}
        </div>
      </div>
    );
  };

  return (
    <div className="survey-overlay">
      <div className="survey-modal">
        <div className="survey-header">
          <h2>Provide Feedback for Your Interview</h2>
          <p className="survey-subtitle">
            Help your partner improve by providing genuine feedback. Your partner will do the same for you.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="survey-form">
          {/* Problem Solving */}
          <div className="survey-question">
            <StarRating
              label="Problem solving"
              rating={formData.problemSolvingRating}
              onChange={(rating) => handleChange('problemSolvingRating', rating)}
            />
            <textarea
              className="survey-textarea"
              placeholder="Describe their problem-solving approach..."
              value={formData.problemSolvingDescription}
              onChange={(e) => handleChange('problemSolvingDescription', e.target.value)}
              rows={3}
            />
          </div>

          {/* Coding Skills */}
          <div className="survey-question">
            <StarRating
              label="Coding skills"
              rating={formData.codingSkillsRating}
              onChange={(rating) => handleChange('codingSkillsRating', rating)}
            />
            <textarea
              className="survey-textarea"
              placeholder="Describe their coding skills..."
              value={formData.codingSkillsDescription}
              onChange={(e) => handleChange('codingSkillsDescription', e.target.value)}
              rows={3}
            />
          </div>

          {/* Communication */}
          <div className="survey-question">
            <StarRating
              label="Communication"
              rating={formData.communicationRating}
              onChange={(rating) => handleChange('communicationRating', rating)}
            />
            <textarea
              className="survey-textarea"
              placeholder="Describe their communication skills..."
              value={formData.communicationDescription}
              onChange={(e) => handleChange('communicationDescription', e.target.value)}
              rows={3}
            />
          </div>

          {/* Things you did well */}
          <div className="survey-question">
            <label>Things you did well *</label>
            <textarea
              className="survey-textarea"
              placeholder="What did your partner do well during the interview?"
              value={formData.thingsDidWell}
              onChange={(e) => handleChange('thingsDidWell', e.target.value)}
              rows={4}
              required
            />
          </div>

          {/* Areas for improvement */}
          <div className="survey-question">
            <label>Areas where you could improve *</label>
            <textarea
              className="survey-textarea"
              placeholder="What areas could your partner improve?"
              value={formData.areasForImprovement}
              onChange={(e) => handleChange('areasForImprovement', e.target.value)}
              rows={4}
              required
            />
          </div>

          {/* Interviewer Performance */}
          <div className="survey-question">
            <StarRating
              label="Interviewer performance"
              rating={formData.interviewerPerformanceRating}
              onChange={(rating) => handleChange('interviewerPerformanceRating', rating)}
            />
            <textarea
              className="survey-textarea"
              placeholder="How did your partner perform as an interviewer?"
              value={formData.interviewerPerformanceDescription}
              onChange={(e) => handleChange('interviewerPerformanceDescription', e.target.value)}
              rows={3}
            />
          </div>

          <div className="survey-actions">
            <button type="submit" className="survey-submit-btn" disabled={submitting}>
              {submitting ? 'Submitting...' : 'Submit Feedback'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
