import { useEffect, useState, useRef, useCallback } from 'react';
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

  // Check for active session
  const checkActiveSession = useCallback(async () => {
    if (!sessionId || !user?.id) return;

    try {
      const session = await peerInterviewService.getSession(sessionId);
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
      await loadWhiteboardData(sessionId);

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
      if (session.interviewerId && session.intervieweeId && session.status === 'InProgress') {
        setShowPartnerVideo(true);
      }
    } catch (error) {
      console.error('Failed to load session:', error);
      connectionLostRef.current = true;
      setShowRejoinModal(true);
    }
  }, [sessionId, user?.id]);

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
    if (!activeSession || !activeSession.interviewerId || !activeSession.intervieweeId || !user?.id) {
      return;
    }

    const initializeSessionHub = async () => {
      try {
        const accessToken = localStorage.getItem('accessToken');
        if (!accessToken) return;

        const baseUrl = (api.defaults.baseURL?.replace('/api', '') || 'http://localhost:5000');
        const connection = new signalR.HubConnectionBuilder()
          .withUrl(`${baseUrl}/api/collaboration?access_token=${accessToken}`, {
            transport: signalR.HttpTransportType.WebSockets,
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
    }, 1000);

    return () => clearInterval(interval);
  }, [sessionStartTime]);

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

  // Handle feedback complete
  const handleFeedbackComplete = useCallback(() => {
    setShowFeedbackForm(false);
    navigate(ROUTES.FIND_PEER);
  }, [navigate]);

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
        {/* Question Sidebar */}
        <QuestionSidebar
          isOpen={isSidebarOpen}
          onToggle={() => setIsSidebarOpen(!isSidebarOpen)}
          selectedQuestion={selectedQuestion}
          onSelectQuestion={handleSelectQuestion}
          onOpenQuestionModal={() => setShowQuestionModal(true)}
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
      {showPartnerVideo && activeSession.interviewerId && activeSession.intervieweeId && activeSession.status === 'InProgress' && (
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

      {/* Feedback Form */}
      {showFeedbackForm && activeSession && user?.id && (
        <FeedbackForm
          liveSessionId={activeSession.liveSessionId || activeSession.id}
          opponentId={
            activeSession.intervieweeId === user.id 
              ? activeSession.interviewerId || '' 
              : activeSession.intervieweeId || ''
          }
          opponentName={
            activeSession.intervieweeId === user.id
              ? activeSession.interviewer 
                ? `${activeSession.interviewer.firstName} ${activeSession.interviewer.lastName}`
                : undefined
              : activeSession.interviewee
                ? `${activeSession.interviewee.firstName} ${activeSession.interviewee.lastName}`
                : undefined
          }
          interviewType={activeSession.interviewType || 'System Design'}
          date={activeSession.scheduledTime}
          onComplete={handleFeedbackComplete}
          onCancel={handleFeedbackComplete}
        />
      )}
    </div>
  );
};
