import { useEffect, useState, useRef, useCallback } from 'react';
import { useParams, Link, useSearchParams, useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { questionService } from '../../services/question.service';
import type { InterviewQuestion, QuestionTestCase, QuestionSolution } from '../../services/question.service';
import { codeExecutionService, type RunResult } from '../../services/codeExecution.service';
import { solutionService } from '../../services/solution.service';
import { ROUTES } from '../../utils/constants';
import { CodeEditor } from '../../components/CodeEditor';
import { CollaborativeCodeEditor } from '../../components/CollaborativeCodeEditor';
import { DraggableVideo } from '../../components/DraggableVideo';
import { useAuth } from '../../hooks/useAuth';
import { getQuestionTemplate } from '../../utils/questionTemplates';
import { ToastContainer } from '../../components/Toast';
import { codeDraftService } from '../../services/codeDraft.service';
import { peerInterviewService } from '../../services/peerInterview.service';
import type { PeerInterviewSession } from '../../services/peerInterview.service';
import api from '../../services/api';
import { RejoinModal } from '../../components/RejoinModal';
import { FeedbackForm } from '../../components/FeedbackForm';
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
  const [selectedLanguage, setSelectedLanguage] = useState('javascript');
  const [code, setCode] = useState('');
  const [isExecuting, setIsExecuting] = useState(false);
  const [activeTestTab, setActiveTestTab] = useState<'testcase' | 'result'>('testcase');
  const [testCaseText, setTestCaseText] = useState('');
  const [validationError, setValidationError] = useState<{ type: string; message: string; lineNumber?: number } | null>(null);
  const [runResult, setRunResult] = useState<RunResult | null>(null);
  const [errorMarkers, setErrorMarkers] = useState<Array<{ line: number; column?: number; endColumn?: number; message: string }>>([]);
  const [selectedCaseIndex, setSelectedCaseIndex] = useState<number>(1);
  const [parameterNames, setParameterNames] = useState<string[]>([]);
  const [parameterCount, setParameterCount] = useState<number>(2); // Default to 2 for twoSum
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

  const showToast = useCallback((message: string, type: 'success' | 'error' | 'info' | 'warning') => {
    const id = Date.now().toString();
    setToasts((prev) => [...prev, { id, message, type }]);
  }, []);

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((toast) => toast.id !== id));
  }, []);
  const editorPanelRef = useRef<HTMLDivElement>(null);
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
        const accessToken = localStorage.getItem('accessToken');
        if (!accessToken) return;

        const baseUrl = (api.defaults.baseURL?.replace('/api', '') || 'http://localhost:5000');
        const connection = new signalR.HubConnectionBuilder()
          .withUrl(`${baseUrl}/api/collaboration?access_token=${accessToken}`, {
            transport: signalR.HttpTransportType.WebSockets,
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
          if (session.status === 'InProgress' && session.intervieweeId) {
            // Get session start time from localStorage or use current time
            const storedStartTime = localStorage.getItem(`session_start_${sessionIdFromUrl}`);
            if (storedStartTime) {
              setSessionStartTime(new Date(storedStartTime));
            } else {
              // Timer starts when both users join - set start time now
              const startTime = new Date();
              setSessionStartTime(startTime);
              localStorage.setItem(`session_start_${sessionIdFromUrl}`, startTime.toISOString());
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
  useEffect(() => {
    const timeoutId = setTimeout(() => {
      const params = extractParameterNames(code, selectedLanguage);
      setParameterNames(params);
      setParameterCount(params.length > 0 ? params.length : 2);
    }, 300); // Debounce parameter extraction

    return () => clearTimeout(timeoutId);
  }, [code, selectedLanguage]); // Only extract when code or language actually changes

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
    const lang = language.toLowerCase();
    
    if (lang === 'javascript' || lang === 'js' || lang === 'nodejs') {
      // Match: function name(params) or var name = function(params) or const name = (params) =>
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
      // Match: def function_name(params):
      const match = code.match(/def\s+\w+\s*\(([^)]*)\)/);
      if (match && match[1]) {
        return match[1].split(',').map(p => p.trim().split('=')[0].trim()).filter(p => p.length > 0);
      }
    }
    
    // Default fallback
    return ['nums', 'target']; // For twoSum
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

    // Check if divisible by parameter count
    if (lines.length % parameterCount !== 0) {
      const incompleteCaseStartLine = ((lines.length / parameterCount) | 0) * parameterCount + 1;
      return {
        valid: false,
        error: {
          type: 'INCOMPLETE_CASE',
          message: `Expected ${parameterCount} lines per testcase. Found incomplete testcase at end (starting at line ${incompleteCaseStartLine}).`,
          lineNumber: incompleteCaseStartLine
        }
      };
    }

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
    if (testCases.length > 0 && !testCaseText) {
      // Convert existing test cases to line-based format
      const visibleCases = testCases.filter(tc => !tc.isHidden);
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
      
      setTestCaseText(lines.join('\n'));
    }
  }, [testCases]);


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
    if (!sessionStartTime || !activeSession?.intervieweeId) {
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
  }, [sessionStartTime, activeSession?.intervieweeId]);

  const loadQuestion = async () => {
    if (!id) return;
    try {
      setLoading(true);
      const [questionData, testCasesData, solutionsData] = await Promise.all([
        questionService.getQuestionById(id),
        questionService.getTestCases(id, false),
        questionService.getSolutions(id),
      ]);
      setQuestion(questionData);
      setTestCases(testCasesData);
      setSolutions(solutionsData);
      
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

  // Only show coding question layout for coding questions
  if (question.questionType?.toLowerCase() !== 'coding') {
    // For non-coding questions, show a simpler layout
    return (
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
        </nav>
        <div className="container" style={{ paddingTop: '70px' }}>
          <h1>{question.title}</h1>
          <div dangerouslySetInnerHTML={{ __html: question.description.replace(/\n/g, '<br />') }} />
        </div>
      </div>
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
          {activeSession && activeSession.interviewerId && activeSession.intervieweeId && (
            <>
              {/* Show current role */}
              <div className="role-indicator" style={{ 
                display: 'flex', 
                alignItems: 'center', 
                gap: '0.5rem',
                padding: '0.5rem 1rem',
                background: activeSession.interviewerId === user?.id ? '#7c3aed' : '#6b7280',
                color: 'white',
                borderRadius: '6px',
                fontSize: '0.875rem',
                fontWeight: 500
              }}>
                <i className={`fas ${activeSession.interviewerId === user?.id ? 'fa-user-tie' : 'fa-user'}`}></i>
                {activeSession.interviewerId === user?.id ? 'Interviewer' : 'Interviewee'}
              </div>
              {/* Change Question button - only for interviewer */}
              {activeSession.interviewerId === user?.id && (
                <button
                  className="nav-icon-btn"
                  title="Change Question"
                  onClick={handleChangeQuestion}
                  disabled={isChangingQuestion}
                  style={{ 
                    background: isChangingQuestion ? '#9ca3af' : '#7c3aed',
                    color: 'white',
                    border: 'none',
                    display: 'flex',
                    alignItems: 'center',
                    gap: '0.5rem',
                    padding: '0.5rem 1rem'
                  }}
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
                className="nav-icon-btn"
                title="Switch Role"
                onClick={handleSwitchRole}
                disabled={isSwitchingRole}
                style={{ 
                  background: isSwitchingRole ? '#9ca3af' : '#10b981',
                  color: 'white',
                  border: 'none',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '0.5rem',
                  padding: '0.5rem 1rem'
                }}
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
                className="nav-icon-btn"
                title="End Session"
                onClick={handleEndSession}
                disabled={isEndingSession}
                style={{ 
                  background: isEndingSession ? '#9ca3af' : '#ef4444',
                  color: 'white',
                  border: 'none',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '0.5rem',
                  padding: '0.5rem 1rem'
                }}
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
                <div className="nav-timer" style={{ 
                  display: 'flex', 
                  alignItems: 'center', 
                  gap: '0.5rem',
                  color: '#6b7280',
                  fontSize: '0.875rem',
                  fontWeight: '500'
                }}>
                  <i className="fas fa-clock"></i>
                  <span>{formatTime(elapsedTime)}</span>
                </div>
              )}
            </>
          )}
          <button className="nav-icon-btn" title="Settings">
            <i className="fas fa-cog"></i>
          </button>
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
          interviewType={activeSession.interviewType}
          date={activeSession.scheduledTime}
          onComplete={handleFeedbackComplete}
          onCancel={handleFeedbackComplete}
        />
      )}

      {showPartnerVideo && activeSession && activeSession.interviewerId && activeSession.intervieweeId && (
        <DraggableVideo
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
          onClose={() => setShowPartnerVideo(false)}
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
            <div className="panel-content">
              <div className="question-header">
                <div className="question-title-row">
                  <h1 className="question-title">{question.title}</h1>
                  {isSolved && (
                    <div className="question-solved-status">
                      <i className="fas fa-check-circle"></i>
                      <span>Solved</span>
                    </div>
                  )}
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
                            <button key={idx} className="stat-tag">{company}</button>
                          ))}
                        </div>
                      )}
                    </div>
                    <i className={`fas fa-chevron-${companiesExpanded ? 'up' : 'down'} collapse-icon`}></i>
                  </div>
                )}
              </div>
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
                  <option value="javascript">JavaScript</option>
                  <option value="python">Python3</option>
                  <option value="java">Java</option>
                  <option value="cpp">C++</option>
                  <option value="csharp">C#</option>
                  <option value="go">Go</option>
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

                      {/* Case Subtabs */}
                      <div className="case-subtabs" style={{ display: 'flex', gap: '8px', marginBottom: '16px' }}>
                        {runResult.cases.map((caseResult) => (
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

                            {/* Stdout (always show section, even if empty) */}
                            <div className="case-detail-section" style={{ marginBottom: '16px' }}>
                              <strong style={{ display: 'block', marginBottom: '8px', fontSize: '0.875rem', color: '#374151' }}>Stdout:</strong>
                              {selectedCase.stdout && selectedCase.stdout.trim() !== '' ? (
                                <pre className="case-output-pre" style={{ backgroundColor: '#f9fafb', padding: '12px', borderRadius: '6px', fontFamily: 'monospace', fontSize: '0.875rem', whiteSpace: 'pre-wrap', wordBreak: 'break-word', overflowX: 'auto' }}>{selectedCase.stdout}</pre>
                              ) : (
                                <pre className="case-output-pre" style={{ backgroundColor: '#f9fafb', padding: '12px', borderRadius: '6px', fontFamily: 'monospace', fontSize: '0.875rem', color: '#9ca3af', fontStyle: 'italic' }}>(no stdout)</pre>
                              )}
                            </div>

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
                          {/* Case Subtabs - Show all test cases */}
                          <div className="case-subtabs" style={{ display: 'flex', gap: '8px', marginBottom: '16px', flexWrap: 'wrap' }}>
                            {testResults.map((result) => (
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
