import { useEffect, useState, useRef, useCallback, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { Navbar } from '../../components/layout/Navbar';
import { Whiteboard } from '../../components/whiteboard/Whiteboard';
import { QuestionSidebar } from '../../components/whiteboard/QuestionSidebar';
import { SystemDesignQuestionModal } from '../../components/whiteboard/SystemDesignQuestionModal';
import { DraggableVideoChat } from '../../components/DraggableVideoChat';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';
import { peerInterviewService } from '../../services/peerInterview.service';
import type { PeerInterviewSession } from '../../services/peerInterview.service';
import { questionService } from '../../services/question.service';
import type { QuestionList } from '../../services/question.service';
import { whiteboardService } from '../../services/whiteboard.service';
import api from '../../services/api';
import { tokenStorage } from '../../utils/tokenStorage';
import { RejoinModal } from '../../components/RejoinModal';
import { FeedbackForm } from '../../components/FeedbackForm';
import '../../components/whiteboard/Whiteboard.css';
import './SystemDesignInterviewPage.css';

interface ExcalidrawInitialDataState {
  elements?: readonly any[];
  appState?: any;
  files?: Record<string, any>;
}

export const SystemDesignInterviewPage = () => {
  const { sessionId } = useParams<{ sessionId: string }>();
  const navigate = useNavigate();
  const { user, isAuthenticated, isLoading } = useAuth();
  const [activeSession, setActiveSession] = useState<PeerInterviewSession | null>(null);
  const [selectedQuestion, setSelectedQuestion] = useState<QuestionList | null>(null);
  const [whiteboardData, setWhiteboardData] = useState<ExcalidrawInitialDataState>({
    elements: [],
    appState: {
      viewBackgroundColor: '#fafafa',
      gridSize: 0,
      zoom: { value: 1 },
      scrollX: 0,
      scrollY: 0,
    },
    files: {},
  });
  const [showQuestionModal, setShowQuestionModal] = useState(false);
  const [isSidebarOpen, setIsSidebarOpen] = useState(true);
  const [isEndingSession, setIsEndingSession] = useState(false);
  const [showRejoinModal, setShowRejoinModal] = useState(false);
  const [showFeedbackForm, setShowFeedbackForm] = useState(false);
  const [showInstructions, setShowInstructions] = useState(true);
  const [sessionStartTime, setSessionStartTime] = useState<Date | null>(null);
  const [elapsedTime, setElapsedTime] = useState<number>(0);
  const [showPartnerVideo, setShowPartnerVideo] = useState(false);
  const connectionLostRef = useRef<boolean>(false);
  const sessionHubConnectionRef = useRef<signalR.HubConnection | null>(null);
  const whiteboardSaveTimeoutRef = useRef<number | null>(null);
  const broadcastThrottleRef = useRef<boolean>(false);
  const pendingRemoteElementsRef = useRef<any[] | null>(null);
  const applyRemoteRafRef = useRef<number | null>(null);

  // Force Excalidraw to resize when sidebar toggles to avoid blank/gap artifacts
  useEffect(() => {
    const fireResize = () => window.dispatchEvent(new Event('resize'));

    const t0 = window.setTimeout(() => {
      requestAnimationFrame(() => requestAnimationFrame(fireResize));
    }, 0);
    const t1 = window.setTimeout(fireResize, 350);

    return () => {
      window.clearTimeout(t0);
      window.clearTimeout(t1);
    };
  }, [isSidebarOpen]);

  // Format elapsed time
  const formatTime = useCallback((seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  }, []);

  const checkActiveSession = useCallback(async () => {
    if (!sessionId || !user?.id) return;

    try {
      const session = await peerInterviewService.getSession(sessionId);
      handleSessionData(session);
    } catch (error: any) {
      const status = error?.response?.status;
      if (status === 401 || status === 403) {
        try {
          await peerInterviewService.joinFriendInterview(sessionId);
          const retrySession = await peerInterviewService.getSession(sessionId);
          handleSessionData(retrySession);
          return; // Successfully joined, stop error flow
        } catch {
          // silently fail and let the normal error handler take over
        }
      }
      console.error('Failed to load session:', error);
      connectionLostRef.current = true;
      setShowRejoinModal(true);
    }
  }, [sessionId, user?.id]);

  const handleSessionData = async (session: any) => {
    setActiveSession(session);

    // Load question if available (optional for system design)
    if (session.questionId) {
      try {
        const question = await questionService.getQuestionById(session.questionId);
        if (question) {
          // Convert InterviewQuestion to QuestionList format
          const questionList: QuestionList = {
            id: question.id,
            title: question.title,
            difficulty: question.difficulty,
            questionType: question.questionType,
            category: question.category,
            tags: question.tags,
            companyTags: question.companyTags,
            isActive: question.isActive,
            approvalStatus: question.approvalStatus,
          };
          setSelectedQuestion(questionList);
        }
      } catch (error) {
        console.error('Failed to load question:', error);
        // For system design, it's ok if there's no question assigned
      }
    }
    // For system design interviews, questions are optional - no error if missing

    // Load whiteboard data for this session
    await loadWhiteboardData(sessionId!);

    // Set session start time
    if (session.createdAt) {
      const startTime = new Date(session.createdAt);
      setSessionStartTime(startTime);
      const savedStartTime = localStorage.getItem(`session_start_${sessionId}`);
      if (savedStartTime) {
        setSessionStartTime(new Date(savedStartTime));
      } else {
        localStorage.setItem(`session_start_${sessionId}`, startTime.toISOString());
        setSessionStartTime(startTime);
      }
    }

    // Show video if both users are in session
    // For friend interviews, show video immediately even with one participant
    const isFriendInterview = session.practiceType === 'friend';
    if (session.status === 'InProgress' && (isFriendInterview || (session.interviewerId && session.intervieweeId))) {
      setShowPartnerVideo(true);
    }
  };

  // Load shared session whiteboard (ONE board for both users)
  const loadWhiteboardData = useCallback(async (liveSessionId: string) => {
    if (!user?.id) return;

    try {
      const data = await whiteboardService.getSessionWhiteboard(liveSessionId);

      if (data) {
        const elements = data.elements ? JSON.parse(data.elements) : [];
        const appState = data.appState ? JSON.parse(data.appState) : {};
        const files = data.files ? JSON.parse(data.files) : {};

        setWhiteboardData({
          elements: Array.isArray(elements) ? elements : [],
          appState: {
            viewBackgroundColor: appState.viewBackgroundColor || '#fafafa',
            gridSize: appState.gridSize || 0,
            zoom: appState.zoom || { value: 1 },
            scrollX: appState.scrollX || 0,
            scrollY: appState.scrollY || 0,
          },
          files: files && typeof files === 'object' ? files : {},
        });
      } else {
        setWhiteboardData({
          elements: [],
          appState: {
            viewBackgroundColor: '#fafafa',
            gridSize: 0,
            zoom: { value: 1 },
            scrollX: 0,
            scrollY: 0,
          },
          files: {},
        });
      }
    } catch (error) {
      console.error('Failed to load session whiteboard:', error);
    }
  }, [user?.id]);

  // Save shared session whiteboard (ONE board for both users)
  const handleSaveWhiteboard = useCallback(
    async (data: ExcalidrawInitialDataState) => {
      if (!user?.id || !sessionId || !activeSession) return;

      try {
        // Persist shared board by session (non-blocking)
        whiteboardService
          .saveSessionWhiteboard(sessionId, {
            elements: JSON.stringify(data.elements || []),
            appState: JSON.stringify(data.appState || {}),
            files: JSON.stringify(data.files || {}),
          })
          .catch((error) => console.error('Failed to save session whiteboard:', error));

        // Broadcast elements only (avoid overriding partner's active tool)
        if (sessionHubConnectionRef.current && sessionHubConnectionRef.current.state === 'Connected') {
          const sessionIdString = String(sessionId);
          const elementsToBroadcast = Array.isArray(data.elements) ? data.elements : [];

          if (!broadcastThrottleRef.current) {
            broadcastThrottleRef.current = true;
            const whiteboardUpdateData = { elements: elementsToBroadcast };

            sessionHubConnectionRef.current
              .invoke('BroadcastWhiteboardUpdate', sessionIdString, whiteboardUpdateData)
              .catch(() => {})
              .finally(() => {
                setTimeout(() => {
                  broadcastThrottleRef.current = false;
                }, 90);
              });
          }
        }
      } catch (error) {
        console.error('Failed to save whiteboard data:', error);
      }
    },
    [user?.id, sessionId, activeSession]
  );

  // Initialize SignalR for session events
  useEffect(() => {
    if (!activeSession || !user?.id) {
      return;
    }

    // Only initialize SignalR if both users joined OR it's a friend interview (single user can start)
    const isFriendInterview = activeSession.practiceType === 'friend';
    if (!(isFriendInterview || (activeSession.interviewerId && activeSession.intervieweeId))) {
      return;
    }

    const initializeSessionHub = async () => {
      try {
        const baseUrl = (api.defaults.baseURL?.replace('/api', '') || 'http://localhost:5000');
        const connection = new signalR.HubConnectionBuilder()
          .withUrl(`${baseUrl}/api/collaboration`, {
            transport: signalR.HttpTransportType.WebSockets,
            accessTokenFactory: () => tokenStorage.getAccessToken() || '',
          })
          .withAutomaticReconnect()
          .build();

        // Listen for question changes
        connection.on('QuestionChanged', async (data: { sessionId: string; questionId: string }) => {
          if (data.sessionId === activeSession.id) {
            try {
              const question = await questionService.getQuestionById(data.questionId);
              if (question) {
                // Convert InterviewQuestion to QuestionList format
                const questionList: QuestionList = {
                  id: question.id,
                  title: question.title,
                  difficulty: question.difficulty,
                  questionType: question.questionType,
                  category: question.category,
                  tags: question.tags,
                  companyTags: question.companyTags,
                  isActive: question.isActive,
                  approvalStatus: question.approvalStatus,
                };
                setSelectedQuestion(questionList);
              }
            } catch (error) {
              console.error('Failed to load changed question:', error);
            }
          }
        });

        // Listen for participant joined (to update session with both users)
        connection.on('ParticipantJoined', async (data: { sessionId: string }) => {
          if (data.sessionId === activeSession.id) {
            try {
              // Reload session to get updated participants
              const updatedSession = await peerInterviewService.getSession(data.sessionId);
              setActiveSession(updatedSession);
              
              // Synchronize timer with backend
              if (updatedSession.startedAt) {
                const backendStartTime = new Date(updatedSession.startedAt);
                setSessionStartTime(backendStartTime);
                localStorage.setItem(`session_start_${data.sessionId}`, backendStartTime.toISOString());
              }
              
              setShowPartnerVideo(true);
            } catch (error) {
              console.error('Error handling participant joined:', error);
            }
          }
        });

        // No role switching for system design (single shared whiteboard)

        // Listen for interview ended
        connection.on('InterviewEnded', async (data: { sessionId: string }) => {
          if (data.sessionId === activeSession.id) {
            if (activeSession.id) {
              localStorage.removeItem(`session_start_${activeSession.id}`);
            }
            setSessionStartTime(null);
            setElapsedTime(0);
            setShowFeedbackForm(true);
            setShowPartnerVideo(false);
          }
        });

        // Listen for timer synchronization
        connection.on('TimerSynced', (serverElapsedTime: number) => {
          if (serverElapsedTime > 0) {
            setSessionStartTime(new Date(Date.now() - serverElapsedTime * 1000));
          }
        });

        await connection.start();
        // Ensure sessionId is a string for SignalR
        const sessionIdString = String(activeSession.id);
        
        // Wait a bit for connection to be fully ready
        await new Promise(resolve => setTimeout(resolve, 100));
        
        await connection.invoke('JoinSession', sessionIdString);
        sessionHubConnectionRef.current = connection;
        
        // Handle connection state changes
        connection.onclose(() => {
          // Connection closed - will be handled by reconnection logic
        });
        connection.onreconnecting(() => {
          // Reconnecting...
        });
        connection.onreconnected(() => {
          connection.invoke('JoinSession', sessionIdString).catch(() => {
            // Silent fail on rejoin
          });
        });
        
        // Listen for whiteboard updates (elements only; do not sync appState/tools)
        connection.on('WhiteboardUpdate', (data: { sessionId: string; elements: any[] }) => {
          const sessionIdString = String(activeSession.id);
          const receivedSessionId = String(data.sessionId || '');
          
          // Compare session IDs (handle both string and GUID formats)
          if (receivedSessionId === sessionIdString || receivedSessionId.toLowerCase() === sessionIdString.toLowerCase()) {
            const newElements = Array.isArray(data.elements) ? data.elements : [];

            // Coalesce remote updates to 1 per animation frame to avoid scene thrash/blinking
            pendingRemoteElementsRef.current = newElements;
            if (applyRemoteRafRef.current == null) {
              applyRemoteRafRef.current = window.requestAnimationFrame(() => {
                applyRemoteRafRef.current = null;
                const pending = pendingRemoteElementsRef.current;
                pendingRemoteElementsRef.current = null;
                if (!pending) return;

                setWhiteboardData((prev) => {
                  const prevElementsStr = JSON.stringify(prev.elements || []);
                  const newElementsStr = JSON.stringify(pending);

                  if (prevElementsStr !== newElementsStr) {
                    return {
                      elements: pending,
                      appState: prev.appState || {},
                      files: prev.files || {},
                    };
                  }

                  return prev;
                });
              });
            }
          }
        });
      } catch (error) {
        console.error('Failed to initialize session hub:', error);
      }
    };

    initializeSessionHub();

    return () => {
      if (sessionHubConnectionRef.current) {
        sessionHubConnectionRef.current.stop().catch(() => {});
        sessionHubConnectionRef.current = null;
      }
      if (applyRemoteRafRef.current != null) {
        window.cancelAnimationFrame(applyRemoteRafRef.current);
        applyRemoteRafRef.current = null;
      }
      pendingRemoteElementsRef.current = null;
    };
  }, [activeSession?.id, activeSession?.interviewerId, activeSession?.intervieweeId, user?.id]);

  // Timer effect
  useEffect(() => {
    if (!sessionStartTime) return;

    const interval = setInterval(() => {
      const elapsed = Math.floor((Date.now() - sessionStartTime.getTime()) / 1000);
      setElapsedTime(elapsed);

      // Periodically broadcast timer if I am the interviewer (or the first user in session)
      if (activeSession?.id && sessionHubConnectionRef.current?.state === 'Connected') {
        const isInterviewer = activeSession.interviewerId === user?.id;
        const isOnlyUserSoFar = !activeSession.interviewerId || !activeSession.intervieweeId;
        // Broadcast every 5 seconds
        if ((isInterviewer || isOnlyUserSoFar) && elapsed > 0 && elapsed % 5 === 0) {
           sessionHubConnectionRef.current.invoke('SendTimerSync', activeSession.id, elapsed).catch(() => {});
        }
      }
    }, 1000);

    return () => clearInterval(interval);
  }, [sessionStartTime, activeSession?.id, activeSession?.interviewerId, activeSession?.intervieweeId, user?.id]);

  // Polling backup: Check for session updates every 3 seconds if waiting for second user
  useEffect(() => {
    if (!activeSession || !user?.id) return;
    
    // Only poll if we're in a friend interview and waiting for the second user
    const isFriendInterview = activeSession.practiceType === 'friend';
    const hasOnlyOneUser = activeSession.interviewerId && !activeSession.intervieweeId;
    
    if (!isFriendInterview || !hasOnlyOneUser) return;
    
    const intervalId = setInterval(async () => {
      try {
        const updatedSession = await peerInterviewService.getSession(activeSession.id);
        
        // Check if second user has joined
        if (updatedSession.interviewerId && updatedSession.intervieweeId) {
          setActiveSession(updatedSession);
          
          // Synchronize timer - use updatedSession.id for localStorage key
          if (updatedSession.startedAt) {
            const backendStartTime = new Date(updatedSession.startedAt);
            setSessionStartTime(backendStartTime);
            localStorage.setItem(`session_start_${updatedSession.id}`, backendStartTime.toISOString());
          }
          
          setShowPartnerVideo(true);
          // Stop polling once second user is found
          clearInterval(intervalId);
        }
      } catch (error) {
        // Polling error - will retry on next interval
      }
    }, 3000); // Poll every 3 seconds
    
    return () => clearInterval(intervalId);
  }, [activeSession, user?.id]);

  // Load session on mount
  useEffect(() => {
    if (sessionId && user?.id) {
      checkActiveSession();
    }
  }, [sessionId, user?.id, checkActiveSession]);

  // Handle question selection
  const handleSelectQuestion = useCallback(async (question: QuestionList) => {
    if (!activeSession || !user?.id) return;
    // System design: both users can take turns selecting questions

    try {
      // If question has a real ID (not default), change question in session
      if (question.id && !question.id.startsWith('default-')) {
        await peerInterviewService.changeQuestion(activeSession.id, question.id);
        
        // Reload session
        const updatedSession = await peerInterviewService.getSession(activeSession.id);
        setActiveSession(updatedSession);

        // Notify partner via SignalR
        if (sessionHubConnectionRef.current) {
          try {
            await sessionHubConnectionRef.current.invoke('SendQuestionChanged', activeSession.id, question.id);
          } catch (error) {
            console.error('Failed to notify partner of question change:', error);
          }
        }
      }
      
      // Set selected question (works for both default and real questions)
      setSelectedQuestion(question);
    } catch (error) {
      console.error('Failed to change question:', error);
    }
  }, [activeSession, user?.id]);
  // No role switching for system design interviews (single shared board)

  // Handle end session
  const handleEndSession = useCallback(async () => {
    if (!activeSession || !user?.id || isEndingSession) return;
    
    if (!window.confirm('Are you sure you want to end this interview session?')) {
      return;
    }

    try {
      setIsEndingSession(true);
      
      // First, reload session to get latest participant data
      const latestSession = await peerInterviewService.getSession(activeSession.id);
      setActiveSession(latestSession);
      
      await peerInterviewService.endInterview(activeSession.id);
      
      if (activeSession.id) {
        localStorage.removeItem(`session_start_${activeSession.id}`);
      }
      setSessionStartTime(null);
      setElapsedTime(0);
      setShowFeedbackForm(true);
      setShowPartnerVideo(false);
    } catch (error) {
      console.error('Failed to end session:', error);
    } finally {
      setIsEndingSession(false);
    }
  }, [activeSession, user?.id, isEndingSession]);

  // Calculate partner user
  const partner = useMemo(() => {
    if (!activeSession || !user?.id) return null;
    return activeSession.interviewerId === user.id
      ? activeSession.interviewee
      : activeSession.interviewer;
  }, [activeSession, user?.id]);

  // Determine if both users joined the session (for feedback logic)
  const hasBothUsers = useMemo(() => {
    return !!(activeSession?.interviewerId && activeSession?.intervieweeId);
  }, [activeSession?.interviewerId, activeSession?.intervieweeId]);

  // Handle feedback complete
  const handleFeedbackComplete = useCallback(() => {
    setShowFeedbackForm(false);
    window.location.assign(ROUTES.FIND_PEER);
  }, []);

  // Handle rejoin
  const handleRejoin = useCallback(() => {
    setShowRejoinModal(false);
    connectionLostRef.current = false;
    if (activeSession) {
      window.location.reload();
    }
  }, [activeSession]);

  // Cleanup
  useEffect(() => {
    return () => {
      if (whiteboardSaveTimeoutRef.current !== null) {
        window.clearTimeout(whiteboardSaveTimeoutRef.current);
      }
      if (sessionHubConnectionRef.current) {
        sessionHubConnectionRef.current.stop().catch(() => {});
      }
    };
  }, []);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      navigate(ROUTES.LOGIN);
    }
  }, [isAuthenticated, isLoading, navigate]);

  if (isLoading || !user) {
    return (
      <div className="system-design-interview-page-loading">
        <div>Loading interview session...</div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  if (!activeSession || !sessionId) {
    return (
      <div className="system-design-interview-page-loading">
        <div>Session not found</div>
      </div>
    );
  }

  const peerUserId = activeSession.interviewerId === user.id
    ? activeSession.intervieweeId
    : activeSession.interviewerId;

  return (
    <div className="system-design-interview-page">
      <Navbar />
      
      {/* Instructions Popup */}
      {showInstructions && (
        <div className="system-design-instructions-overlay">
          <div className="system-design-instructions-modal">
            <button 
              className="instructions-close-btn"
              onClick={() => setShowInstructions(false)}
              aria-label="Close instructions"
            >
              <i className="fas fa-times"></i>
            </button>
            <h2>System Design Interview</h2>
            <div className="instructions-content">
              <p><strong>Take turns interviewing each other</strong></p>
              <ul>
                <li>Select a system design question from the sidebar</li>
                <li>Sketch your design on the shared whiteboard</li>
                <li>Discuss trade-offs and architectural decisions</li>
                <li>Ask clarifying questions about requirements</li>
              </ul>
              <p className="instructions-note">
                <i className="fas fa-info-circle"></i> Both participants can draw on the whiteboard simultaneously
              </p>
            </div>
            <button 
              className="instructions-got-it-btn"
              onClick={() => setShowInstructions(false)}
            >
              Got it!
            </button>
          </div>
        </div>
      )}
      
      {/* Session Header */}
      <div className="system-design-interview-header">
        <div className="session-info">
          <div className="session-instructions">
            Take turns selecting a question and sketching your system design on the shared board.
          </div>
          {selectedQuestion && (
            <div className="question-info">
              <span className="question-label">Question:</span>
              <span className="question-title">{selectedQuestion.title}</span>
            </div>
          )}
          {!selectedQuestion && (
            <button
              onClick={() => setShowQuestionModal(true)}
              className="select-question-btn"
              title="Select a question"
            >
              <i className="fas fa-clipboard-list"></i>
              Select Question
            </button>
          )}
          <div className="timer">
            <i className="fas fa-clock"></i>
            <span>{formatTime(elapsedTime)}</span>
          </div>
        </div>
        <div className="session-controls">
          {activeSession?.practiceType === 'friend' && (!activeSession.interviewerId || !activeSession.intervieweeId) && (
            <button
              className="control-button"
              title="Copy Invite Link"
              onClick={() => {
                const url = `${window.location.origin}/friend-invite/${activeSession.id}`;
                navigator.clipboard.writeText(url).then(() => {
                  // Assuming showToast is not imported, let's use a standard alert or a custom toast if available
                  // Let's add a simple alert since showToast is not in this file's imports
                  alert('Invite link copied to clipboard!');
                });
              }}
              style={{ backgroundColor: '#ecfdf5', color: '#059669', border: '1px solid #10b981', marginRight: '10px' }}
            >
              <i className="fas fa-link"></i> Copy Link
            </button>
          )}
          <button
            onClick={handleEndSession}
            disabled={isEndingSession}
            className="control-button end-session-button"
            title="End Session"
          >
            <i className="fas fa-stop"></i>
            {isEndingSession ? 'Ending...' : 'End Session'}
          </button>
        </div>
      </div>

      {/* Main Content - Similar to WhiteboardPage */}
      <div className="system-design-interview-content">
        {/* Question Sidebar with Timer and Finish Button */}
        <QuestionSidebar
          isOpen={isSidebarOpen}
          onToggle={() => setIsSidebarOpen(!isSidebarOpen)}
          selectedQuestion={selectedQuestion}
          onSelectQuestion={handleSelectQuestion}
          onOpenQuestionModal={() => setShowQuestionModal(true)}
          showTimer={true}
          timerDisplay={formatTime(elapsedTime)}
          onFinish={handleEndSession}
          isFinishing={isEndingSession}
        />

        {/* Whiteboard Area */}
        <div className={`whiteboard-main-area ${isSidebarOpen ? 'with-sidebar' : ''}`}>
          <Whiteboard
            initialData={whiteboardData}
            onSave={handleSaveWhiteboard}
          />
        </div>
      </div>

      {/* Video Chat */}
      {/* Partner Video - Show for friend interviews or when both users joined */}
      {showPartnerVideo && activeSession.status === 'InProgress' && (
        activeSession.practiceType === 'friend' || (activeSession.interviewerId && activeSession.intervieweeId)
      ) && (
        <DraggableVideoChat
          sessionId={activeSession.id}
          userId={user.id}
          peerUserId={peerUserId}
          onError={(error) => {
            console.error('Video chat error:', error);
          }}
        />
      )}

      {/* Question Selection Modal */}
      <SystemDesignQuestionModal
        isOpen={showQuestionModal}
        onClose={() => setShowQuestionModal(false)}
        onSelectQuestion={handleSelectQuestion}
      />

      {/* Rejoin Modal */}
      {showRejoinModal && (
        <RejoinModal
          onRejoin={handleRejoin}
          onFeedback={() => {
            setShowRejoinModal(false);
            if (activeSession) {
              setShowFeedbackForm(true);
            }
          }}
        />
      )}

      {/* Feedback Form or No Partner Message */}
      {showFeedbackForm && (
        <div className="feedback-overlay">
          <div className="feedback-modal-card">
            {hasBothUsers && partner?.id ? (
              <FeedbackForm
                liveSessionId={activeSession.id}
                opponentId={partner.id}
                opponentName={partner.firstName || partner.email}
                interviewType="System Design"
                date={activeSession.createdAt}
                onComplete={handleFeedbackComplete}
                onCancel={handleFeedbackComplete}
              />
            ) : (
              <div className="feedback-no-partner">
                <div className="feedback-no-partner-content">
                  <i className="fas fa-info-circle"></i>
                  <h3>Interview Ended</h3>
                  <p>Your practice partner didn't join this session, so there's no feedback to provide.</p>
                  <button onClick={handleFeedbackComplete} className="btn-primary">
                    Continue
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
