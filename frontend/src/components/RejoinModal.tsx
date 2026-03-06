import React from 'react';
import './RejoinModal.css';

interface RejoinModalProps {
  onRejoin: () => void;
  onFeedback: () => void;
  onFinishInterview?: () => void;
  onClose?: () => void;
}

export const RejoinModal: React.FC<RejoinModalProps> = ({ onRejoin, onFeedback, onFinishInterview, onClose }) => {
  const handleClose = () => {
    if (onClose) {
      onClose();
    }
  };

  return (
    <div className="rejoin-modal-overlay" onClick={handleClose}>
      <div className="rejoin-modal" onClick={(e) => e.stopPropagation()}>
        <button className="rejoin-modal-close" onClick={handleClose}>
          <i className="fas fa-times"></i>
        </button>
        
        <div className="rejoin-modal-icon">
          <div className="rejoin-avatar">
            <i className="fas fa-user"></i>
          </div>
        </div>

        <h2 className="rejoin-modal-title">You have an interview in progress!</h2>

        <button className="rejoin-btn" onClick={onRejoin}>
          Rejoin your mock interview
        </button>

        {onFinishInterview && (
          <button 
            className="rejoin-btn finish-btn" 
            onClick={onFinishInterview}
            style={{
              marginTop: '0.75rem',
              background: '#6b7280',
              color: 'white'
            }}
          >
            Finish interview
          </button>
        )}

        <div className="rejoin-feedback">
          <p>Had issues with your session or already finished?</p>
          <a href="#" onClick={(e) => { e.preventDefault(); onFeedback(); }}>
            Give us feedback to help us improve!
          </a>
        </div>
      </div>
    </div>
  );
};

