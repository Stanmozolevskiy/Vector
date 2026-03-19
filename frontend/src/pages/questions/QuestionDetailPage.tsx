import { useEffect, useState, useRef, useCallback, useMemo } from 'react';
import { useParams, Link, useSearchParams, useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { questionService } from '../../services/question.service';
import type { InterviewQuestion, QuestionComment, QuestionTestCase, QuestionSolution } from '../../services/question.service';
import { codeExecutionService, type RunResult } from '../../services/codeExecution.service';
import { solutionService } from '../../services/solution.service';
import { ROUTES } from '../../utils/constants';
import { CodeEditor } from '../../components/CodeEditor';
import { CollaborativeCodeEditor } from '../../components/CollaborativeCodeEditor';
import { DraggableVideoChat } from '../../components/DraggableVideoChat';
import { useAuth } from '../../hooks/useAuth';
import { getQuestionTemplate } from '../../utils/questionTemplates';
import { ToastContainer } from '../../components/Toast';
import { MarkdownEditor } from '../../components/common/MarkdownEditor';
import { MarkdownRenderer } from '../../components/common/MarkdownRenderer';
import { CompanyIcon } from '../../components/common/CompanyIcon';
import { codeDraftService } from '../../services/codeDraft.service';
import { peerInterviewService } from '../../services/peerInterview.service';
import type { PeerInterviewSession } from '../../services/peerInterview.service';
import api from '../../services/api';
import { tokenStorage } from '../../utils/tokenStorage';
import { RejoinModal } from '../../components/RejoinModal';
import { FeedbackForm } from '../../components/FeedbackForm';
import { QuestionVoting } from '../../components/QuestionVoting';
import { QuestionDiscussion } from '../../components/QuestionDiscussion';
import { commentsService } from '../../services/comments.service';
import BookmarkButton from '../../components/questions/BookmarkButton';
import '../../styles/question-detail.css';

export const QuestionDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [question, setQuestion] = useState<InterviewQuestion | null>(null);
  const [testCases, setTestCases] = useState<QuestionTestCase[]>([]);
  const [solutions, setSolutions] = useState<QuestionSolution[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('description');
  // Initialize language based on question type - SQL for SQL questions, javascript for others
  const [selectedLanguage, setSelectedLanguage] = useState<string>(() => {
    // Will be updated when question loads
    return 'javascript';
  });
  const [code, setCode] = useState('');
  const [isExecuting, setIsExecuting] = useState(false);
  const [activeTestTab, setActiveTestTab] = useState<'testcase' | 'result'>('testcase');
  const [testCaseText, setTestCaseText] = useState('');
  const [validationError, setValidationError] = useState<{ type: string; message: string; lineNumber?: number } | null>(null);
  const [runResult, setRunResult] = useState<RunResult | null>(null);
  const [errorMarkers, setErrorMarkers] = useState<Array<{ line: number; column?: number; endColumn?: number; message: string }>>([]);
  const [selectedCaseIndex, setSelectedCaseIndex] = useState<number>(1);
  const [parameterNames, setParameterNames] = useState<string[]>([]);
  const [parameterCount, setParameterCount] = useState<number>(1);
  const [selectedTestLine, setSelectedTestLine] = useState<number | null>(null);
  const [cursorLine, setCursorLine] = useState<number>(1);
  const [cursorColumn, setCursorColumn] = useState<number>(1);
  const [topicsExpanded, setTopicsExpanded] = useState<boolean>(false);
  const [companiesExpanded, setCompaniesExpanded] = useState<boolean>(false);
  const [expandedHints, setExpandedHints] = useState<Set<number>>(new Set());
  const [expandedSolutions, setExpandedSolutions] = useState<Set<string>>(new Set());
  const [isSolved, setIsSolved] = useState<boolean>(false);
  const [testResults, setTestResults] = useState<Array<{
    testCaseNumber: number;
    passed: boolean;
    stdout?: string;
    output?: string;
    expectedOutput?: string;
    input?: string;
    error?: string;
    runtime?: number;
    memory?: number;
    status: string;
  }>>([]);
  const [executionResult, setExecutionResult] = useState<{
    status: string;
    runtime?: number;
    memory?: number;
    output?: string;
    error?: string;
  } | null>(null);
  const [toasts, setToasts] = useState<Array<{ id: string; message: string; type: 'success' | 'error' | 'info' | 'warning' }>>([]);
  const [hasUnsavedCode, setHasUnsavedCode] = useState(false);
  const codeSaveTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const descriptionPanelRef = useRef<HTMLDivElement>(null);
  const resizerRef = useRef<HTMLDivElement>(null);
  const [activeSession, setActiveSession] = useState<PeerInterviewSession | null>(null);
  const [showPartnerVideo, setShowPartnerVideo] = useState(false);
  const [isChangingQuestion, setIsChangingQuestion] = useState(false);
  const [isSwitchingRole, setIsSwitchingRole] = useState(false);
  const [showRejoinModal, setShowRejoinModal] = useState(false);
  const [showFeedbackForm, setShowFeedbackForm] = useState(false);
  const [sessionStartTime, setSessionStartTime] = useState<Date | null>(null);
  const [elapsedTime, setElapsedTime] = useState<number>(0);
  const [isEndingSession, setIsEndingSession] = useState(false);
  const connectionLostRef = useRef<boolean>(false);

  // Non-coding question: comments (community answers/discussion)
  const [comments, setComments] = useState<QuestionComment[]>(() => []);
  const [isLoadingComments, setIsLoadingComments] = useState(false);
  const [commentDraft, setCommentDraft] = useState('');
  const [isPostingComment, setIsPostingComment] = useState(false);
  const [isSaved, setIsSaved] = useState(false);
  const [wasAsked, setWasAsked] = useState(false);
  const [videoHelpfulRating, setVideoHelpfulRating] = useState<number>(0); // 0-5
  const [answerSort, setAnswerSort] = useState<'Hot' | 'Top' | 'New'>('Hot');
  const [isAnswerSortOpen, setIsAnswerSortOpen] = useState(false);
  const [openReplyForCommentId, setOpenReplyForCommentId] = useState<string | null>(null);
  const [replyDraftByCommentId, setReplyDraftByCommentId] = useState<Record<string, string>>({});
  const [isPostingReplyForCommentId, setIsPostingReplyForCommentId] = useState<string | null>(null);
  const [expandedRepliesByCommentId, setExpandedRepliesByCommentId] = useState<Record<string, boolean>>({});

  const showToast = useCallback((message: string, type: 'success' | 'error' | 'info' | 'warning') => {
    const id = Date.now().toString();
    setToasts((prev) => [...prev, { id, message, type }]);
  }, []);

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((toast) => toast.id !== id));
  }, []);
  const editorPanelRef = useRef<HTMLDivElement>(null);

  // Determine if both users joined (for feedback form logic)
  const hasBothUsers = useMemo(() => {
    return !!(activeSession?.interviewerId && activeSession?.intervieweeId);
  }, [activeSession?.interviewerId, activeSession?.intervieweeId]);
  const codeEditorRef = useRef<any>(null);
  const codeAreaRef = useRef<HTMLDivElement>(null);
  const testcasePanelRef = useRef<HTMLDivElement>(null);
  const horizontalResizerRef = useRef<HTMLDivElement>(null);
  const sessionHubConnectionRef = useRef<signalR.HubConnection | null>(null);

  // Handle browser back button - redirect to find interview page if coming from session
  useEffect(() => {
    const handlePopState = () => {
      const sessionId = searchParams.get('session');
      if (sessionId && activeSession) {
        // If user clicks back and we're in a session, redirect to find interview page
        const timeoutId = setTimeout(() => {
          navigate(ROUTES.FIND_PEER, { replace: true });
        }, 100);
        return () => clearTimeout(timeoutId);
      }
    };

    window.addEventListener('popstate', handlePopState);
    return () => window.removeEventListener('popstate', handlePopState);
  }, [searchParams, activeSession, navigate]);

  // Close non-coding answer sort dropdown when clicking outside
  useEffect(() => {
    if (!isAnswerSortOpen) return;

    const handleClick = (e: MouseEvent) => {
      const el = e.target as HTMLElement | null;
      if (!el) return;
      if (el.closest('.qa-sort')) return;
      setIsAnswerSortOpen(false);
    };

    document.addEventListener('click', handleClick);
    return () => document.removeEventListener('click', handleClick);
  }, [isAnswerSortOpen]);

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

  useEffect(() => {
    if (id && user?.id) {
      loadQuestion();
      // Only check session if sessionId is in URL - no automatic discovery
      const sessionIdFromUrl = searchParams.get('session');
      if (sessionIdFromUrl) {
        checkActiveSession();
      }
    }
  }, [id, user?.id, searchParams]);

  // Hydrate lightweight local UI state (non-coding actions)
  useEffect(() => {
    if (!id) return;
    const timer = setTimeout(() => {
      try {
        setIsSaved(localStorage.getItem(`question:${id}:saved`) === '1');
        setWasAsked(localStorage.getItem(`question:${id}:asked`) === '1');
        setVideoHelpfulRating(Number(localStorage.getItem(`question:${id}:videoRating`) || '0') || 0);
      } catch {
        // ignore
      }
    }, 0);
    return () => clearTimeout(timer);
  }, [id]);

  // Load comments for non-coding questions (non-blocking)
  useEffect(() => {
    if (!id || !question) return;
    const type = question.questionType?.toLowerCase();

    let cancelled = false;
    const timer = setTimeout(() => {
      setIsLoadingComments(true);
      
      // For coding/SQL questions, use commentsService
      if (type === 'coding' || type === 'sql') {
        commentsService.getComments(id)
          .then((data) => {
            if (cancelled) return;
            setComments(data);
          })
          .catch(() => {
            if (cancelled) return;
            setComments([]);
          })
          .finally(() => {
            if (cancelled) return;
            setIsLoadingComments(false);
          });
      } else {
        // For behavioral/system design, use questionService
        const sortParam = (answerSort || 'Hot').toLowerCase() as 'hot' | 'top' | 'new';
        questionService.getQuestionComments(id, 1, 50, sortParam)
          .then((data) => {
            if (cancelled) return;
            setComments(data);
          })
          .catch(() => {
            if (cancelled) return;
            setComments([]);
          })
          .finally(() => {
            if (cancelled) return;
            setIsLoadingComments(false);
          });
      }
    }, 0);

    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [id, question, answerSort]);

  // Function to reload comments (for after posting new comments)
  const loadComments = async () => {
    if (!id || !question) return;
    const type = question.questionType?.toLowerCase();
    
    try {
      setIsLoadingComments(true);
      
      // For coding/SQL questions, use the commentsService
      if (type === 'coding' || type === 'sql') {
        const data = await commentsService.getComments(id);
        setComments(data);
      } else {
        // For behavioral/system design, use questionService
        const sortParam = (answerSort || 'Hot').toLowerCase() as 'hot' | 'top' | 'new';
        const data = await questionService.getQuestionComments(id, 1, 50, sortParam);
        setComments(data);
      }
    } catch (error) {
      console.error('Error loading comments:', error);
      setComments([]);
    } finally {
      setIsLoadingComments(false);
    }
  };

  // Also check session when user becomes available
  // Session check removed - only checks when sessionId is in URL params
  // No automatic session discovery to avoid 404 errors

  // Initialize SignalR connection for session events (test results, role switching, question changes)
  // Show buttons once both users have joined (both interviewerId and intervieweeId exist)
  useEffect(() => {
    if (!activeSession || !activeSession.interviewerId || !activeSession.intervieweeId || !user?.id) {
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
          .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: (retryContext) => {
              // Show rejoin modal if connection fails
              if (retryContext.previousRetryCount > 3 && !connectionLostRef.current) {
                connectionLostRef.current = true;
                setShowRejoinModal(true);
              }
              return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
            }
          })
          .build();

        // Handle connection close (network error or kicked out)
        connection.onclose((error) => {
          if (error && !connectionLostRef.current) {
            connectionLostRef.current = true;
            setShowRejoinModal(true);
          }
        });

        // Listen for test results
        connection.on('TestResultsUpdated', (testResults: any) => {
          setRunResult(testResults.runResult || null);
          setTestResults(testResults.testResults || []);
          setExecutionResult(testResults.executionResult || null);
          setActiveTestTab('result');
        });

        // Listen for role switching
        connection.on('RoleSwitched', async (data: { sessionId: string; newActiveQuestionId?: string }) => {
          try {
            const updatedSession = await peerInterviewService.getSession(data.sessionId);
            setActiveSession(updatedSession);
            
            // If question changed (switched to second question), navigate to it and reload test cases
            const newQuestionId = data.newActiveQuestionId || updatedSession.questionId || (updatedSession as any).activeQuestionId;
            if (newQuestionId && newQuestionId !== id) {
              // Reload question data and test cases for the new question
              const [questionData, testCasesData] = await Promise.all([
                questionService.getQuestionById(newQuestionId),
                questionService.getTestCases(newQuestionId, false),
              ]);
              setQuestion(questionData);
              setTestCases(testCasesData);
              setTestCaseText('');
              setTestResults([]);
              setRunResult(null);
              navigate(`${ROUTES.QUESTIONS}/${newQuestionId}?session=${data.sessionId}`, { replace: false });
            } else {
              // Question didn't change, just reload test cases for current question
              if (id) {
                const testCasesData = await questionService.getTestCases(id, false);
                setTestCases(testCasesData);
              }
              // Reload page to update role indicators
              window.location.reload();
            }
          } catch (error) {
            console.error('Error reloading session after role switch:', error);
          }
        });

        // Listen for question changes
        connection.on('QuestionChanged', async (data: { sessionId: string; questionId: string }) => {
          try {
            const updatedSession = await peerInterviewService.getSession(data.sessionId);
            setActiveSession(updatedSession);
            
            // Reload question data and test cases for the new question
            const [questionData, testCasesData] = await Promise.all([
              questionService.getQuestionById(data.questionId),
              questionService.getTestCases(data.questionId, false),
            ]);
            setQuestion(questionData);
            setTestCases(testCasesData);
            setTestCaseText('');
            setTestResults([]);
            setRunResult(null);
            
            navigate(`${ROUTES.QUESTIONS}/${data.questionId}?session=${data.sessionId}`, { replace: false });
          } catch (error) {
            console.error('Error navigating to new question:', error);
          }
        });

        // Listen for participant joined (to synchronize timer when second user joins)
        connection.on('ParticipantJoined', async (data: { sessionId: string }) => {
          try {
            // Reload session to get updated participants and ensure timer is synchronized
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
        });

        // Listen for interview ended event
        connection.on('InterviewEnded', async (data: { sessionId: string }) => {
          try {
            // Clear timer
            if (data.sessionId) {
              localStorage.removeItem(`session_start_${data.sessionId}`);
            }
            setSessionStartTime(null);
            setElapsedTime(0);
            
            // Show feedback form
            setShowFeedbackForm(true);
            setShowPartnerVideo(false);
          } catch (error) {
            console.error('Error handling interview ended event:', error);
          }
        });

        await connection.start();
        await connection.invoke('JoinSession', activeSession.id);
        sessionHubConnectionRef.current = connection;
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
    };
  }, [activeSession?.id, activeSession?.intervieweeId, user?.id, id, navigate]);

  // Check for active peer interview session for this question
  // ONLY checks sessionId from URL params - no automatic session discovery
  const checkActiveSession = async () => {
    if (!id || !user?.id) return;
    
    // Only check if sessionId is in URL query params - no automatic discovery
    const sessionIdFromUrl = searchParams.get('session');
    if (!sessionIdFromUrl) {
      // No session ID in URL - don't try to find sessions automatically
      return;
    }
    
    try {
      const session = await peerInterviewService.getSession(sessionIdFromUrl);
      if (session) {
        // Check if user is part of this session
        const isUserInSession = session.interviewerId === user.id || 
                               (session.intervieweeId && session.intervieweeId === user.id);
        
        if (isUserInSession) {
          // User is in session - set it as active
          setActiveSession(session);
          setShowPartnerVideo(true);
          
          // Start timer ONLY if both users have joined (status is InProgress AND intervieweeId is set)
          // Exception: For "practice with a friend", start timer immediately with 1 participant
          const isFriendInterview = session.practiceType === 'friend';
          const shouldStartTimer = session.status === 'InProgress' && (isFriendInterview || session.intervieweeId);
          
          if (shouldStartTimer) {
            // Try to use the backend's startedAt time as the source of truth
            if (session.startedAt) {
              const backendStartTime = new Date(session.startedAt);
              setSessionStartTime(backendStartTime);
              const storedStartTime = localStorage.getItem(`session_start_${sessionIdFromUrl}`);
              if (!storedStartTime) {
                localStorage.setItem(`session_start_${sessionIdFromUrl}`, backendStartTime.toISOString());
              }
            } else {
              // Fallback: If backend didn't provide startedAt, check localStorage or use current time
              const storedStartTime = localStorage.getItem(`session_start_${sessionIdFromUrl}`);
              if (storedStartTime) {
                setSessionStartTime(new Date(storedStartTime));
              } else {
                // No backend time and no stored time - start timer now
                const now = new Date();
                setSessionStartTime(now);
                localStorage.setItem(`session_start_${sessionIdFromUrl}`, now.toISOString());
              }
            }
          }
          
          // If questionId doesn't match, redirect to correct question
          if (session.questionId && session.questionId !== id) {
            navigate(`${ROUTES.QUESTIONS}/${session.questionId}?session=${sessionIdFromUrl}`, { replace: true });
            return;
          }
        }
      }
    } catch (error) {
      // Silently fail - session check is optional
      // Don't log errors to console - they're expected when session doesn't exist
    }
  };

  // Set initial panel widths on mount
  useEffect(() => {
    if (descriptionPanelRef.current && editorPanelRef.current) {
      const container = document.querySelector('.question-container') as HTMLElement;
      if (container) {
        const containerWidth = container.offsetWidth;
        // Set description panel to 30% of container width
        const descriptionWidth = Math.max(300, Math.min(containerWidth * 0.3, containerWidth - 500));
        descriptionPanelRef.current.style.width = `${descriptionWidth}px`;
        descriptionPanelRef.current.style.flexShrink = '0';
        descriptionPanelRef.current.style.flexGrow = '0';
        editorPanelRef.current.style.flex = '1';
      }
    }
  }, [question]); // Run when question loads

  // Load saved code or template when question/language changes
  useEffect(() => {
    if (selectedLanguage && question && id) {
      // Clear code first to prevent showing old language code
      setCode('');
      
      // Try to load saved code from database first
      codeDraftService.getCodeDraft(id, selectedLanguage)
        .then((draft) => {
          if (draft && draft.code) {
            setCode(draft.code);
            setHasUnsavedCode(false);
          } else {
            // If no saved code, load template for the new language
            const template = getQuestionTemplate(question.title, selectedLanguage);
            setCode(template);
            setHasUnsavedCode(false);
          }
        })
        .catch((error) => {
          // Suppress console error for expected 404s (draft doesn't exist yet)
          if (error?.response?.status !== 404) {
            console.error('Failed to load code draft:', error);
          }
          // On error, load template
          const template = getQuestionTemplate(question.title, selectedLanguage);
          setCode(template);
          setHasUnsavedCode(false);
        });
    }
  }, [selectedLanguage, question?.title, id]); // Update when language or question changes

  // Auto-save code on change (debounced)
  useEffect(() => {
    if (!id || !selectedLanguage || !code || !question) return;

    // Clear previous timeout
    if (codeSaveTimeoutRef.current) {
      clearTimeout(codeSaveTimeoutRef.current);
    }

    // Debounce auto-save (save after 1 second of no changes)
    codeSaveTimeoutRef.current = setTimeout(() => {
      if (id && selectedLanguage && code && question) {
        codeDraftService.saveCodeDraft({
          questionId: id,
          language: selectedLanguage,
          code: code,
        })
        .then(() => {
          setHasUnsavedCode(false);
        })
        .catch((error) => {
          console.error('Failed to save code draft:', error);
          setHasUnsavedCode(true);
        });
      }
    }, 1000);

    return () => {
      if (codeSaveTimeoutRef.current) {
        clearTimeout(codeSaveTimeoutRef.current);
      }
    };
  }, [code, id, selectedLanguage, showToast]);

  // Extract parameters separately, debounced to avoid re-renders on every keystroke
  // Skip parameter extraction for SQL questions
  useEffect(() => {
    // Don't extract parameters for SQL questions
    if (question?.questionType?.toLowerCase() === 'sql') {
      setParameterNames([]);
      setParameterCount(1); // Set to 1 to avoid validation issues, but SQL validation will skip this
      return;
    }
    
    const timeoutId = setTimeout(() => {
      const params = extractParameterNames(code, selectedLanguage);
      setParameterNames(params);
      setParameterCount(params.length > 0 ? params.length : 1);
    }, 300); // Debounce parameter extraction

    return () => clearTimeout(timeoutId);
  }, [code, selectedLanguage, question?.questionType]); // Include question type in dependencies

  // Memoize onChange handler to prevent re-renders
  const handleCodeChange = useCallback((value: string | undefined) => {
    setCode(value || '');
    // Mark as having unsaved changes
    if (id && selectedLanguage) {
      setHasUnsavedCode(true);
    }
  }, [id, selectedLanguage]);

  // Extract parameter names from function signature
  const extractParameterNames = (code: string, language: string): string[] => {
    if (!code) return [];
    const lang = language.toLowerCase();
    try {
    
    if (lang === 'javascript' || lang === 'js' || lang === 'nodejs') {
      const patterns = [
        /(?:var|let|const)\s+\w+\s*=\s*(?:function\s*)?\(([^)]*)\)/,
        /function\s+\w+\s*\(([^)]*)\)/,
        /\w+\s*=\s*\(([^)]*)\)\s*=>/
      ];
      for (const pattern of patterns) {
        const match = code.match(pattern);
        if (match && match[1]) {
          return match[1].split(',').map(p => p.trim()).filter(p => p.length > 0);
        }
      }
    } else if (lang === 'python' || lang === 'python3') {
      // Match module-level def (not indented) — skip __dunder__ methods
      const matches = [...code.matchAll(/^def\s+(\w+)\s*\(([^)]*)\)/gm)];
      for (let i = matches.length - 1; i >= 0; i--) {
        const name = matches[i][1];
        if (!name.startsWith('__')) {
          return matches[i][2]
            .split(',')
            .map(p => p.trim().split('=')[0].trim().replace(/\*+/, ''))
            .filter(p => p.length > 0 && p !== 'self');
        }
      }
    } else if (lang === 'cpp' || lang === 'c++') {
      // Match: ReturnType* methodName(TypeA* paramA, TypeB paramB)
      const matches = [...code.matchAll(/\w+[\s*]+(\w+)\s*\(([^)]*)\)\s*\{/g)];
      if (matches.length > 0) {
        const last = matches[matches.length - 1];
        return last[2]
          .split(',')
          .map(p => p.trim().split(/\s+/).pop()?.replace(/[*&]/g, '') ?? '')
          .filter(p => p.length > 0);
      }
    } else if (lang === 'java') {
      // Match: public ReturnType methodName(TypeA paramA, TypeB paramB)
      const matches = [...code.matchAll(/(?:public|private|protected)?\s+\w+\s+(\w+)\s*\(([^)]*)\)\s*\{/g)];
      for (let i = matches.length - 1; i >= 0; i--) {
        const params = matches[i][2].trim();
        if (params) {
          return params
            .split(',')
            .map(p => p.trim().split(/\s+/).pop() ?? '')
            .filter(p => p.length > 0);
        }
      }
    } else if (lang === 'csharp' || lang === 'c#') {
      const matches = [...code.matchAll(/(?:public|private|protected)?\s+\w+\s+(\w+)\s*\(([^)]*)\)\s*\{/g)];
      for (let i = matches.length - 1; i >= 0; i--) {
        const params = matches[i][2].trim();
        if (params) {
          return params
            .split(',')
            .map(p => p.trim().split(/\s+/).pop() ?? '')
            .filter(p => p.length > 0);
        }
      }
    } else if (lang === 'go' || lang === 'golang') {
      const match = code.match(/func\s+\w+\s*\(([^)]*)\)/);
      if (match && match[1]) {
        return match[1]
          .split(',')
          .map(p => p.trim().split(/\s+/)[0]?.replace(/[*&]/g, '') ?? '')
          .filter(p => p.length > 0);
      }
    }
    
    // Default fallback: return empty so parameterCount defaults to 1
    return [];
    } catch {
      return [];
    }
  };

  // Parse and validate testcases
  const parseAndValidateTestCases = (): { valid: boolean; error?: { type: string; message: string; lineNumber?: number } } => {
    if (!testCaseText.trim()) {
      return {
        valid: false,
        error: {
          type: 'NO_TESTCASES',
          message: 'No testcases provided'
        }
      };
    }

    const lines = testCaseText.split('\n')
      .map((line, index) => ({ line: line.trimEnd(), lineNumber: index + 1 }))
      .filter(l => l.line.trim() !== ''); // Filter non-empty lines

    if (lines.length === 0) {
      return {
        valid: false,
        error: {
          type: 'NO_TESTCASES',
          message: 'No testcases provided'
        }
      };
    }

    // Check if this is a SQL question - check question type, selected language, OR test case format
    // Also check if the first line looks like a SQL test case (JSON with schema/data)
    let isSqlQuestion = question?.questionType?.toLowerCase() === 'sql' || selectedLanguage === 'sql';
    
    // Auto-detect SQL test case format by checking if first line is JSON with schema/data
    if (!isSqlQuestion && lines.length > 0) {
      try {
        const firstLine = JSON.parse(lines[0].line);
        if (firstLine && typeof firstLine === 'object' && 'schema' in firstLine && 'data' in firstLine) {
          isSqlQuestion = true;
        }
      } catch {
        // Not JSON, not a SQL test case
      }
    }
    
    // For SQL questions, validate JSON objects with schema and data
    if (isSqlQuestion) {
      // SQL test cases: each line should be a JSON object with schema and data
      for (const lineInfo of lines) {
        try {
          const parsed = JSON.parse(lineInfo.line);
          // Check if it has schema and data fields (SQL test case format)
          if (!parsed.schema || !parsed.data) {
            return {
              valid: false,
              error: {
                type: 'INVALID_SQL_TESTCASE',
                message: `Invalid SQL test case format at line ${lineInfo.lineNumber}. Expected JSON object with "schema" and "data" fields.`,
                lineNumber: lineInfo.lineNumber
              }
            };
          }
        } catch (e) {
          return {
            valid: false,
            error: {
              type: 'INVALID_JSON',
              message: `Invalid JSON format at line ${lineInfo.lineNumber}: ${e instanceof Error ? e.message : 'Parse error'}.`,
              lineNumber: lineInfo.lineNumber
            }
          };
        }
      }
      // All SQL test cases are valid
      return { valid: true };
    }

    // Note: we no longer validate the line-count divisibility here on the frontend.
    // The backend performs that check with the authoritative parameterCount extracted
    // from the submitted source code, so any frontend mismatch just caused false errors.

    // Try to parse each line as JSON
    for (const lineInfo of lines) {
      try {
        JSON.parse(lineInfo.line);
      } catch {
        // If JSON parse fails, treat as raw string (allowed)
        continue;
      }
    }

    return { valid: true };
  };

  // Helper function to format values for display
  const formatValue = (value: any): string => {
    if (value === null || value === undefined) {
      return 'undefined';
    }
    if (typeof value === 'string') {
      try {
        // Try to parse and re-stringify to format JSON strings
        const parsed = JSON.parse(value);
        return JSON.stringify(parsed, null, 2);
      } catch {
        return value;
      }
    }
    return JSON.stringify(value, null, 2);
  };

  // Compact format for Output/Expected (single line, no pretty printing)
  const formatCompactValue = (value: any): string => {
    if (value === null || value === undefined) {
      return String(value);
    }
    if (typeof value === 'string') {
      try {
        const parsed = JSON.parse(value);
        return JSON.stringify(parsed);
      } catch {
        return value;
      }
    }
    return JSON.stringify(value);
  };

  // Parse error message to extract line number
  const parseErrorLineNumber = useCallback((errorMessage: string | { message?: string; stack?: string } | undefined): number | null => {
    if (!errorMessage) {
      return null;
    }
    
    // Extract message and stack from error object
    let message = '';
    let stack = '';
    if (typeof errorMessage === 'string') {
      message = errorMessage;
      stack = errorMessage;
    } else if (errorMessage) {
      message = errorMessage.message || '';
      stack = errorMessage.stack || '';
    }
    
    // Combine message and stack for parsing
    const fullText = (stack || message).trim();
    if (!fullText) {
      return null;
    }

    // Patterns to match (in order of specificity):
    // /box/script.js:7:19
    // /box/script.js:7
    // at addTwoNumbers (/box/script.js:7:19)
    // at Object.<anonymous> (/box/script.js:7:19)
    // Line 7
    // ReferenceError: ListNode is not defined\n    at addTwoNumbers (/box/script.js:7:19)
    const patterns = [
      /[:\/](?:script|main|code|box)\.(?:js|py|java|cpp|cs|go|ts):(\d+)(?::\d+)?/i,
      /\([^)]*[:\/](?:script|main|code|box)\.(?:js|py|java|cpp|cs|go|ts):(\d+)(?::\d+)?\)/i,
      /at\s+[^(]+\([^)]*[:\/](?:script|main|code|box)\.(?:js|py|java|cpp|cs|go|ts):(\d+)(?::\d+)?\)/i,
      /at\s+[^\n]+[:\/](?:script|main|code|box)\.(?:js|py|java|cpp|cs|go|ts):(\d+)(?::\d+)?/i,
      /[Ll]ine\s+(\d+)/,
      /:(\d+):\d+/,
      /:(\d+)(?:\s|$|\))/,
    ];

    for (const pattern of patterns) {
      const match = fullText.match(pattern);
      if (match && match[1]) {
        const lineNum = parseInt(match[1], 10);
        if (lineNum > 0 && lineNum < 10000) { // Sanity check
          return lineNum;
        }
      }
    }

    return null;
  }, []);

  // Scroll to error line when error markers change
  useEffect(() => {
    if (errorMarkers.length > 0 && codeEditorRef.current) {
      const firstError = errorMarkers[0];
      // Use setTimeout to ensure editor is ready
      setTimeout(() => {
        if (codeEditorRef.current) {
          try {
            codeEditorRef.current.revealLineInCenter(firstError.line);
          } catch (e) {
            // Editor might not be ready yet, ignore
          }
        }
      }, 100);
    }
  }, [errorMarkers]);

  // Initialize testcase text from existing test cases on load
  useEffect(() => {
    if (!question) return;
    
    const visibleCases = testCases.filter(tc => !tc.isHidden);
    
    if (visibleCases.length === 0) {
      // No visible test cases
      if (testCaseText) {
        setTestCaseText('');
      }
      return;
    }
    
    // Check if this is a SQL question OR if test cases look like SQL format (JSON with schema/data)
    const isSqlQuestion = question.questionType?.toLowerCase() === 'sql' || selectedLanguage === 'sql';
    
    // Also auto-detect SQL format by checking first test case
    let isSqlFormat = isSqlQuestion;
    if (!isSqlFormat && visibleCases.length > 0) {
      try {
        const firstInput = JSON.parse(visibleCases[0].input);
        if (firstInput && typeof firstInput === 'object' && 'schema' in firstInput && 'data' in firstInput) {
          isSqlFormat = true;
        }
      } catch {
        // Not JSON with schema/data
      }
    }
    
    // For SQL questions, keep the full JSON object with schema and data
    if (isSqlFormat) {
      const sqlTestCases = visibleCases.map(testCase => testCase.input);
      const newTestCaseText = sqlTestCases.join('\n');
      setTestCaseText(newTestCaseText);
    } else {
      // For coding questions, convert to line-based format
      const lines: string[] = [];
      
      visibleCases.forEach(testCase => {
        try {
          const inputObj = JSON.parse(testCase.input);
          // Extract values in order (assuming parameter order)
          const values = Object.values(inputObj);
          values.forEach(val => {
            lines.push(JSON.stringify(val));
          });
        } catch {
          // If not JSON, use as-is
          lines.push(testCase.input);
        }
      });
      
      const newTestCaseText = lines.join('\n');
      setTestCaseText(newTestCaseText);
    }
  }, [testCases, question, selectedLanguage]); // Removed testCaseText from deps to avoid infinite loops


  // Vertical resizer (between description and editor)
  useEffect(() => {
    const resizer = resizerRef.current;
    if (!resizer) return;

    let isResizing = false;
    let startX = 0;
    let startWidth = 0;

    const handleMouseDown = (e: MouseEvent) => {
      const descriptionPanel = descriptionPanelRef.current;
      if (!descriptionPanel) return;
      
      e.preventDefault();
      e.stopPropagation();
      isResizing = true;
      startX = e.clientX;
      startWidth = descriptionPanel.offsetWidth;
      document.body.style.cursor = 'col-resize';
      document.body.style.userSelect = 'none';
      document.body.style.pointerEvents = 'none';
    };

    const handleMouseMove = (e: MouseEvent) => {
      if (!isResizing) return;
      
      const descriptionPanel = descriptionPanelRef.current;
      const editorPanel = editorPanelRef.current;
      if (!descriptionPanel || !editorPanel) return;
      
      e.preventDefault();
      const deltaX = e.clientX - startX;
      const newWidth = startWidth + deltaX;
      const container = document.querySelector('.question-container') as HTMLElement;
      if (!container) return;
      
      const containerWidth = container.offsetWidth;
      const minWidth = 300;
      const maxWidth = containerWidth - 300;
      
      if (newWidth >= minWidth && newWidth <= maxWidth) {
        descriptionPanel.style.width = `${newWidth}px`;
        descriptionPanel.style.flexShrink = '0';
        descriptionPanel.style.flexGrow = '0';
        editorPanel.style.flex = '1';
      }
    };

    const handleMouseUp = () => {
      if (isResizing) {
        isResizing = false;
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
        document.body.style.pointerEvents = '';
      }
    };

    resizer.addEventListener('mousedown', handleMouseDown);
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      resizer.removeEventListener('mousedown', handleMouseDown);
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [question]);

  // Horizontal resizer (between editor panel and testcase panel)
  useEffect(() => {
    const resizer = horizontalResizerRef.current;
    if (!resizer) return;

    let isResizing = false;
    let startY = 0;
    let startEditorHeight = 0;
    let startTestcaseHeight = 0;

    const handleMouseDown = (e: MouseEvent) => {
      const editorPanel = editorPanelRef.current;
      const testcasePanel = testcasePanelRef.current;
      if (!editorPanel || !testcasePanel) return;
      
      e.preventDefault();
      e.stopPropagation();
      isResizing = true;
      startY = e.clientY;
      startEditorHeight = editorPanel.offsetHeight;
      startTestcaseHeight = testcasePanel.offsetHeight;
      document.body.style.cursor = 'row-resize';
      document.body.style.userSelect = 'none';
      document.body.style.pointerEvents = 'none';
    };

    const handleMouseMove = (e: MouseEvent) => {
      if (!isResizing) return;
      
      const editorPanel = editorPanelRef.current;
      const testcasePanel = testcasePanelRef.current;
      if (!editorPanel || !testcasePanel) return;
      
      e.preventDefault();
      const deltaY = e.clientY - startY;
      const newEditorHeight = startEditorHeight + deltaY;
      const newTestcaseHeight = startTestcaseHeight - deltaY;
      
      // Get the wrapper container to calculate available space
      const wrapper = editorPanel.parentElement;
      if (!wrapper) return;
      
      const wrapperHeight = wrapper.offsetHeight;
      const resizerHeight = 8; // Height of the resizer
      const availableHeight = wrapperHeight - resizerHeight;
      
      const minEditorHeight = 300;
      const minTestcaseHeight = 200;
      const maxEditorHeight = availableHeight - minTestcaseHeight;
      
      // Constrain editor height
      if (newEditorHeight >= minEditorHeight && newEditorHeight <= maxEditorHeight) {
        editorPanel.style.height = `${newEditorHeight}px`;
        editorPanel.style.flex = 'none';
        editorPanel.style.flexShrink = '0';
        editorPanel.style.flexGrow = '0';
        
        // Adjust testcase panel
        if (newTestcaseHeight >= minTestcaseHeight) {
          testcasePanel.style.height = `${newTestcaseHeight}px`;
          testcasePanel.style.flex = 'none';
          testcasePanel.style.flexShrink = '0';
          testcasePanel.style.flexGrow = '0';
        }
      }
    };

    const handleMouseUp = () => {
      if (isResizing) {
        isResizing = false;
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
        document.body.style.pointerEvents = '';
      }
    };

    resizer.addEventListener('mousedown', handleMouseDown);
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      resizer.removeEventListener('mousedown', handleMouseDown);
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [question]);

  const handleChangeQuestion = async () => {
    if (!activeSession || !user?.id) return;
    
    if (activeSession.interviewerId !== user.id) {
      showToast('Only the interviewer can change the question', 'error');
      return;
    }

    try {
      setIsChangingQuestion(true);
      await peerInterviewService.changeQuestion(activeSession.id);
      
      // Reload the session to get the updated question
      const updatedSession = await peerInterviewService.getSession(activeSession.id);
      setActiveSession(updatedSession);
      
      // Notify partner via SignalR
      if (sessionHubConnectionRef.current && updatedSession.questionId) {
        try {
          await sessionHubConnectionRef.current.invoke('SendQuestionChanged', activeSession.id, updatedSession.questionId);
        } catch (error) {
          console.error('Failed to notify partner of question change:', error);
        }
      }
      
      // Navigate to the new question using replace to allow browser back button
      if (updatedSession.questionId) {
        navigate(`${ROUTES.QUESTIONS}/${updatedSession.questionId}?session=${activeSession.id}`, { replace: false });
      } else {
        showToast('Question changed, but no new question was assigned', 'warning');
      }
    } catch (error: any) {
      const errorMessage = error?.response?.data?.message || 'Failed to change question';
      showToast(errorMessage, 'error');
    } finally {
      setIsChangingQuestion(false);
    }
  };

  const handleSwitchRole = async () => {
    if (!activeSession || !user?.id || isSwitchingRole) return;

    try {
      setIsSwitchingRole(true);
      await peerInterviewService.switchRoles(activeSession.id);
      
      // Reload the session to get fresh data
      const freshSession = await peerInterviewService.getSession(activeSession.id);
      setActiveSession(freshSession);
      
      // Notify partner via SignalR
      if (sessionHubConnectionRef.current) {
        try {
          await sessionHubConnectionRef.current.invoke('SendRoleSwitched', activeSession.id);
        } catch (error) {
          console.error('Failed to notify partner of role switch:', error);
        }
      }
      
      // Clear loading state before navigation to prevent spinner from staying
      setIsSwitchingRole(false);
      
      // Navigate to the new question (if assigned) using replace to allow browser back button
      if (freshSession.questionId && freshSession.questionId !== id) {
        navigate(`${ROUTES.QUESTIONS}/${freshSession.questionId}?session=${freshSession.id}`, { replace: false });
      } else {
        // If same question, just reload the page to update role indicator
        window.location.reload();
      }
    } catch (error: any) {
      const errorMessage = error?.response?.data?.message || 'Failed to switch roles';
      showToast(errorMessage, 'error');
      setIsSwitchingRole(false); // Always clear loading state on error
    }
  };

  const handleEndSession = async () => {
    if (!activeSession || !user?.id || isEndingSession) return;
    
    if (!confirm('Are you sure you want to end this interview session?')) {
      return;
    }

    try {
      setIsEndingSession(true);
      
      // First, reload session to get latest participant data
      const latestSession = await peerInterviewService.getSession(activeSession.id);
      setActiveSession(latestSession);
      
      // End the interview session
      await peerInterviewService.endInterview(activeSession.id);
      
      // Clear timer
      if (activeSession.id) {
        localStorage.removeItem(`session_start_${activeSession.id}`);
      }
      setSessionStartTime(null);
      setElapsedTime(0);
      
      // Show feedback form
      setShowFeedbackForm(true);
      setShowPartnerVideo(false);
    } catch (error: any) {
      console.error('Error ending session:', error);
      showToast('Failed to end session', 'error');
    } finally {
      setIsEndingSession(false);
    }
  };

  const handleRejoin = () => {
    setShowRejoinModal(false);
    connectionLostRef.current = false;
    if (activeSession) {
      // Reload the page to reconnect
      window.location.reload();
    } else {
      // Try to get session from URL
      const sessionId = searchParams.get('session');
      if (sessionId) {
        window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
      }
    }
  };

  const handleFeedbackComplete = () => {
    setShowFeedbackForm(false);
    setActiveSession(null);
    // Navigate to find peer page
    navigate(ROUTES.FIND_PEER);
  };

  const formatTime = (seconds: number): string => {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;
    return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
  };

  // Timer effect - update elapsedTime every second
  useEffect(() => {
    // For friend interviews, start timer immediately
    // For matched interviews, wait for both users
    const isFriendInterview = activeSession?.practiceType === 'friend';
    const shouldStartTimer = sessionStartTime && (isFriendInterview || activeSession?.intervieweeId);
    
    if (!shouldStartTimer) {
      setElapsedTime(0);
      return;
    }

    const updateTimer = () => {
      const now = new Date();
      const elapsed = Math.floor((now.getTime() - sessionStartTime.getTime()) / 1000);
      setElapsedTime(elapsed);
    };

    // Update immediately
    updateTimer();

    // Update every second
    const intervalId = setInterval(updateTimer, 1000);

    return () => clearInterval(intervalId);
  }, [sessionStartTime, activeSession?.intervieweeId, activeSession?.practiceType]);

  const loadQuestion = async () => {
    if (!id) return;
    try {
      setLoading(true);
      // Clear test case text when loading new question
      setTestCaseText('');
      const [questionData, testCasesData, solutionsData] = await Promise.all([
        questionService.getQuestionById(id),
        questionService.getTestCases(id, false),
        questionService.getSolutions(id),
      ]);
      setQuestion(questionData);
      setTestCases(testCasesData);
      setSolutions(solutionsData);
      
      // Log for debugging SQL test cases
      if (questionData?.questionType?.toLowerCase() === 'sql') {
        console.log(`SQL question loaded: ${questionData.title}, Test cases: ${testCasesData.length}`);
      }
      
      // Set default language based on question type
      if (questionData?.questionType?.toLowerCase() === 'sql') {
        setSelectedLanguage('sql');
      } else if (!selectedLanguage || selectedLanguage === 'sql') {
        // If currently SQL but question is not SQL, switch to javascript
        setSelectedLanguage('javascript');
      }
      
      // Check if user has solved this question (optimized lookup)
      // This is independent of code loading - code always loads from saved draft
      // Run this check asynchronously without blocking the page load
      if (user?.id) {
        // Use optimized endpoint - don't await to avoid blocking
        solutionService.hasSolvedQuestion(id)
          .then((hasSolved) => {
            setIsSolved(hasSolved);
          })
          .catch((error) => {
            // Silently fail - assume not solved if error occurs
            // Don't log expected errors (401, 404) to avoid console noise
            if (error?.response?.status !== 401 && error?.response?.status !== 404) {
              console.error('Error checking solved status:', error);
            }
            setIsSolved(false);
          });
      } else {
        setIsSolved(false);
      }
    } catch (error) {
    } finally {
      setLoading(false);
    }
  };

  const handleRunCode = async () => {
    if (!question || !id) return;
    
    // Clear submit results when clicking Run
    setTestResults([]);
    setExecutionResult(null);
    
    // Validate testcases first
    const validation = parseAndValidateTestCases();
    if (!validation.valid) {
      setValidationError(validation.error!);
      setActiveTestTab('result');
      setRunResult({
        status: 'INVALID_INPUT',
        validationError: validation.error!,
        cases: []
      });
      return;
    }

    setValidationError(null);
    setErrorMarkers([]); // Clear previous error markers
    setIsExecuting(true);
    setActiveTestTab('result');
    
    try {
      
      // Use new endpoint with line-based testcases
      const result = await codeExecutionService.runCodeWithTestCases(id, {
        sourceCode: code,
        language: selectedLanguage,
        testCaseText: testCaseText
      });
      
      
      setRunResult(result);
      
      // Share test results with partner via SignalR
      if (activeSession && activeSession.intervieweeId && sessionHubConnectionRef.current) {
        try {
          await sessionHubConnectionRef.current.invoke('SendTestResults', activeSession.id, {
            runResult: result,
            testResults: result.cases || [],
            executionResult: null,
          });
        } catch (error) {
          console.error('Failed to share test results:', error);
        }
      }
      
      // Auto-select first failing case, or Case 1 if all pass
      if (result.cases && result.cases.length > 0) {
        const firstFailure = result.cases.find(c => c.passed === false || c.error);
        setSelectedCaseIndex(firstFailure ? firstFailure.caseIndex : 1);
        
        // Extract error line numbers and set markers
        const markers: Array<{ line: number; column?: number; endColumn?: number; message: string }> = [];
        result.cases.forEach(caseResult => {
          if (caseResult.error) {
            const lineNum = parseErrorLineNumber(caseResult.error);
            if (lineNum) {
              const errorMsg = typeof caseResult.error === 'string' 
                ? caseResult.error 
                : caseResult.error.message || caseResult.error.stack || 'Runtime error';
              markers.push({
                line: lineNum,
                message: errorMsg.substring(0, 200), // Limit message length
              });
            }
          }
        });
        setErrorMarkers(markers);
      }
    } catch (error: any) {
      const errorMessage = error?.response?.data?.error || 
                          error?.response?.data?.message || 
                          error?.message || 
                          'Failed to execute code. Please check your code and try again.';
      setRunResult({
        status: 'RUNTIME_ERROR',
        cases: [{
          caseIndex: 1,
          inputValues: [],
          parameterNames: parameterNames,
          stdout: '',
          output: '',
          runtime: 0,
          memory: 0,
          status: 'Error',
          error: {
            message: errorMessage
          }
        }]
      });
      
      // Extract error line number
      const lineNum = parseErrorLineNumber(errorMessage);
      if (lineNum) {
        setErrorMarkers([{
          line: lineNum,
          message: errorMessage.substring(0, 200),
        }]);
      } else {
        setErrorMarkers([]);
      }
    } finally {
      setIsExecuting(false);
    }
  };

  const handleSubmit = async () => {
    if (!question || !id) return;
    
    setIsExecuting(true);
    
    // Clear run result when submitting to ensure submit results are shown
    setRunResult(null);
    
    // Clear test results during loading (don't show placeholder)
    setTestResults([]);
    setExecutionResult(null); // Clear execution result when submitting
    setActiveTestTab('result');
    
    try {
      
      // First validate the solution
      const results = await codeExecutionService.validateSolution(id, {
        sourceCode: code,
        language: selectedLanguage,
      });
      
      
      // Map results to match our state structure
      const mappedResults = results.map((result) => ({
        testCaseNumber: result.testCaseNumber,
        passed: result.passed,
        stdout: result.stdout || '',
        output: result.output || '',
        expectedOutput: result.expectedOutput || '',
        input: result.input || '',
        error: result.error || '',
        runtime: result.runtime || 0,
        memory: result.memory || 0,
        status: result.status || 'Unknown',
      }));
      
      // Calculate overall status
      const allPassed = mappedResults.every(r => r.passed);
      const overallStatus = allPassed ? 'Accepted' : 'Wrong Answer';
      const passedCount = mappedResults.filter(r => r.passed).length;
      const totalCount = mappedResults.length;
      
      // Share test results with partner via SignalR
      if (activeSession && activeSession.intervieweeId && sessionHubConnectionRef.current) {
        try {
          await sessionHubConnectionRef.current.invoke('SendTestResults', activeSession.id, {
            runResult: null,
            testResults: mappedResults,
            executionResult: { status: overallStatus, passedCount, totalCount },
          });
        } catch (error) {
          console.error('Failed to share test results:', error);
        }
      }
      
      // For submit: Show different results based on success/failure
      if (allPassed) {
        // Success: Show first 3 test cases
        const firstThreeCases = mappedResults.slice(0, 3);
        setTestResults(firstThreeCases);
        if (firstThreeCases.length > 0) {
          setSelectedCaseIndex(firstThreeCases[0].testCaseNumber);
        } else {
          setSelectedCaseIndex(1);
        }
      } else {
        // Failure: Show only the first failing test case
        const firstFailingCase = mappedResults.find(r => !r.passed);
        if (firstFailingCase) {
          setTestResults([firstFailingCase]);
          setSelectedCaseIndex(firstFailingCase.testCaseNumber);
          
          // Extract error line number from first failing case
          if (firstFailingCase.error) {
            const lineNum = parseErrorLineNumber(firstFailingCase.error);
            if (lineNum) {
              setErrorMarkers([{
                line: lineNum,
                message: typeof firstFailingCase.error === 'string' 
                  ? firstFailingCase.error.substring(0, 200)
                  : 'Runtime error',
              }]);
            } else {
              setErrorMarkers([]);
            }
          } else {
            setErrorMarkers([]);
          }
        } else {
          // Fallback: show first case if somehow no failing case found
          setTestResults(mappedResults.slice(0, 1));
          setSelectedCaseIndex(mappedResults[0]?.testCaseNumber || 1);
          setErrorMarkers([]);
        }
      }
      
      // Set execution result with test case count
      setExecutionResult({
        status: overallStatus,
        runtime: 0, // Don't show runtime at top level
        memory: 0, // Don't show memory at top level
        output: `Test Cases: ${passedCount}/${totalCount} passed`,
        error: '', // Don't show error message at top level
      });
      
      // Stop loading indicator - validation is complete
      setIsExecuting(false);
      
      // Only save solution to database if all tests passed (silently in background)
      if (allPassed) {
        // Save solution silently (no loading indicator, no notification during save)
        // Run in background without blocking UI
        solutionService.submitSolution({
          questionId: id!,
          language: selectedLanguage,
          code: code,
        }).then(() => {
          // Mark as solved since validation passed
          setIsSolved(true);
          // Don't clear code on successful submission - keep it in database
          // Show success message
          showToast('Solution submitted successfully!', 'success');
          
          // Trigger analytics refresh for other tabs/pages
          localStorage.setItem('solutionSubmitted', Date.now().toString());
          window.dispatchEvent(new Event('storage'));
          // Also dispatch custom event for same-tab listeners
          window.dispatchEvent(new Event('solutionSubmitted'));
        }).catch((saveError: any) => {
          // Log error but don't fail the submission - validation already passed
          // Show error toast
          const errorMessage = saveError?.response?.data?.error || 
                             saveError?.response?.data?.message || 
                             saveError?.message || 
                             'Solution validated successfully but failed to save. Please try submitting again.';
          showToast(errorMessage, 'error');
        });
      }
    } catch (error: any) {
      const errorMessage = error?.response?.data?.error || 
                          error?.response?.data?.message || 
                          error?.message || 
                          'Failed to validate solution. Please check your code and try again.';
      setTestResults([{
        testCaseNumber: 1,
        passed: false,
        error: errorMessage,
        status: 'Error',
        runtime: 0,
        memory: 0,
        output: '',
      }]);
      setExecutionResult({
        status: 'Error',
        error: errorMessage,
        output: '',
        runtime: 0,
        memory: 0,
      });
      
      // Extract error line number
      const lineNum = parseErrorLineNumber(errorMessage);
      if (lineNum) {
        setErrorMarkers([{
          line: lineNum,
          message: errorMessage.substring(0, 200),
        }]);
      } else {
        setErrorMarkers([]);
      }
      
      // Show error toast
      showToast(errorMessage, 'error');
    } finally {
      setIsExecuting(false);
    }
  };

  const getDifficultyClass = (difficulty: string) => {
    return difficulty.toLowerCase();
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <i className="fas fa-spinner fa-spin text-4xl text-blue-600 mb-4"></i>
          <p className="text-gray-600">Loading question...</p>
        </div>
      </div>
    );
  }

  if (!question) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <h2 className="text-2xl font-bold mb-4">Question not found</h2>
          <Link to={ROUTES.QUESTIONS} className="btn-primary">
            Back to Questions
          </Link>
        </div>
      </div>
    );
  }

  // Show coding question layout for coding questions and SQL questions
  if (question.questionType?.toLowerCase() !== 'coding' && question.questionType?.toLowerCase() !== 'sql') {
    const getInitials = (name?: string) => {
      const cleaned = (name || '').trim();
      if (!cleaned) return 'U';
      const parts = cleaned.split(/\s+/).filter(Boolean);
      const first = parts[0]?.[0] ?? 'U';
      const last = parts.length > 1 ? (parts[parts.length - 1]?.[0] ?? '') : '';
      return (first + last).toUpperCase();
    };

    const currentUserName = `${user?.firstName || ''} ${user?.lastName || ''}`.trim();

    const handleToggleSaved = () => {
      if (!id) return;
      setIsSaved((prev) => {
        const next = !prev;
        try {
          localStorage.setItem(`question:${id}:saved`, next ? '1' : '0');
        } catch {
          // ignore
        }
        return next;
      });
    };

    const handleToggleWasAsked = () => {
      if (!id) return;
      setWasAsked((prev) => {
        const next = !prev;
        try {
          localStorage.setItem(`question:${id}:asked`, next ? '1' : '0');
        } catch {
          // ignore
        }
        return next;
      });
    };

    const handleShare = () => {
      const url = window.location.href;
      if (navigator.share) {
        navigator.share({ title: question.title, url }).catch(() => {});
        return;
      }
      navigator.clipboard?.writeText(url)
        .then(() => showToast('Link copied to clipboard', 'success'))
        .catch(() => showToast('Failed to copy link', 'error'));
    };

    const handleFlag = () => {
      showToast("Thanks — we'll review this question.", 'info');
    };

    const handleSetVideoRating = (rating: number) => {
      if (!id) return;
      const next = Math.max(0, Math.min(5, rating));
      setVideoHelpfulRating(next);
      try {
        localStorage.setItem(`question:${id}:videoRating`, String(next));
      } catch {
        // ignore
      }
    };

    const handleAddComment = async () => {
      if (!id) return;
      const content = commentDraft.trim();
      if (!content) return;
      if (isPostingComment) return;

      setIsPostingComment(true);
      try {
        const created = await questionService.addQuestionComment(id, content);
        setComments((prev) => [created, ...prev]);
        setCommentDraft('');
      } catch (error) {
        showToast('Failed to post comment. Please try again.', 'error');
      } finally {
        setIsPostingComment(false);
      }
    };

    const updateCommentTree = (
      list: QuestionComment[],
      commentId: string,
      updater: (c: QuestionComment) => QuestionComment
    ): QuestionComment[] => {
      return list.map((c) => {
        if (c.id === commentId) return updater(c);
        if (c.replies && c.replies.length > 0) {
          return { ...c, replies: updateCommentTree(c.replies, commentId, updater) };
        }
        return c;
      });
    };

    const handleToggleUpvote = async (commentId: string) => {
      if (!id) return;
      if (!user) {
        showToast('Please log in to upvote comments', 'info');
        return;
      }

      // Optimistic UI
      setComments((prev) =>
        updateCommentTree(prev, commentId, (c) => {
          const nextHasUpvoted = !c.hasUpvoted;
          const nextCount = Math.max(0, (c.upvoteCount || 0) + (nextHasUpvoted ? 1 : -1));
          return { ...c, hasUpvoted: nextHasUpvoted, upvoteCount: nextCount };
        })
      );

      try {
        const currentComment = comments.find(c => c.id === commentId);
        const wasUpvoted = currentComment?.hasUpvoted;
        
        if (wasUpvoted) {
          await commentsService.removeUpvote(commentId);
        } else {
          await commentsService.upvoteComment(commentId);
        }
        
        showToast(wasUpvoted ? 'Upvote removed' : 'Comment upvoted!', 'success');
      } catch (error) {
        // Revert on failure
        setComments((prev) =>
          updateCommentTree(prev, commentId, (c) => ({
            ...c,
            hasUpvoted: !c.hasUpvoted,
            upvoteCount: Math.max(0, (c.upvoteCount || 0) + (c.hasUpvoted ? -1 : 1))
          }))
        );
        showToast('Failed to update upvote. Please try again.', 'error');
      }
    };

    const handleOpenReply = (commentId: string) => {
      setOpenReplyForCommentId((prev) => (prev === commentId ? null : commentId));
      setReplyDraftByCommentId((prev) => ({ ...prev, [commentId]: prev[commentId] ?? '' }));
    };

    const handleToggleReplies = (commentId: string) => {
      setExpandedRepliesByCommentId((prev) => ({ ...prev, [commentId]: !prev[commentId] }));
    };

    const handleAddReply = async (parentCommentId: string) => {
      if (!id) return;
      const content = (replyDraftByCommentId[parentCommentId] || '').trim();
      if (!content) return;
      if (isPostingReplyForCommentId) return;

      setIsPostingReplyForCommentId(parentCommentId);
      try {
        const created = await questionService.addQuestionComment(id, content, parentCommentId);
        setComments((prev) =>
          updateCommentTree(prev, parentCommentId, (c) => ({ ...c, replies: [...(c.replies || []), created] }))
        );
        setReplyDraftByCommentId((prev) => ({ ...prev, [parentCommentId]: '' }));
        setOpenReplyForCommentId(null);
        setExpandedRepliesByCommentId((prev) => ({ ...prev, [parentCommentId]: true }));
      } catch {
        showToast('Failed to post reply. Please try again.', 'error');
      } finally {
        setIsPostingReplyForCommentId(null);
      }
    };

    return (
      <>
        <ToastContainer toasts={toasts} onRemove={removeToast} />
        <div className="question-detail-page noncoding-detail">
          <nav className="question-navbar">
            <div className="question-nav-left">
              <Link to={ROUTES.QUESTIONS} className="back-btn" aria-label="Back to all questions">
                <i className="fas fa-chevron-left"></i>
              </Link>
              <Link to={ROUTES.QUESTIONS} className="noncoding-breadcrumb">
                All Questions
              </Link>
            </div>
          </nav>

          <div className="noncoding-container">
            <div className="noncoding-layout">
              <main className="noncoding-main">
                <h1 className="noncoding-title">{question.title}</h1>
                <div className="noncoding-actions-row">
                  {user?.role === 'admin' && id ? (
                    <Link
                      to={`${ROUTES.EDIT_QUESTION}/${id}`}
                      className="noncoding-action-btn"
                      aria-label="Edit question"
                    >
                      <i className="fa-regular fa-pen-to-square"></i>
                      <span>Edit</span>
                    </Link>
                  ) : null}
                  <button
                    type="button"
                    className={`noncoding-action-btn ${isSaved ? 'is-active' : ''}`}
                    onClick={handleToggleSaved}
                    aria-label={isSaved ? 'Unsave question' : 'Save question'}
                  >
                    <i className={`fa-${isSaved ? 'solid' : 'regular'} fa-bookmark`}></i>
                    <span>Save</span>
                  </button>
                  <button
                    type="button"
                    className={`noncoding-action-btn ${wasAsked ? 'is-active' : ''}`}
                    onClick={handleToggleWasAsked}
                    aria-label={wasAsked ? 'Remove “I was asked this”' : 'Mark “I was asked this”'}
                  >
                    <i className="fa-regular fa-circle-check"></i>
                    <span>I was asked this</span>
                  </button>
                  <button type="button" className="noncoding-action-btn" onClick={handleShare} aria-label="Share question">
                    <i className="fa-solid fa-share-nodes"></i>
                    <span>Share</span>
                  </button>
                  <button type="button" className="noncoding-action-btn" onClick={handleFlag} aria-label="Flag question">
                    <i className="fa-regular fa-flag"></i>
                    <span>Flag</span>
                  </button>
                </div>

                {question.videoUrl ? (
                  <section className="noncoding-video">
                    <video
                      className="noncoding-video-player"
                      controls
                      preload="metadata"
                      src={question.videoUrl}
                    />
                    <div className="noncoding-video-feedback">
                      <div className="noncoding-video-feedback-label">Was this video helpful?</div>
                      <div className="noncoding-stars" role="radiogroup" aria-label="Video helpful rating">
                        {Array.from({ length: 5 }).map((_, idx) => {
                          const star = idx + 1;
                          const active = videoHelpfulRating >= star;
                          return (
                            <button
                              key={star}
                              type="button"
                              className={`noncoding-star ${active ? 'is-on' : ''}`}
                              onClick={() => handleSetVideoRating(star)}
                              aria-label={`${star} star${star === 1 ? '' : 's'}`}
                            >
                              <i className={`fa-${active ? 'solid' : 'regular'} fa-star`}></i>
                            </button>
                          );
                        })}
                      </div>
                    </div>
                  </section>
                ) : null}

                <section className="noncoding-section">
                  <h2 className="noncoding-section-title">Question</h2>
                  <div className="noncoding-question-body">{question.description}</div>

                  {question.hints && question.hints.length > 0 ? (
                    <>
                      <div className="noncoding-divider" />
                      <h3 className="noncoding-subsection-title">Hints</h3>
                      <div className="noncoding-accordion">
                        {question.hints.map((hint, idx) => {
                          const isExpanded = expandedHints.has(idx);
                          return (
                            <div key={idx} className={`noncoding-accordion-item ${isExpanded ? 'is-open' : ''}`}>
                              <button
                                type="button"
                                className="noncoding-accordion-trigger"
                                onClick={() => {
                                  const next = new Set(expandedHints);
                                  if (isExpanded) next.delete(idx);
                                  else next.add(idx);
                                  setExpandedHints(next);
                                }}
                              >
                                <span className="noncoding-accordion-title">
                                  <i className="far fa-lightbulb"></i>
                                  Hint {idx + 1}
                                </span>
                                <i className={`fas fa-chevron-${isExpanded ? 'down' : 'right'} noncoding-accordion-icon`}></i>
                              </button>
                              {isExpanded ? (
                                <div className="noncoding-accordion-content">{hint}</div>
                              ) : null}
                            </div>
                          );
                        })}
                      </div>
                    </>
                  ) : null}
                </section>

                <section className="noncoding-section">
                  <div className="noncoding-guidelines">
                    <div className="noncoding-guidelines-title">Community guidelines</div>
                    <ul className="noncoding-guidelines-list">
                      <li>Stay on topic. Use this section for submitting solutions and providing feedback to others.</li>
                      <li>Be inclusive. Please respect others&apos; opinions and beliefs.</li>
                    </ul>
                  </div>
                </section>

                <section className="noncoding-section">
                  <div className="qa-editor">
                    <div className="qa-editor-header">
                      <div className="qa-avatar">
                        {user?.profilePictureUrl ? (
                          <img className="qa-avatar-img" src={user.profilePictureUrl} alt={currentUserName || 'User'} />
                        ) : (
                          <div className="qa-avatar-fallback">{getInitials(currentUserName)}</div>
                        )}
                      </div>
                      <div className="qa-editor-author">
                        <div className="qa-author-name">{currentUserName || 'User'}</div>
                      </div>
                    </div>

                    <MarkdownEditor
                      value={commentDraft}
                      onChange={setCommentDraft}
                      placeholder="Add your own answer to this question..."
                      rows={6}
                      ariaLabel="Add your own answer"
                    />

                    <div className="qa-editor-footer">
                      <button
                        type="button"
                        className="qa-submit-btn"
                        onClick={handleAddComment}
                        disabled={isPostingComment || !commentDraft.trim()}
                      >
                        {isPostingComment ? 'Submitting...' : 'Submit'}
                      </button>
                    </div>
                  </div>
                </section>

                <section className="noncoding-section">
                  <div className="qa-answers-bar">
                    <div className="qa-answers-count">
                      <i className="fa-regular fa-comment-dots"></i>
                      <span>{comments.length}</span>
                    </div>

                    <div className="qa-sort">
                      <button
                        type="button"
                        className="qa-sort-btn"
                        onClick={() => setIsAnswerSortOpen((v) => !v)}
                        aria-label="Sort answers"
                      >
                        <span>🔥 {answerSort}</span>
                        <i className="fa-solid fa-chevron-down"></i>
                      </button>
                      {isAnswerSortOpen ? (
                        <div className="qa-sort-menu" role="menu">
                          {(['Hot', 'Top', 'New'] as const).map((opt) => (
                            <button
                              key={opt}
                              type="button"
                              className={`qa-sort-option ${answerSort === opt ? 'is-active' : ''}`}
                              onClick={() => {
                                setAnswerSort(opt);
                                setIsAnswerSortOpen(false);
                              }}
                              role="menuitem"
                            >
                              {opt === 'Hot' ? '🔥 Hot' : opt === 'Top' ? '⬆️ Top' : '✨ New'}
                            </button>
                          ))}
                        </div>
                      ) : null}
                    </div>
                  </div>

                  {isLoadingComments ? (
                    <div className="noncoding-muted">Loading answers...</div>
                  ) : comments.length > 0 ? (
                    <div className="qa-answer-list">
                      {comments.map((c) => (
                        <div key={c.id} className="qa-answer-card">
                          <div className="qa-answer-header">
                            <div className="qa-avatar qa-avatar--sm">
                              {c.userProfilePictureUrl ? (
                                <img className="qa-avatar-img qa-avatar-img--sm" src={c.userProfilePictureUrl} alt={c.userName || 'User'} />
                              ) : (
                                <div className="qa-avatar-fallback qa-avatar-fallback--sm">{getInitials(c.userName)}</div>
                              )}
                            </div>
                            <div className="qa-answer-meta">
                              <div className="qa-author-name">{c.userName || 'User'}</div>
                              <div className="qa-date">{new Date(c.createdAt).toLocaleDateString()}</div>
                            </div>
                          </div>
                          <MarkdownRenderer content={c.content} className="qa-answer-body markdown-body" />

                          <div className="qa-answer-actions">
                            <button
                              type="button"
                              className={`qa-action-btn ${c.hasUpvoted ? 'is-active' : ''}`}
                              onClick={() => handleToggleUpvote(c.id)}
                              aria-label="Upvote"
                            >
                              <i className="fa-solid fa-arrow-up"></i>
                              <span>{c.upvoteCount ?? 0}</span>
                            </button>
                            <button
                              type="button"
                              className="qa-action-btn"
                              onClick={() => handleOpenReply(c.id)}
                              aria-label="Reply"
                            >
                              <i className="fa-regular fa-comment"></i>
                              <span>Reply</span>
                            </button>
                            {c.replies && c.replies.length > 0 ? (
                              <button
                                type="button"
                                className="qa-action-btn"
                                onClick={() => handleToggleReplies(c.id)}
                                aria-label="Toggle replies"
                              >
                                <i className={`fa-solid fa-chevron-${expandedRepliesByCommentId[c.id] ? 'up' : 'down'}`}></i>
                                <span>{expandedRepliesByCommentId[c.id] ? 'Hide' : 'View'} {c.replies.length} {c.replies.length === 1 ? 'reply' : 'replies'}</span>
                              </button>
                            ) : null}
                          </div>

                          {openReplyForCommentId === c.id ? (
                            <div className="qa-reply-editor">
                              <div className="qa-reply-editor-inner">
                                <MarkdownEditor
                                  value={replyDraftByCommentId[c.id] || ''}
                                  onChange={(next) => setReplyDraftByCommentId((prev) => ({ ...prev, [c.id]: next }))}
                                  placeholder="Write a reply..."
                                  rows={3}
                                  ariaLabel="Write a reply"
                                />
                                <div className="qa-reply-actions">
                                  <button
                                    type="button"
                                    className="qa-reply-cancel"
                                    onClick={() => setOpenReplyForCommentId(null)}
                                  >
                                    Cancel
                                  </button>
                                  <button
                                    type="button"
                                    className="qa-submit-btn"
                                    onClick={() => handleAddReply(c.id)}
                                    disabled={isPostingReplyForCommentId === c.id || !(replyDraftByCommentId[c.id] || '').trim()}
                                  >
                                    {isPostingReplyForCommentId === c.id ? 'Submitting...' : 'Reply'}
                                  </button>
                                </div>
                              </div>
                            </div>
                          ) : null}

                          {c.replies && c.replies.length > 0 && expandedRepliesByCommentId[c.id] ? (
                            <div className="qa-replies">
                              {c.replies.map((r) => (
                                <div key={r.id} className="qa-reply-card">
                                  <div className="qa-answer-header">
                                    <div className="qa-avatar qa-avatar--sm">
                                      {r.userProfilePictureUrl ? (
                                        <img className="qa-avatar-img qa-avatar-img--sm" src={r.userProfilePictureUrl} alt={r.userName || 'User'} />
                                      ) : (
                                        <div className="qa-avatar-fallback qa-avatar-fallback--sm">{getInitials(r.userName)}</div>
                                      )}
                                    </div>
                                    <div className="qa-answer-meta">
                                      <div className="qa-author-name">{r.userName || 'User'}</div>
                                      <div className="qa-date">{new Date(r.createdAt).toLocaleDateString()}</div>
                                    </div>
                                  </div>
                                  <MarkdownRenderer content={r.content} className="qa-answer-body markdown-body" />
                                  <div className="qa-answer-actions">
                                    <button
                                      type="button"
                                      className={`qa-action-btn ${r.hasUpvoted ? 'is-active' : ''}`}
                                      onClick={() => handleToggleUpvote(r.id)}
                                      aria-label="Upvote reply"
                                    >
                                      <i className="fa-solid fa-arrow-up"></i>
                                      <span>{r.upvoteCount ?? 0}</span>
                                    </button>
                                  </div>
                                </div>
                              ))}
                            </div>
                          ) : null}
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="noncoding-muted">No answers yet. Be the first to add one.</div>
                  )}
                </section>
              </main>

              <aside className="noncoding-sidebar">
                <div className="noncoding-card">
                  <h3 className="noncoding-card-title">Interview Details</h3>

                  {question.roleTags && question.roleTags.length > 0 ? (
                    <div className="noncoding-field">
                      <div className="noncoding-field-label">Roles</div>
                      <div className="noncoding-pill-row">
                        {question.roleTags.map((role) => (
                          <span key={role} className="noncoding-pill">{role}</span>
                        ))}
                      </div>
                    </div>
                  ) : null}

                  {question.companyTags && question.companyTags.length > 0 ? (
                    <div className="noncoding-field">
                      <div className="noncoding-field-label">Companies</div>
                      <div className="noncoding-pill-row">
                        {question.companyTags.map((company) => (
                          <span key={company} className="noncoding-pill">
                            <CompanyIcon company={company} size={14} className="company-pill-icon" />
                            {company}
                          </span>
                        ))}
                      </div>
                    </div>
                  ) : null}

                  <div className="noncoding-field">
                    <div className="noncoding-field-label">Categories</div>
                    <div className="noncoding-pill-row">
                      <span className="noncoding-pill">{question.category}</span>
                    </div>
                  </div>
                </div>

                <div className="noncoding-card noncoding-card-muted">
                  <h3 className="noncoding-card-title">Related Courses</h3>
                  <div className="noncoding-muted">Coming soon.</div>
                </div>

                {question.relatedQuestions && question.relatedQuestions.length > 0 ? (
                  <div className="noncoding-card">
                    <h3 className="noncoding-card-title">Related Questions</h3>
                    <div className="related-question-list">
                      {question.relatedQuestions.map((rq) => (
                        <Link key={rq.id} to={`${ROUTES.QUESTIONS}/${rq.id}`} className="related-question-link">
                          {rq.title}
                        </Link>
                      ))}
                    </div>
                  </div>
                ) : null}
              </aside>
            </div>
          </div>
        </div>
      </>
    );
  }

  return (
    <>
      <ToastContainer toasts={toasts} onRemove={removeToast} />
      <div className="question-detail-page">
        <nav className="question-navbar">
        <div className="question-nav-left">
          <Link to={ROUTES.QUESTIONS} className="back-btn">
            <i className="fas fa-chevron-left"></i>
          </Link>
          <div className="question-nav-title">
            <span className="nav-question-number">{question.title}</span>
          </div>
        </div>
        <div className="question-nav-center">
          <button 
            className="nav-btn" 
            onClick={handleRunCode}
            disabled={isExecuting || !code.trim()}
          >
            <i className={`fas ${isExecuting ? 'fa-spinner fa-spin' : 'fa-play'}`}></i> 
            {isExecuting ? 'Running...' : 'Run'}
          </button>
          <button 
            className="nav-btn nav-btn-primary" 
            onClick={handleSubmit}
            disabled={isExecuting || !code.trim()}
          >
            <i className="fas fa-check"></i> Submit
          </button>
        </div>
        <div className="question-nav-right">
          <button 
            className="nav-icon-btn" 
            title="Reset Solution"
            onClick={() => {
              if (id && question && selectedLanguage) {
                // Delete saved code draft from database
                codeDraftService.deleteCodeDraft(id, selectedLanguage)
                  .then(() => {
                    // Load template
                    const template = getQuestionTemplate(question.title, selectedLanguage);
                    setCode(template);
                    setHasUnsavedCode(false);
                    // Clear test results
                    setRunResult(null);
                    setTestResults([]);
                    setExecutionResult(null);
                    showToast('Solution reset to template', 'info');
                  })
                  .catch((error) => {
                    console.error('Failed to delete code draft:', error);
                    // Still reset locally even if delete fails
                    const template = getQuestionTemplate(question.title, selectedLanguage);
                    setCode(template);
                    setHasUnsavedCode(false);
                    setRunResult(null);
                    setTestResults([]);
                    setExecutionResult(null);
                    showToast('Solution reset to template', 'info');
                  });
              }
            }}
          >
            <i className="fas fa-redo"></i>
          </button>
          {user?.role === 'admin' && (
            <Link
              to={`${ROUTES.EDIT_QUESTION}/${id}`}
              className="nav-icon-btn"
              title="Edit Question"
            >
              <i className="fas fa-edit"></i>
            </Link>
          )}
          {/* Show session controls once both users have joined (session exists with both participants) */}
          {/* For "practice with a friend", show controls immediately even with one participant */}
          {activeSession && (activeSession.interviewerId || activeSession.intervieweeId) && (
            // For friend interviews (practiceType="friend"), show controls with 1+ participants
            // For matched interviews, require both participants
            (activeSession.practiceType === 'friend' || (activeSession.interviewerId && activeSession.intervieweeId))
          ) && (
            <>
              {/* Show current role as plain text title */}
              <div className="role-title">
                {activeSession.interviewerId === user?.id ? 'Interviewer' : 'Interviewee'}
              </div>
              {/* Change Question button - only for interviewer */}
              {activeSession.interviewerId === user?.id && (
                <button
                  className="session-control-btn session-control-change"
                  title="Change Question"
                  onClick={handleChangeQuestion}
                  disabled={isChangingQuestion}
                >
                  {isChangingQuestion ? (
                    <i className="fas fa-spinner fa-spin"></i>
                  ) : (
                    <i className="fas fa-sync-alt"></i>
                  )}
                  <span>Change Question</span>
                </button>
              )}
              {/* Switch Role button - for both */}
              <button
                className="session-control-btn session-control-switch"
                title="Switch Role"
                onClick={handleSwitchRole}
                disabled={isSwitchingRole}
              >
                {isSwitchingRole ? (
                  <i className="fas fa-spinner fa-spin"></i>
                ) : (
                  <i className="fas fa-exchange-alt"></i>
                )}
                <span>Switch Roles</span>
              </button>
              {/* End Session button - for both */}
              <button
                className="session-control-btn session-control-end"
                title="End Session"
                onClick={handleEndSession}
                disabled={isEndingSession}
              >
                {isEndingSession ? (
                  <i className="fas fa-spinner fa-spin"></i>
                ) : (
                  <i className="fas fa-stop"></i>
                )}
                <span>End Session</span>
              </button>
              {/* Timer display */}
              {sessionStartTime && (
                <div className="nav-timer">
                  <i className="fas fa-clock"></i>
                  <span>{formatTime(elapsedTime)}</span>
                </div>
              )}
            </>
          )}
        </div>
      </nav>

      {/* Rejoin Modal - Only show if connection lost, not for periodic notifications */}
      {/* Periodic notifications are handled by SessionNotificationManager in App.tsx */}
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

      {/* Feedback Form Modal */}
      {showFeedbackForm && activeSession && user?.id && hasBothUsers && (
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
          interviewType={activeSession.interviewType}
          date={activeSession.scheduledTime}
          onComplete={handleFeedbackComplete}
          onCancel={handleFeedbackComplete}
        />
      )}

      {/* No partner - just redirect */}
      {showFeedbackForm && activeSession && user?.id && !hasBothUsers && (
        <div className="feedback-overlay" style={{
          position: 'fixed',
          top: 0,
          left: 0,
          width: '100vw',
          height: '100vh',
          background: 'rgba(0, 0, 0, 0.5)',
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          zIndex: 10000
        }}>
          <div style={{
            background: 'white',
            borderRadius: '12px',
            padding: '2rem',
            maxWidth: '400px',
            textAlign: 'center'
          }}>
            <i className="fas fa-info-circle" style={{ fontSize: '3rem', color: '#6366f1', marginBottom: '1rem' }}></i>
            <h3 style={{ fontSize: '1.5rem', fontWeight: 600, color: '#111827', marginBottom: '0.5rem' }}>Interview Ended</h3>
            <p style={{ color: '#6b7280', marginBottom: '1.5rem', lineHeight: 1.6 }}>
              Your practice partner didn't join this session, so there's no feedback to provide.
            </p>
            <button 
              onClick={handleFeedbackComplete}
              style={{
                background: '#6366f1',
                color: 'white',
                border: 'none',
                padding: '0.75rem 2rem',
                borderRadius: '8px',
                fontWeight: 500,
                cursor: 'pointer'
              }}
            >
              Continue
            </button>
          </div>
        </div>
      )}

      {showPartnerVideo && activeSession && activeSession.interviewerId && activeSession.intervieweeId && activeSession.status === 'InProgress' && (
        <DraggableVideoChat
          sessionId={activeSession.id}
          userId={user?.id || ''}
          peerUserId={
            activeSession.interviewerId === user?.id
              ? activeSession.intervieweeId
              : activeSession.interviewerId
          }
          onError={(error) => {
            showToast(error, 'error');
          }}
        />
      )}

      <div className="question-container">
        <div className="description-panel" ref={descriptionPanelRef}>
          <div className="panel-tabs">
            <button
              className={`panel-tab ${activeTab === 'description' ? 'active' : ''}`}
              onClick={() => setActiveTab('description')}
            >
              Description
            </button>
            {/* Only show Hints and Solution tabs for interviewer or when not in an active session */}
            {(!activeSession || activeSession.interviewerId === user?.id) && (
              <>
                <button
                  className={`panel-tab ${activeTab === 'hints' ? 'active' : ''}`}
                  onClick={() => setActiveTab('hints')}
                >
                  Hints
                </button>
                <button
                  className={`panel-tab ${activeTab === 'solution' ? 'active' : ''}`}
                  onClick={() => setActiveTab('solution')}
                >
                  Solution
                </button>
              </>
            )}
          </div>

          {activeTab === 'description' && (
            <div className="panel-content" style={{ position: 'relative', display: 'flex', flexDirection: 'column', height: '100%', padding: 0 }}>
              <div style={{ flex: 1, overflowY: 'auto', padding: '1.5rem' }}>
                <div className="question-header">
                  <div style={{ display: 'flex', gap: '1rem', alignItems: 'flex-start' }}>
                    <div style={{ flex: 1 }}>
                      <div className="question-title-row">
                        <h1 className="question-title">{question.title}</h1>
                        {isSolved && (
                          <div className="question-solved-status">
                            <i className="fas fa-check-circle"></i>
                            <span>Solved</span>
                          </div>
                        )}
                        <BookmarkButton questionId={question.id} size="medium" showLabel />
                      </div>
                    </div>
                  </div>
                  <div className="question-meta">
                    <span className={`difficulty-badge ${getDifficultyClass(question.difficulty)}`}>
                      {question.difficulty}
                    </span>
                    {question.tags && question.tags.length > 0 && (
                      <button className="meta-btn">
                        <i className="far fa-bookmark"></i> Topics
                      </button>
                    )}
                    {question.companyTags && question.companyTags.length > 0 && (
                      <button className="meta-btn">
                        <i className="far fa-building"></i> Companies
                      </button>
                    )}
                    {question.hints && question.hints.length > 0 && (
                      <button className="meta-btn">
                        <i className="far fa-lightbulb"></i> Hint 1
                      </button>
                    )}
                  </div>
                </div>

                <div className="problem-content">
                <div dangerouslySetInnerHTML={{ __html: question.description.replace(/\n/g, '<br />') }} />
                
                {question.examples && question.examples.length > 0 && (
                  <>
                    {question.examples.map((example, idx) => (
                      <div key={idx} className="example-section">
                        <h3>Example {idx + 1}:</h3>
                        <div className="example-box">
                          {example.input && (
                            <div className="example-line">
                              <strong>Input:</strong> <code>{example.input}</code>
                            </div>
                          )}
                          {example.output && (
                            <div className="example-line">
                              <strong>Output:</strong> <code>{example.output}</code>
                            </div>
                          )}
                          {example.explanation && (
                            <div className="example-line">
                              <strong>Explanation:</strong> {example.explanation}
                            </div>
                          )}
                        </div>
                      </div>
                    ))}
                  </>
                )}

                {question.constraints && (
                  <div className="constraints-section">
                    <h3>Constraints:</h3>
                    <ul className="constraints-list">
                      {question.constraints.split('\n').map((constraint, idx) => {
                        // Check if constraint contains code (backticks, <=, >=, numbers with operators)
                        const hasCode = constraint.includes('`') || 
                                       constraint.includes('<=') || 
                                       constraint.includes('>=') ||
                                       /-?\d+\^?\d*\s*[<>=]/.test(constraint);
                        
                        // Extract code parts and text parts
                        const processConstraint = (text: string) => {
                          if (!hasCode) {
                            return <span dangerouslySetInnerHTML={{ __html: text }} />;
                          }
                          
                          // For code constraints, wrap the entire constraint in a code box
                          return (
                            <span className="constraint-code-box" dangerouslySetInnerHTML={{ __html: text }} />
                          );
                        };
                        
                        return (
                          <li key={idx} className="constraint-item">
                            {processConstraint(constraint)}
                          </li>
                        );
                      })}
                    </ul>
                  </div>
                )}

                {question.acceptanceRate && (
                  <div className="stats-section">
                    <div className="stat-item">
                      <span className="stat-label">Accepted</span>
                      <span className="accepted-stats">
                        <span className="accepted-number">19,926,834</span>
                        <span className="accepted-total">/35.1M</span>
                      </span>
                      <span className="stat-separator">|</span>
                      <span className="stat-label">Acceptance Rate</span>
                      <span className="stat-value">{question.acceptanceRate}%</span>
                    </div>
                  </div>
                )}

                {question.tags && question.tags.length > 0 && (
                  <div 
                    className="stats-section collapsible-stats-section"
                    onClick={() => setTopicsExpanded(!topicsExpanded)}
                  >
                    <div className="stat-item stat-item-collapsible">
                      <span className="stat-label">Topics</span>
                      {topicsExpanded && (
                        <div className="stat-tags">
                          {question.tags.map((tag, idx) => (
                            <button key={idx} className="stat-tag">{tag}</button>
                          ))}
                        </div>
                      )}
                    </div>
                    <i className={`fas fa-chevron-${topicsExpanded ? 'up' : 'down'} collapse-icon`}></i>
                  </div>
                )}

                {question.companyTags && question.companyTags.length > 0 && (
                  <div 
                    className="stats-section collapsible-stats-section"
                    onClick={() => setCompaniesExpanded(!companiesExpanded)}
                  >
                    <div className="stat-item stat-item-collapsible">
                      <span className="stat-label">Companies</span>
                      {companiesExpanded && (
                        <div className="stat-tags">
                          {question.companyTags.map((company, idx) => (
                            <button key={idx} className="stat-tag">
                              <CompanyIcon company={company} size={14} className="company-pill-icon" />
                              {company}
                            </button>
                          ))}
                        </div>
                      )}
                    </div>
                    <i className={`fas fa-chevron-${companiesExpanded ? 'up' : 'down'} collapse-icon`}></i>
                  </div>
                )}

                {/* Discussion Section - scrollable content */}
                {id && (
                  <QuestionDiscussion
                    questionId={id}
                    comments={comments}
                    onCommentAdded={loadComments}
                  />
                )}
                </div>
              </div>

              {/* Fixed voting bar at bottom */}
              {id && (
                <QuestionVoting 
                  questionId={id} 
                  commentCount={comments.length}
                  onCommentsClick={() => {
                    // Scroll to comments section if it exists
                    const commentsSection = document.getElementById('discussion-section');
                    if (commentsSection) {
                      commentsSection.scrollIntoView({ behavior: 'smooth' });
                    }
                  }}
                />
              )}
            </div>
          )}

          {/* Only show hints content for interviewer or when not in an active session */}
          {activeTab === 'hints' && (!activeSession || activeSession.interviewerId === user?.id) && (
            <div className="panel-content">
              <h2>Hints</h2>
              {question.hints && question.hints.length > 0 ? (
                <div className="hints-container">
                  {question.hints.map((hint, idx) => {
                    const isExpanded = expandedHints.has(idx);
                    return (
                      <div key={idx} className="hint-item">
                        <div 
                          className="hint-header collapsible-header"
                          onClick={() => {
                            const newExpanded = new Set(expandedHints);
                            if (isExpanded) {
                              newExpanded.delete(idx);
                            } else {
                              newExpanded.add(idx);
                            }
                            setExpandedHints(newExpanded);
                          }}
                        >
                          <div className="hint-title">
                            <i className="far fa-lightbulb"></i>
                            <span>Hint {idx + 1}</span>
                          </div>
                          <i className={`fas fa-chevron-${isExpanded ? 'down' : 'right'} collapse-icon`}></i>
                        </div>
                        {isExpanded && (
                          <div className="hint-content">
                            <p>{hint}</p>
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              ) : (
                <p>No hints available for this question.</p>
              )}
            </div>
          )}

          {/* Only show solution content for interviewer or when not in an active session */}
          {activeTab === 'solution' && (!activeSession || activeSession.interviewerId === user?.id) && (
            <div className="panel-content">
              <h2>Solution</h2>
              {solutions.length > 0 ? (
                <div className="solutions-container">
                  {solutions.map((solution, idx) => {
                    // Extract approach from explanation or use default
                    const getApproach = (): string => {
                      if (!solution.explanation) return `Approach ${idx + 1}`;
                      const explanation = solution.explanation.toLowerCase();
                      if (explanation.includes('hash') || explanation.includes('map') || explanation.includes('dictionary')) {
                        return 'Hash Map';
                      } else if (explanation.includes('two pointer') || explanation.includes('two-pointer')) {
                        return 'Two Pointers';
                      } else if (explanation.includes('brute force') || explanation.includes('nested loop')) {
                        return 'Brute Force';
                      } else if (explanation.includes('dynamic programming') || explanation.includes('dp')) {
                        return 'Dynamic Programming';
                      } else if (explanation.includes('sliding window')) {
                        return 'Sliding Window';
                      } else if (explanation.includes('binary search')) {
                        return 'Binary Search';
                      } else if (explanation.includes('greedy')) {
                        return 'Greedy';
                      } else if (explanation.includes('backtrack')) {
                        return 'Backtracking';
                      } else if (explanation.includes('sort')) {
                        return 'Sorting';
                      } else {
                        return `Approach ${idx + 1}`;
                      }
                    };
                    const approach = getApproach();
                    const solutionKey = solution.id;
                    const isExpanded = expandedSolutions.has(solutionKey);
                    
                    // Parse explanation to extract main description
                    const parseExplanation = (explanation: string) => {
                      if (!explanation) return { main: '', complexity: null };
                      
                      // Try to extract time and space complexity sections
                      const timeMatch = explanation.match(/Time Complexity[:\s]+([^\.]+(?:\.|$))/i);
                      const spaceMatch = explanation.match(/Space Complexity[:\s]+([^\.]+(?:\.|$))/i);
                      
                      let mainText = explanation;
                      if (timeMatch) {
                        mainText = mainText.replace(/Time Complexity[:\s]+[^\.]+(?:\.|$)/i, '').trim();
                      }
                      if (spaceMatch) {
                        mainText = mainText.replace(/Space Complexity[:\s]+[^\.]+(?:\.|$)/i, '').trim();
                      }
                      
                      return {
                        main: mainText,
                        timeComplexity: timeMatch ? timeMatch[1].trim() : null,
                        spaceComplexity: spaceMatch ? spaceMatch[1].trim() : null
                      };
                    };
                    
                    const parsed = parseExplanation(solution.explanation || '');
                    
                    return (
                      <div key={solution.id} className="solution-item">
                        <div 
                          className="solution-header collapsible-header"
                          onClick={() => {
                            const newExpanded = new Set(expandedSolutions);
                            if (isExpanded) {
                              newExpanded.delete(solutionKey);
                            } else {
                              newExpanded.add(solutionKey);
                            }
                            setExpandedSolutions(newExpanded);
                          }}
                        >
                          <span className="solution-approach">Solution {idx + 1}: {approach} approach</span>
                          <i className={`fas fa-chevron-${isExpanded ? 'down' : 'right'} collapse-icon`}></i>
                        </div>
                        {isExpanded && (
                          <div className="solution-content">
                            {parsed.main && (
                              <div className="solution-explanation">
                                <p>{parsed.main}</p>
                              </div>
                            )}
                            <div className="solution-code">
                              <div className="solution-code-header">
                                <span>{solution.language}</span>
                              </div>
                              <pre><code>{solution.code}</code></pre>
                            </div>
                            {(parsed.timeComplexity || parsed.spaceComplexity || solution.timeComplexity || solution.spaceComplexity) && (
                              <div className="solution-complexity">
                                {parsed.timeComplexity && (
                                  <div className="complexity-item">
                                    <strong>Time Complexity:</strong> {parsed.timeComplexity}
                                  </div>
                                )}
                                {!parsed.timeComplexity && solution.timeComplexity && (
                                  <div className="complexity-item">
                                    <strong>Time Complexity:</strong> {solution.timeComplexity}
                                  </div>
                                )}
                                {parsed.spaceComplexity && (
                                  <div className="complexity-item">
                                    <strong>Space Complexity:</strong> {parsed.spaceComplexity}
                                  </div>
                                )}
                                {!parsed.spaceComplexity && solution.spaceComplexity && (
                                  <div className="complexity-item">
                                    <strong>Space Complexity:</strong> {solution.spaceComplexity}
                                  </div>
                                )}
                              </div>
                            )}
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              ) : (
                <p>No solutions available for this question.</p>
              )}
            </div>
          )}
        </div>

        <div className="panel-resizer" ref={resizerRef}></div>

        <div style={{ display: 'flex', flexDirection: 'column', flex: 1, minWidth: 0 }}>
          <div className="editor-panel" ref={editorPanelRef}>
            <div className="editor-header">
              <div className="editor-controls">
                <select
                  className="language-select"
                  value={selectedLanguage}
                  onChange={(e) => setSelectedLanguage(e.target.value)}
                >
                  {question?.questionType?.toLowerCase() === 'sql' ? (
                    <option value="sql">SQL</option>
                  ) : (
                    <>
                      <option value="javascript">JavaScript</option>
                      <option value="python">Python3</option>
                      <option value="java">Java</option>
                      <option value="cpp">C++</option>
                      <option value="csharp">C#</option>
                      <option value="go">Go</option>
                    </>
                  )}
                </select>
                {hasUnsavedCode && (
                  <span 
                    style={{ 
                      fontSize: '0.75rem', 
                      color: '#f59e0b',
                      display: 'flex',
                      alignItems: 'center',
                      gap: '4px'
                    }}
                    title="Code has unsaved changes"
                  >
                    <i className="fas fa-circle" style={{ fontSize: '6px' }}></i>
                    Unsaved
                  </span>
                )}
              </div>
            </div>

            <div className="code-area" ref={codeAreaRef} style={{ display: 'flex', flexDirection: 'column', minHeight: 0 }}>
              {activeSession && activeSession.interviewerId && activeSession.intervieweeId ? (
                <CollaborativeCodeEditor
                  value={code}
                  language={selectedLanguage}
                  onChange={(value) => {
                    setCode(value || '');
                    handleCodeChange(value || '');
                  }}
                  sessionId={activeSession.id}
                  userId={user?.id || ''}
                  peerUserId={
                    activeSession.interviewerId === user?.id
                      ? activeSession.intervieweeId
                      : activeSession.interviewerId
                  }
                  isInterviewer={activeSession.interviewerId === user?.id}
                  onError={(error) => {
                    showToast(error, 'error');
                  }}
                />
              ) : (
                <CodeEditor
                  value={code}
                  language={selectedLanguage}
                  onChange={handleCodeChange}
                  onCursorChange={(line, column) => {
                    setCursorLine(line);
                    setCursorColumn(column);
                  }}
                  errorMarkers={errorMarkers}
                  editorRef={codeEditorRef}
                  height="100%"
                  theme="light"
                  readOnly={false}
                />
              )}
            </div>
            {/* Status bar at bottom of code editor */}
            <div className="editor-status-bar">
              <div className="status-bar-left">
                {!hasUnsavedCode && (
                  <span className="status-saved">
                    <i className="fas fa-check"></i> Saved
                  </span>
                )}
              </div>
              <div className="status-bar-right">
                <span className="status-cursor">
                  Ln {cursorLine}, Col {cursorColumn}
                </span>
              </div>
            </div>
          </div>

          <div className="panel-resizer horizontal-resizer" ref={horizontalResizerRef}></div>

          <div className="testcase-panel" ref={testcasePanelRef}>
            <div className="testcase-header">
              <div className="testcase-tabs">
                <button 
                  className={`testcase-tab ${activeTestTab === 'testcase' ? 'active' : ''}`}
                  onClick={() => setActiveTestTab('testcase')}
                >
                  <i className={`fas ${activeTestTab === 'testcase' ? 'fa-check-circle' : 'fa-edit'}`}></i> Testcase
                </button>
                <button 
                  className={`testcase-tab ${activeTestTab === 'result' ? 'active' : ''}`}
                  onClick={() => setActiveTestTab('result')}
                >
                  <i className={`fas ${activeTestTab === 'result' ? 'fa-clipboard-check' : 'fa-clipboard-list'}`}></i> Test Result
                </button>
              </div>
            </div>
            <div className="testcase-content">
              {activeTestTab === 'testcase' ? (
                <div className="testcase-editor-container" style={{ position: 'relative', flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
                  <div style={{ flex: 1, minHeight: 0 }}>
                    <CodeEditor
                      value={testCaseText}
                      language="plaintext"
                      onChange={(value) => {
                        setTestCaseText(value || '');
                        setValidationError(null); // Clear error on change
                      }}
                      onCursorChange={(line) => {
                        setSelectedTestLine(line);
                      }}
                      height="100%"
                      theme="light"
                    />
                  </div>
                  {validationError && (
                    <div className="testcase-validation-error" style={{ padding: '8px 12px', backgroundColor: '#fee2e2', color: '#991b1b', borderTop: '1px solid #fecaca', fontSize: '0.875rem' }}>
                      <i className="fas fa-exclamation-circle"></i>
                      {' '}
                      {validationError.message}
                      {validationError.lineNumber && ` (Line ${validationError.lineNumber})`}
                    </div>
                  )}
                </div>
              ) : (
                <div className="test-result-content" style={{ padding: '16px', overflowY: 'auto', height: '100%' }}>
                  {runResult && runResult.validationError ? (
                    <div className="validation-error-banner" style={{ padding: '16px', backgroundColor: '#fef3c7', border: '1px solid #fbbf24', borderRadius: '6px', marginBottom: '16px' }}>
                      <div className="validation-error-header" style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px', fontWeight: 600, color: '#92400e' }}>
                        <i className="fas fa-exclamation-triangle"></i>
                        <strong>Cannot run: invalid testcase input</strong>
                      </div>
                      <div className="validation-error-details" style={{ color: '#78350f', fontSize: '0.875rem' }}>
                        {runResult.validationError.message}
                        {runResult.validationError.lineNumber && ` (Line ${runResult.validationError.lineNumber})`}
                      </div>
                    </div>
                  ) : runResult && runResult.cases.length > 0 ? (
                    <>
                      {/* Overall Status */}
                      <div className="result-overall-status" style={{ marginBottom: '16px', display: 'flex', alignItems: 'center', gap: '12px', flexWrap: 'wrap' }}>
                        <div className={`status-badge-large ${
                          runResult.status === 'ACCEPTED' ? 'accepted' : 
                          runResult.status === 'WRONG_ANSWER' ? 'wrong-answer' :
                          runResult.status === 'RUNTIME_ERROR' ? 'runtime-error' :
                          'error'
                        }`} style={{ 
                          display: 'inline-flex', 
                          alignItems: 'center', 
                          gap: '8px', 
                          padding: '0',
                          borderRadius: '0',
                          fontSize: '1rem',
                          fontWeight: 600,
                          marginBottom: '0',
                          backgroundColor: 'transparent',
                          color: runResult.status === 'ACCEPTED' ? '#059669' : runResult.status === 'WRONG_ANSWER' ? '#dc2626' : runResult.status === 'RUNTIME_ERROR' ? '#f59e0b' : '#6b7280',
                          border: 'none'
                        }}>
                          <i className={`fas ${
                            runResult.status === 'ACCEPTED' ? 'fa-check-circle' : 
                            runResult.status === 'WRONG_ANSWER' ? 'fa-times-circle' :
                            runResult.status === 'RUNTIME_ERROR' ? 'fa-exclamation-triangle' :
                            'fa-times-circle'
                          }`}></i>
                          {runResult.status === 'ACCEPTED' ? 'Accepted' :
                           runResult.status === 'WRONG_ANSWER' ? 'Wrong Answer' :
                           runResult.status === 'RUNTIME_ERROR' ? 'Runtime Error' :
                           runResult.status}
                        </div>
                        {runResult.runtimeMs !== undefined && runResult.runtimeMs > 0 && (
                          <span style={{ 
                            fontSize: '0.875rem', 
                            color: '#6b7280',
                            fontWeight: 400
                          }}>
                            Runtime: {runResult.runtimeMs.toFixed(0)} ms
                          </span>
                        )}
                      </div>

                      {/* Case Subtabs - Show all test cases for manual runs */}
                      <div className="case-subtabs" style={{ display: 'flex', gap: '8px', marginBottom: '16px', flexWrap: 'wrap' }}>
                        {runResult.cases
                          .map((caseResult) => (
                          <button
                            key={caseResult.caseIndex}
                            className={`case-subtab ${selectedCaseIndex === caseResult.caseIndex ? 'active' : ''} ${
                              caseResult.passed === true ? 'passed' :
                              caseResult.error ? 'error' :
                              caseResult.passed === false ? 'failed' : ''
                            }`}
                            onClick={() => setSelectedCaseIndex(caseResult.caseIndex)}
                            style={{
                              padding: '6px 12px',
                              borderRadius: '6px',
                              border: selectedCaseIndex === caseResult.caseIndex ? '1px solid #e5e7eb' : 'none',
                              backgroundColor: caseResult.passed === true ? '#d1fae5' : caseResult.error ? '#fee2e2' : caseResult.passed === false ? '#fee2e2' : '#f3f4f6',
                              cursor: 'pointer',
                              fontSize: '0.875rem',
                              display: 'flex',
                              alignItems: 'center',
                              gap: '6px',
                              color: caseResult.passed === true ? '#065f46' : caseResult.error ? '#991b1b' : caseResult.passed === false ? '#991b1b' : '#6b7280',
                              transition: 'all 0.2s ease',
                              opacity: selectedCaseIndex === caseResult.caseIndex ? 1 : 0.8
                            }}
                            onMouseEnter={(e) => {
                              if (selectedCaseIndex !== caseResult.caseIndex) {
                                e.currentTarget.style.opacity = '1';
                              }
                            }}
                            onMouseLeave={(e) => {
                              if (selectedCaseIndex !== caseResult.caseIndex) {
                                e.currentTarget.style.opacity = '0.8';
                              }
                            }}
                          >
                            <i className={`fas ${
                              caseResult.passed === true ? 'fa-check-circle' :
                              caseResult.error ? 'fa-exclamation-triangle' :
                              'fa-times-circle'
                            }`}></i>
                            Case {caseResult.caseIndex}
                          </button>
                        ))}
                      </div>

                      {/* Selected Case Details */}
                      {runResult.cases.find(c => c.caseIndex === selectedCaseIndex) && (() => {
                        const selectedCase = runResult.cases.find(c => c.caseIndex === selectedCaseIndex)!;
                        return (
                          <div className="case-details">
                            {/* Input */}
                            <div className="case-detail-section" style={{ marginBottom: '16px' }}>
                              <strong style={{ display: 'block', marginBottom: '8px', fontSize: '0.875rem', color: '#374151' }}>Input:</strong>
                              <div className="case-input-values" style={{ backgroundColor: '#f9fafb', padding: '12px', borderRadius: '6px', fontFamily: 'monospace', fontSize: '0.875rem' }}>
                                {selectedCase.parameterNames.map((paramName, idx) => (
                                  <div key={idx} className="case-input-line" style={{ marginBottom: idx < selectedCase.parameterNames.length - 1 ? '4px' : '0' }}>
                                    <span className="param-name" style={{ fontWeight: 600 }}>{paramName}</span>
                                    <span className="param-equals"> = </span>
                                    <span className="param-value">{formatValue(selectedCase.inputValues[idx])}</span>
                                  </div>
                                ))}
                              </div>
                            </div>

                            {/* Stdout - only show when there is output */}
                            {selectedCase.stdout && selectedCase.stdout.trim() !== '' && (
                              <div className="case-detail-section" style={{ marginBottom: '16px' }}>
                                <strong style={{ display: 'block', marginBottom: '8px', fontSize: '0.875rem', color: '#374151' }}>Stdout:</strong>
                                <pre className="case-output-pre" style={{ backgroundColor: '#f9fafb', padding: '12px', borderRadius: '6px', fontFamily: 'monospace', fontSize: '0.875rem', whiteSpace: 'pre-wrap', wordBreak: 'break-word', overflowX: 'auto' }}>{selectedCase.stdout}</pre>
                              </div>
                            )}

                            {/* Output */}
                            {selectedCase.output !== undefined && selectedCase.output !== null && (
                              <div className="case-detail-section" style={{ marginBottom: '16px' }}>
                                <strong style={{ display: 'block', marginBottom: '8px', fontSize: '0.875rem', color: '#374151' }}>Output:</strong>
                                <pre className="case-output-pre" style={{ backgroundColor: '#f9fafb', padding: '12px', borderRadius: '6px', fontFamily: 'monospace', fontSize: '0.875rem', whiteSpace: 'pre', wordBreak: 'break-word', overflowX: 'auto' }}>{formatCompactValue(selectedCase.output)}</pre>
                              </div>
                            )}
                            {/* Expected (on new line) */}
                            {selectedCase.expectedOutput && (
                              <div className="case-detail-section" style={{ marginBottom: '16px' }}>
                                <strong style={{ display: 'block', marginBottom: '8px', fontSize: '0.875rem', color: '#374151' }}>Expected:</strong>
                                <pre className="case-output-pre" style={{ backgroundColor: '#f9fafb', padding: '12px', borderRadius: '6px', fontFamily: 'monospace', fontSize: '0.875rem', whiteSpace: 'pre', wordBreak: 'break-word', overflowX: 'auto' }}>{formatCompactValue(selectedCase.expectedOutput)}</pre>
                              </div>
                            )}

                            {/* Exception */}
                            {selectedCase.error && (
                              <div className="case-detail-section error-section" style={{ marginBottom: '16px' }}>
                                <strong style={{ display: 'block', marginBottom: '8px', fontSize: '0.875rem', color: '#dc2626' }}>Runtime Error:</strong>
                                <pre className="case-output-pre error" style={{ backgroundColor: '#fee2e2', color: '#991b1b', padding: '12px', borderRadius: '6px', fontFamily: 'monospace', fontSize: '0.875rem', whiteSpace: 'pre-wrap', wordBreak: 'break-word', overflowX: 'auto' }}>{selectedCase.error.message}</pre>
                                {selectedCase.error.stack && (
                                  <pre className="case-output-pre error stack" style={{ backgroundColor: '#fee2e2', color: '#991b1b', padding: '12px', borderRadius: '6px', fontFamily: 'monospace', fontSize: '0.75rem', whiteSpace: 'pre-wrap', wordBreak: 'break-word', overflowX: 'auto', marginTop: '8px' }}>{selectedCase.error.stack}</pre>
                                )}
                              </div>
                            )}

                          </div>
                        );
                      })()}
                    </>
                  ) : !isExecuting ? null : (
                    <div className="no-results" style={{ textAlign: 'center', padding: '32px', color: '#6b7280' }}>
                      <i className="fas fa-spinner fa-spin" style={{ fontSize: '24px', marginBottom: '8px' }}></i>
                      <p>Running...</p>
                    </div>
                  )}
                  
                  {/* Show overall status when all tests complete (Submit button) */}
                  {executionResult && !runResult && (
                    <>
                      {/* Overall Status Header */}
                      <div className="result-overall-status" style={{ marginBottom: '8px', display: 'flex', alignItems: 'center', gap: '12px', flexWrap: 'wrap' }}>
                        <div className={`status-badge-large ${
                          executionResult.status === 'Accepted' ? 'accepted' : 
                          executionResult.status === 'Error' ? 'error' :
                          'wrong-answer'
                        }`} style={{ 
                          display: 'inline-flex', 
                          alignItems: 'center', 
                          gap: '8px', 
                          padding: '0',
                          borderRadius: '0',
                          fontSize: '1rem',
                          fontWeight: 600,
                          marginBottom: '0',
                          backgroundColor: 'transparent',
                          color: executionResult.status === 'Accepted' ? '#059669' : executionResult.status === 'Error' ? '#dc2626' : '#dc2626',
                          border: 'none'
                        }}>
                          <i className={`fas ${
                            executionResult.status === 'Accepted' ? 'fa-check-circle' : 
                            executionResult.status === 'Error' ? 'fa-exclamation-triangle' :
                            'fa-times-circle'
                          }`}></i>
                          {executionResult.status === 'Accepted' ? 'Accepted' :
                           executionResult.status === 'Error' ? 'Runtime Error' :
                           'Wrong Answer'}
                        </div>
                        {/* Always show test cases count for submit */}
                        {executionResult.output && executionResult.output.trim() !== '' && (
                          <span style={{ 
                            fontSize: '0.875rem', 
                            color: '#6b7280',
                            fontWeight: 400
                          }}>
                            {executionResult.output}
                          </span>
                        )}
                      </div>

                      {/* Show all test cases in tabs (like Run button) */}
                      {testResults.length > 0 && (
                        <>
                          {/* Case Subtabs - Show first 3 and any failed test cases */}
                          <div className="case-subtabs" style={{ display: 'flex', gap: '8px', marginBottom: '16px', flexWrap: 'wrap' }}>
                            {testResults
                              .filter(r => r.testCaseNumber <= 3 || !r.passed)
                              .map((result) => (
                              <button
                                key={result.testCaseNumber}
                                className={`case-subtab ${selectedCaseIndex === result.testCaseNumber ? 'active' : ''} ${
                                  result.passed === true ? 'passed' :
                                  result.error ? 'error' :
                                  result.passed === false ? 'failed' : ''
                                }`}
                                onClick={() => setSelectedCaseIndex(result.testCaseNumber)}
                                style={{
                                  padding: '6px 12px',
                                  borderRadius: '6px',
                                  border: selectedCaseIndex === result.testCaseNumber ? '1px solid #e5e7eb' : 'none',
                                  backgroundColor: result.passed === true ? '#d1fae5' : result.error ? '#fee2e2' : result.passed === false ? '#fee2e2' : '#f3f4f6',
                                  cursor: 'pointer',
                                  fontSize: '0.875rem',
                                  display: 'flex',
                                  alignItems: 'center',
                                  gap: '6px',
                                  color: result.passed === true ? '#065f46' : result.error ? '#991b1b' : result.passed === false ? '#991b1b' : '#6b7280',
                                  transition: 'all 0.2s ease',
                                  opacity: selectedCaseIndex === result.testCaseNumber ? 1 : 0.8
                                }}
                              >
                                <i className={`fas ${
                                  result.passed === true ? 'fa-check-circle' :
                                  result.error ? 'fa-exclamation-triangle' :
                                  'fa-times-circle'
                                }`}></i>
                                Case {result.testCaseNumber}
                              </button>
                            ))}
                          </div>

                          {/* Selected Case Details - Show details for selected case */}
                          {testResults.find(r => r.testCaseNumber === selectedCaseIndex) && (() => {
                            const selectedCase = testResults.find(r => r.testCaseNumber === selectedCaseIndex)!;
                            return (
                              <div className="selected-case-details">
                                {/* Input */}
                                {selectedCase.input && (
                                  <div className="test-result-section">
                                    <strong style={{ display: 'block', marginBottom: '8px', fontSize: '0.875rem', color: '#374151' }}>Input:</strong>
                                    <pre className="case-output-pre" style={{ backgroundColor: '#f9fafb', padding: '12px', borderRadius: '6px', fontFamily: 'monospace', fontSize: '0.875rem', whiteSpace: 'pre-wrap', wordBreak: 'break-word', overflowX: 'auto' }}>{selectedCase.input}</pre>
                                  </div>
                                )}

                                {/* Stdout (console.log/print output) */}
                                {selectedCase.stdout && selectedCase.stdout.trim() !== '' && (
                                  <div className="test-result-section">
                                    <strong style={{ display: 'block', marginBottom: '8px', fontSize: '0.875rem', color: '#374151' }}>Stdout:</strong>
                                    <pre className="case-output-pre" style={{ backgroundColor: '#f9fafb', padding: '12px', borderRadius: '6px', fontFamily: 'monospace', fontSize: '0.875rem', whiteSpace: 'pre-wrap', wordBreak: 'break-word', overflowX: 'auto' }}>{selectedCase.stdout}</pre>
                                  </div>
                                )}

                                {/* Output (actual return value) */}
                                {selectedCase.output !== undefined && selectedCase.output !== null && selectedCase.output !== '' && (
                                  <div className="test-result-section">
                                    <strong style={{ display: 'block', marginBottom: '8px', fontSize: '0.875rem', color: '#374151' }}>Output:</strong>
                                    <pre className="case-output-pre" style={{ backgroundColor: '#f9fafb', padding: '12px', borderRadius: '6px', fontFamily: 'monospace', fontSize: '0.875rem', whiteSpace: 'pre', wordBreak: 'break-word', overflowX: 'auto' }}>{formatCompactValue(selectedCase.output)}</pre>
                                  </div>
                                )}

                                {/* Expected Output (on new line) */}
                                {selectedCase.expectedOutput && (
                                  <div className="test-result-section">
                                    <strong style={{ display: 'block', marginBottom: '8px', fontSize: '0.875rem', color: '#374151' }}>Expected:</strong>
                                    <pre className="case-output-pre" style={{ backgroundColor: '#f9fafb', padding: '12px', borderRadius: '6px', fontFamily: 'monospace', fontSize: '0.875rem', whiteSpace: 'pre', wordBreak: 'break-word', overflowX: 'auto' }}>{formatCompactValue(selectedCase.expectedOutput)}</pre>
                                  </div>
                                )}

                                {/* Error/Runtime Error */}
                                {selectedCase.error && selectedCase.error.trim() !== '' && (
                                  <div className="test-result-section test-result-error">
                                    <strong style={{ display: 'block', marginBottom: '8px', fontSize: '0.875rem', color: '#dc2626' }}>Runtime Error:</strong>
                                    <pre className="case-output-pre" style={{ backgroundColor: '#fee2e2', padding: '12px', borderRadius: '6px', fontFamily: 'monospace', fontSize: '0.875rem', whiteSpace: 'pre-wrap', wordBreak: 'break-word', overflowX: 'auto', color: '#991b1b' }}>{selectedCase.error}</pre>
                                  </div>
                                )}

                                {/* Metrics */}
                                {selectedCase.runtime !== undefined && selectedCase.runtime > 0 && (
                                  <div className="test-result-detail" style={{ marginTop: '1rem', fontSize: '0.875rem', color: '#6b7280' }}>
                                    <span>Runtime: {selectedCase.runtime.toFixed(2)} ms</span>
                                    {selectedCase.memory !== undefined && selectedCase.memory > 0 && (
                                      <span style={{ marginLeft: '16px' }}>Memory: {(selectedCase.memory / 1024).toFixed(2)} MB</span>
                                    )}
                                  </div>
                                )}
                              </div>
                            );
                          })()}
                        </>
                      )}
                    </>
                  )}
                </div>
              )}
              {/* Status bar at bottom of testcase panel - always visible when line is selected */}
              {activeTestTab === 'testcase' && selectedTestLine !== null && parameterNames.length > 0 && (
                <div className="testcase-status-bar">
                  <div className="status-bar-right">
                    <span className="status-cursor">
                      Line {selectedTestLine} • {(() => {
                        // Calculate which parameter this line corresponds to
                        const lineIndex = selectedTestLine - 1;
                        const paramIndex = lineIndex % parameterCount;
                        const paramName = parameterNames[paramIndex] || `Parameter ${paramIndex + 1}`;
                        return paramName;
                      })()}
                    </span>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
    </>
  );
};
