import React, { useState } from 'react';
import './InterviewSurvey.css';

interface InterviewSurveyProps {
  sessionId: string;
  onComplete: () => void;
}

export const InterviewSurvey: React.FC<InterviewSurveyProps> = ({ sessionId, onComplete }) => {
  const [formData, setFormData] = useState({
    didSessionHappen: '',
    audioVideoIssues: '',
    codeEditorIssues: '',
    additionalFeedback: '',
    wantEmailIntroduction: ''
  });

  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!formData.didSessionHappen) {
      alert('Please answer if the session happened');
      return;
    }

    setSubmitting(true);
    try {
      // TODO: Submit survey data to backend
      // await peerInterviewService.submitSurvey(sessionId, formData);
      console.log('Survey data:', formData);
      
      // Mark session as Completed to prevent rejoin popup
      try {
        const { peerInterviewService } = await import('../services/peerInterview.service');
        await peerInterviewService.updateSessionStatus(sessionId, 'Completed');
      } catch (error) {
        console.error('Failed to update session status:', error);
        // Continue even if status update fails
      }
      
      // Mark survey as completed in localStorage
      localStorage.setItem(`survey_completed_${sessionId}`, 'true');
      
      onComplete();
    } catch (error) {
      console.error('Error submitting survey:', error);
      alert('Failed to submit survey. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleChange = (field: string, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  return (
    <div className="survey-overlay">
      <div className="survey-modal">
        <div className="survey-header">
          <h2>How did your interview today go?</h2>
          <p className="survey-subtitle">
            Help your partner improve by providing genuine feedback. Your partner will do the same for you.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="survey-form">
          <div className="survey-question">
            <label>Did this session happen? *</label>
            <div className="survey-buttons">
              <button
                type="button"
                className={`survey-btn ${formData.didSessionHappen === 'yes' ? 'selected' : ''}`}
                onClick={() => handleChange('didSessionHappen', 'yes')}
              >
                Yes
              </button>
              <button
                type="button"
                className={`survey-btn ${formData.didSessionHappen === 'no' ? 'selected' : ''}`}
                onClick={() => handleChange('didSessionHappen', 'no')}
              >
                No
              </button>
            </div>
          </div>

          <div className="survey-question">
            <label>Did you experience any audio or video issues during today's session?</label>
            <div className="survey-buttons">
              <button
                type="button"
                className={`survey-btn ${formData.audioVideoIssues === 'yes' ? 'selected' : ''}`}
                onClick={() => handleChange('audioVideoIssues', 'yes')}
              >
                Yes
              </button>
              <button
                type="button"
                className={`survey-btn ${formData.audioVideoIssues === 'no' ? 'selected' : ''}`}
                onClick={() => handleChange('audioVideoIssues', 'no')}
              >
                No
              </button>
            </div>
          </div>

          <div className="survey-question">
            <label>Did you experience any issues with the code editor during today's session?</label>
            <div className="survey-buttons">
              <button
                type="button"
                className={`survey-btn ${formData.codeEditorIssues === 'yes' ? 'selected' : ''}`}
                onClick={() => handleChange('codeEditorIssues', 'yes')}
              >
                Yes
              </button>
              <button
                type="button"
                className={`survey-btn ${formData.codeEditorIssues === 'no' ? 'selected' : ''}`}
                onClick={() => handleChange('codeEditorIssues', 'no')}
              >
                No
              </button>
            </div>
          </div>

          <div className="survey-question">
            <label>Any additional feedback for Exponent?</label>
            <textarea
              className="survey-textarea"
              placeholder="What issues did you encounter? How can we improve?"
              value={formData.additionalFeedback}
              onChange={(e) => handleChange('additionalFeedback', e.target.value)}
              rows={4}
            />
          </div>

          <div className="survey-question">
            <label>
              Do you want an email introduction to your partner?
              <i className="fas fa-info-circle" style={{ marginLeft: '8px', color: '#6b7280' }}></i>
            </label>
            <div className="survey-buttons">
              <button
                type="button"
                className={`survey-btn ${formData.wantEmailIntroduction === 'yes' ? 'selected' : ''}`}
                onClick={() => handleChange('wantEmailIntroduction', 'yes')}
              >
                Yes
              </button>
              <button
                type="button"
                className={`survey-btn ${formData.wantEmailIntroduction === 'no' ? 'selected' : ''}`}
                onClick={() => handleChange('wantEmailIntroduction', 'no')}
              >
                No
              </button>
            </div>
          </div>

          <div className="survey-actions">
            <button type="submit" className="survey-submit-btn" disabled={submitting}>
              {submitting ? 'Submitting...' : 'Submit'}
            </button>
          </div>
        </form>

        <div className="survey-footer">
          <p>Got more to say? Let us know at <a href="mailto:practice@tryexponent.com">practice@tryexponent.com</a></p>
        </div>
      </div>
    </div>
  );
};

