import type { QuestionList } from '../../services/question.service';
import './QuestionSidebar.css';

interface QuestionSidebarProps {
  isOpen: boolean;
  onToggle: () => void;
  selectedQuestion: QuestionList | null;
  onSelectQuestion: (question: QuestionList) => void;
  onOpenQuestionModal: () => void;
  // Optional timer and controls
  showTimer?: boolean;
  timerDisplay?: string;
  onFinish?: () => void;
  isFinishing?: boolean;
}

// Default system design questions
const DEFAULT_SYSTEM_DESIGN_QUESTIONS: QuestionList[] = [
  {
    id: 'default-1',
    title: 'Implement Youtube recommendation system',
    difficulty: 'Hard',
    questionType: 'System Design',
    category: 'System Design',
    isActive: true,
    approvalStatus: 'Approved',
    tags: ['Recommendation Systems', 'Video Streaming', 'Machine Learning'],
  },
  {
    id: 'default-2',
    title: 'Implement Uber',
    difficulty: 'Hard',
    questionType: 'System Design',
    category: 'System Design',
    isActive: true,
    approvalStatus: 'Approved',
    tags: ['Location Services', 'Real-time', 'Matching'],
  },
  {
    id: 'default-3',
    title: 'Implement Instagram',
    difficulty: 'Hard',
    questionType: 'System Design',
    category: 'System Design',
    isActive: true,
    approvalStatus: 'Approved',
    tags: ['Social Media', 'Image Sharing', 'Feed Generation'],
  },
  {
    id: 'default-4',
    title: 'Design a URL shortener (like bit.ly)',
    difficulty: 'Medium',
    questionType: 'System Design',
    category: 'System Design',
    isActive: true,
    approvalStatus: 'Approved',
    tags: ['URL Shortening', 'Hash Functions', 'Caching'],
  },
  {
    id: 'default-5',
    title: 'Design a chat system (like WhatsApp)',
    difficulty: 'Hard',
    questionType: 'System Design',
    category: 'System Design',
    isActive: true,
    approvalStatus: 'Approved',
    tags: ['Real-time Messaging', 'WebSockets', 'Scalability'],
  },
  {
    id: 'default-6',
    title: 'Design a distributed file storage system (like Dropbox)',
    difficulty: 'Hard',
    questionType: 'System Design',
    category: 'System Design',
    isActive: true,
    approvalStatus: 'Approved',
    tags: ['File Storage', 'Synchronization', 'Distributed Systems'],
  },
];

export const QuestionSidebar = ({
  isOpen,
  onToggle,
  selectedQuestion,
  onSelectQuestion,
  onOpenQuestionModal,
  showTimer = false,
  timerDisplay = '00:00',
  onFinish,
  isFinishing = false,
}: QuestionSidebarProps) => {
  // Combine default questions with selected question (remove duplicates)
  const allQuestions = selectedQuestion
    ? [...DEFAULT_SYSTEM_DESIGN_QUESTIONS, selectedQuestion]
    : DEFAULT_SYSTEM_DESIGN_QUESTIONS;
  
  // Remove duplicates based on ID
  const questions = allQuestions.filter(
    (q, index, self) => index === self.findIndex((t) => t.id === q.id)
  );

  return (
    <div className={`question-sidebar ${isOpen ? 'open' : 'closed'}`}>
      <button className="sidebar-toggle-button" onClick={onToggle} aria-label="Toggle sidebar">
        <i className={`fas fa-chevron-${isOpen ? 'left' : 'right'}`}></i>
      </button>
      
      {isOpen && (
        <div className="sidebar-content">
          {/* Show timer and finish button instead of header if showTimer is true */}
          {showTimer ? (
            <div className="sidebar-session-controls">
              <div className="sidebar-timer">
                <i className="fas fa-clock"></i>
                <span>{timerDisplay}</span>
              </div>
              {onFinish && (
                <button 
                  className="sidebar-finish-btn"
                  onClick={onFinish}
                  disabled={isFinishing}
                  title="Finish Interview"
                >
                  <i className={`fas ${isFinishing ? 'fa-spinner fa-spin' : 'fa-stop'}`}></i>
                  {isFinishing ? 'Ending...' : 'Finish'}
                </button>
              )}
            </div>
          ) : (
            <div className="sidebar-header">
              <h3>System Design Questions</h3>
              <button 
                className="add-question-button" 
                onClick={onOpenQuestionModal}
                title="Select a question"
              >
                <i className="fas fa-plus"></i>
              </button>
            </div>
          )}
          
          <div className="questions-list">
            {questions.length === 0 ? (
              <div className="empty-state">
                <i className="fas fa-clipboard-list"></i>
                <p>No questions selected</p>
                <button className="select-question-button" onClick={onOpenQuestionModal}>
                  Select Question
                </button>
              </div>
            ) : (
              questions.map((question) => (
                <div
                  key={question.id}
                  className={`question-item ${selectedQuestion?.id === question.id ? 'active' : ''}`}
                  onClick={() => onSelectQuestion(question)}
                >
                  <div className="question-item-header">
                    <span className="question-title">{question.title}</span>
                    {selectedQuestion?.id === question.id && (
                      <i className="fas fa-check-circle"></i>
                    )}
                  </div>
                  {question.category && (
                    <div className="question-category">
                      <i className="fas fa-tag"></i>
                      <span>{question.category}</span>
                    </div>
                  )}
                  <div className="question-meta">
                    <span className={`difficulty-badge difficulty-${question.difficulty.toLowerCase()}`}>
                      {question.difficulty}
                    </span>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
};
