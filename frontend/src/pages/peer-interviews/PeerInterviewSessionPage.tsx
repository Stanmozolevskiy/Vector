import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { peerInterviewService } from '../../services/peerInterview.service';
import type { PeerInterviewSession } from '../../services/peerInterview.service';
import { CollaborativeCodeEditor } from '../../components/CollaborativeCodeEditor';
import { VideoChat } from '../../components/VideoChat';
import '../../styles/peer-interview-session.css';

const PeerInterviewSessionPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [session, setSession] = useState<PeerInterviewSession | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [code, setCode] = useState('');
  const [selectedLanguage, setSelectedLanguage] = useState('javascript');
  const [timeRemaining, setTimeRemaining] = useState<number | null>(null);
  const [isTimerRunning, setIsTimerRunning] = useState(false);
  const [videoError, setVideoError] = useState<string | null>(null);
  const [collaborationError, setCollaborationError] = useState<string | null>(null);

  useEffect(() => {
    if (id) {
      loadSession();
    }
  }, [id]);

  useEffect(() => {
    let interval: ReturnType<typeof setInterval> | null = null;
    if (isTimerRunning && timeRemaining !== null && timeRemaining > 0) {
      interval = setInterval(() => {
        setTimeRemaining(prev => {
          if (prev === null || prev <= 1) {
            setIsTimerRunning(false);
            return 0;
          }
          return prev - 1;
        });
      }, 1000);
    }
    return () => {
      if (interval) clearInterval(interval);
    };
  }, [isTimerRunning, timeRemaining]);

  const loadSession = async () => {
    if (!id) return;
    try {
      setLoading(true);
      const sessionData = await peerInterviewService.getSession(id);
      setSession(sessionData);
      
      // Initialize timer if session is in progress
      if (sessionData.status === 'InProgress' && sessionData.duration) {
        const startTime = sessionData.scheduledTime 
          ? new Date(sessionData.scheduledTime).getTime()
          : Date.now();
        const elapsed = Math.floor((Date.now() - startTime) / 1000);
        const remaining = (sessionData.duration * 60) - elapsed;
        setTimeRemaining(Math.max(0, remaining));
      }
    } catch (error: any) {
      setError(error?.response?.data?.message || 'Failed to load session');
    } finally {
      setLoading(false);
    }
  };

  const handleStartInterview = async () => {
    if (!id) return;
    try {
      await peerInterviewService.updateSessionStatus(id, 'InProgress');
      if (session) {
        const duration = session.duration * 60; // Convert to seconds
        setTimeRemaining(duration);
        setIsTimerRunning(true);
        setSession({ ...session, status: 'InProgress' });
      }
    } catch (error: any) {
      setError(error?.response?.data?.message || 'Failed to start interview');
    }
  };

  const handleEndInterview = async () => {
    if (!id) return;
    try {
      await peerInterviewService.updateSessionStatus(id, 'Completed');
      if (session) {
        setSession({ ...session, status: 'Completed' });
        setIsTimerRunning(false);
      }
    } catch (error: any) {
      setError(error?.response?.data?.message || 'Failed to end interview');
    }
  };

  const formatTime = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  const getRole = (): 'interviewer' | 'interviewee' | null => {
    if (!session || !user) return null;
    if (session.interviewerId === user.id) return 'interviewer';
    if (session.intervieweeId === user.id) return 'interviewee';
    return null;
  };

  const getPartner = () => {
    if (!session || !user) return null;
    return session.interviewerId === user.id ? session.interviewee : session.interviewer;
  };

  if (loading) {
    return (
      <div className="session-page">
        <div className="loading">Loading session...</div>
      </div>
    );
  }

  if (error && !session) {
    return (
      <div className="session-page">
        <div className="error-message">{error}</div>
        <button onClick={() => navigate('/peer-interviews/find')} className="btn-back">
          Back to Find Peer
        </button>
      </div>
    );
  }

  if (!session) {
    return (
      <div className="session-page">
        <div className="error-message">Session not found</div>
      </div>
    );
  }

  const role = getRole();
  const partner = getPartner();
  const isInterviewer = role === 'interviewer';

  return (
    <div className="session-page">
      <div className="session-header">
        <div className="session-info">
          <h1>Peer Interview Session</h1>
          <div className="session-meta">
            <span className={`status-badge status-${session.status.toLowerCase()}`}>
              {session.status}
            </span>
            <span className="role-badge">
              You are: <strong>{isInterviewer ? 'Interviewer' : 'Interviewee'}</strong>
            </span>
            {partner && (
              <span className="partner-info">
                Partner: {session.interviewerId === user?.id ? 'Interviewee' : 'Interviewer'}
              </span>
            )}
          </div>
        </div>
        {timeRemaining !== null && (
          <div className={`timer ${timeRemaining < 300 ? 'timer-warning' : ''} ${timeRemaining < 60 ? 'timer-critical' : ''}`}>
            {formatTime(timeRemaining)}
          </div>
        )}
      </div>

      <div className="session-content">
        <div className="session-main-layout">
          {/* Left Column: Question Panel */}
          <div className="question-panel">
            {session.question ? (
              <div className="question-display">
                <h2>{session.question.title}</h2>
                <span className={`difficulty-badge difficulty-${session.question.difficulty.toLowerCase()}`}>
                  {session.question.difficulty}
                </span>
                <button
                  onClick={() => navigate(`/questions/${session.questionId}`)}
                  className="btn-view-full"
                >
                  View Full Question
                </button>
              </div>
            ) : (
              <div className="question-display">
                <p>No question selected yet.</p>
                {isInterviewer && (
                  <button
                    onClick={() => navigate('/questions')}
                    className="btn-select-question"
                  >
                    Select Question
                  </button>
                )}
              </div>
            )}
          </div>

          {/* Right Column: Video Chat and Editor */}
          <div className="collaboration-panel">
            {/* Video Chat Panel - Only show when session is InProgress */}
            {session.status === 'InProgress' && (
              <div className="video-panel">
                <h3 className="panel-title">Video Chat</h3>
                {videoError && (
                  <div className="error-message-small">{videoError}</div>
                )}
                <VideoChat
                  sessionId={id || ''}
                  userId={user?.id || ''}
                  peerUserId={partner?.id}
                  onError={(error) => setVideoError(error)}
                />
              </div>
            )}

            {/* Collaborative Code Editor */}
            <div className="editor-panel">
              <div className="editor-header">
                <select
                  value={selectedLanguage}
                  onChange={(e) => setSelectedLanguage(e.target.value)}
                  className="language-select"
                >
                  <option value="javascript">JavaScript</option>
                  <option value="python">Python3</option>
                  <option value="java">Java</option>
                  <option value="cpp">C++</option>
                  <option value="csharp">C#</option>
                  <option value="go">Go</option>
                </select>
                {collaborationError && (
                  <span className="collaboration-status error">
                    <i className="fas fa-exclamation-triangle"></i> Collaboration offline
                  </span>
                )}
                {!collaborationError && session.status === 'InProgress' && (
                  <span className="collaboration-status active">
                    <i className="fas fa-circle"></i> Live collaboration
                  </span>
                )}
              </div>
              {session.status === 'InProgress' ? (
                <CollaborativeCodeEditor
                  value={code}
                  language={selectedLanguage}
                  onChange={(value) => setCode(value || '')}
                  sessionId={id || ''}
                  userId={user?.id || ''}
                  peerUserId={partner?.id}
                  onError={(error) => setCollaborationError(error)}
                />
              ) : (
                <div className="editor-placeholder">
                  <p>Start the interview to begin collaborative coding</p>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      <div className="session-actions">
        {session.status === 'Scheduled' && (
          <button onClick={handleStartInterview} className="btn-start">
            Start Interview
          </button>
        )}
        {session.status === 'InProgress' && (
          <button onClick={handleEndInterview} className="btn-end">
            End Interview
          </button>
        )}
        <button onClick={() => navigate('/peer-interviews/find')} className="btn-back">
          Back
        </button>
      </div>
    </div>
  );
};

export default PeerInterviewSessionPage;

