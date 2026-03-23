import React, { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import api from '../../services/api';
import { tokenStorage } from '../../utils/tokenStorage';
import { useAuth } from '../../hooks/useAuth';
import { peerInterviewService, type LiveInterviewSession, type ScheduledInterviewSession, type UserDto } from '../../services/peerInterview.service';
import { questionService, type InterviewQuestion, type QuestionList } from '../../services/question.service';
import { VideoChat, type VideoChatHandle, type VideoChatState } from '../../components/VideoChat';
import { FeedbackForm } from '../../components/FeedbackForm';
import { ROUTES } from '../../utils/constants';
import '../../styles/peer-noncoding-session.css';

const PeerInterviewSessionPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [liveSession, setLiveSession] = useState<LiveInterviewSession | null>(null);
  const [scheduledSession, setScheduledSession] = useState<ScheduledInterviewSession | null>(null);

  const [timeRemainingSeconds, setTimeRemainingSeconds] = useState<number | null>(null);
  const [isEnding, setIsEnding] = useState(false);
  const [videoError, setVideoError] = useState<string | null>(null);

  const [questionList, setQuestionList] = useState<QuestionList[]>([]);
  const [selectedQuestionId, setSelectedQuestionId] = useState<string | null>(null);
  const [questionCache, setQuestionCache] = useState<Record<string, InterviewQuestion>>({});
  const [isLoadingQuestion, setIsLoadingQuestion] = useState(false);

  const [isInstructionsOpen, setIsInstructionsOpen] = useState(true);
  const [isInstructionsExpanded, setIsInstructionsExpanded] = useState(false);
  const [isTroubleshootingExpanded, setIsTroubleshootingExpanded] = useState(false);

  const [isPickerOpen, setIsPickerOpen] = useState(false);
  const [pickerSearch, setPickerSearch] = useState('');
  const [pickerRole, setPickerRole] = useState<string>('');
  const [pickerCompany, setPickerCompany] = useState<string>('');
  const [pickerCategory, setPickerCategory] = useState<string>('');

  const [isChatOpen, setIsChatOpen] = useState(false);
  const [chatInput, setChatInput] = useState('');
  const [chatMessages, setChatMessages] = useState<Array<{ userId: string; message: string; timestamp: string }>>([]);

  const [videoState, setVideoState] = useState<VideoChatState>(() => ({
    isVideoEnabled: true,
    isAudioEnabled: true,
    hasRemoteStream: false,
  }));
  const videoChatRef = useRef<VideoChatHandle | null>(null);

  const [showFeedback, setShowFeedback] = useState(false);
  const signalRRef = useRef<signalR.HubConnection | null>(null);

  const urlInterviewType = useMemo(() => {
    try {
      const params = new URLSearchParams(window.location.search);
      const type = params.get('type') || '';
      return type.trim().toLowerCase();
    } catch {
      return '';
    }
  }, []);

  const interviewType = (scheduledSession?.interviewType || liveSession?.interviewType || urlInterviewType || '').trim().toLowerCase();
  const isNonCodingInterview = interviewType === 'behavioral' || interviewType === 'product-management';

  const partner: UserDto | undefined = useMemo(() => {
    if (!liveSession?.participants || !user?.id) return undefined;
    const other = liveSession.participants.find((p) => p.userId !== user.id);
    return other?.user;
  }, [liveSession?.participants, user?.id]);

  // Determine if both users joined (for feedback form logic)
  const hasBothUsers = useMemo(() => {
    if (!liveSession) return false;
    const participants = liveSession.participants || [];
    return participants.length >= 2;
  }, [liveSession]);

  const formatTime = (seconds: number | null): string => {
    if (seconds === null) return '--:--';
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  // Load session (live or scheduled)
  useEffect(() => {
    if (!id) return;
    let cancelled = false;

    const timer = setTimeout(() => {
      setLoading(true);
      setError(null);

      api.get(`/peer-interviews/sessions/${id}`)
        .then(async (resp) => {
          if (cancelled) return;
          const data: any = resp.data;

          if (data && Array.isArray(data.participants)) {
            setLiveSession(data as LiveInterviewSession);
            if (data.scheduledSessionId) {
              try {
                const scheduled = await peerInterviewService.getScheduledSession(data.scheduledSessionId);
                if (!cancelled) setScheduledSession(scheduled);
              } catch {
                // Not all participants can access scheduled session details; type is passed via URL.
              }
            }
            return;
          }

          // scheduled session dto
          setScheduledSession(data as ScheduledInterviewSession);
          if (data.liveSessionId) {
            const live = await api.get(`/peer-interviews/sessions/${data.liveSessionId}`);
            if (!cancelled) setLiveSession(live.data as LiveInterviewSession);
          }
        })
        .catch(async (e) => {
          if (cancelled) return;

          // If user opened a "practice with a friend" invite link, the backend will initially reject
          // until we join the session. Attempt an auto-join then retry loading once.
          const status = e?.response?.status;
          if ((status === 401 || status === 403) && id) {
            try {
              await peerInterviewService.joinFriendInterview(id);
              const retry = await api.get(`/peer-interviews/sessions/${id}`);
              if (cancelled) return;
              const data: any = retry.data;
              if (data && Array.isArray(data.participants)) {
                setLiveSession(data as LiveInterviewSession);
                if (data.scheduledSessionId) {
                  try {
                    const scheduled = await peerInterviewService.getScheduledSession(data.scheduledSessionId);
                    if (!cancelled) setScheduledSession(scheduled);
                  } catch {}
                }
                return;
              }
              setScheduledSession(data as ScheduledInterviewSession);
              if (data.liveSessionId) {
                const live = await api.get(`/peer-interviews/sessions/${data.liveSessionId}`);
                if (!cancelled) setLiveSession(live.data as LiveInterviewSession);
              }
              return;
            } catch {
              // fall through to show error
            }
          }

          setError(e?.response?.data?.message || 'Failed to load session');
        })
        .finally(() => {
          if (cancelled) return;
          setLoading(false);
        });
    }, 0);

    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [id]);

  // (Non-coding interview UI is fixed-position; no draggable carousel state)

  // Synced timer state
  const [syncedStartedAtMs, setSyncedStartedAtMs] = useState<number | null>(null);

  // Initialize synced time when live session loads
  useEffect(() => {
    if (liveSession?.startedAt && !syncedStartedAtMs) {
      setSyncedStartedAtMs(new Date(liveSession.startedAt).getTime());
    }
  }, [liveSession?.startedAt, syncedStartedAtMs]);

  // SignalR: listen for InterviewEnded and ChatMessage
  useEffect(() => {
    if (!id) return;
    const baseUrl = (api.defaults.baseURL && typeof api.defaults.baseURL === 'string')
      ? api.defaults.baseURL.replace('/api', '')
      : 'http://localhost:5000';

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/api/collaboration`, {
        transport: signalR.HttpTransportType.WebSockets,
        accessTokenFactory: () => tokenStorage.getAccessToken() || '',
      })
      .withAutomaticReconnect()
      .build();

    signalRRef.current = connection;

    const start = async () => {
      try {
        await connection.start();
        await connection.invoke('JoinSession', id);
        connection.on('InterviewEnded', () => {
          setShowFeedback(true);
        });
        
        // Listen for timer synchronization
        connection.on('TimerSynced', (serverElapsedTime: number) => {
          if (serverElapsedTime > 0) {
            setSyncedStartedAtMs(Date.now() - serverElapsedTime * 1000);
          }
        });

        connection.on('ChatMessage', (payload: any) => {
          const msg = String(payload?.message || '').trim();
          const senderId = String(payload?.userId || '');
          const ts = String(payload?.timestamp || new Date().toISOString());
          if (!msg || !senderId) return;
          setChatMessages((prev) => [...prev, { userId: senderId, message: msg, timestamp: ts }]);
        });
      } catch {
        // ignore
      }
    };

    start();

    return () => {
      connection.stop().catch(() => {});
      signalRRef.current = null;
    };
  }, [id]);

  // Synced timer: based on syncedStartedAtMs
  useEffect(() => {
    if (!syncedStartedAtMs) {
      setTimeRemainingSeconds(null);
      return;
    }

    const durationSeconds = 60 * 60; // 60 minutes

    const update = () => {
      const elapsed = Math.floor((Date.now() - syncedStartedAtMs) / 1000);
      const remaining = Math.max(0, durationSeconds - elapsed);
      setTimeRemainingSeconds(remaining);

      // Periodically broadcast timer if I am the interviewer (or first user)
      if (signalRRef.current?.state === 'Connected' && id) {
        const myParticipant = liveSession?.participants?.find(p => p.userId === user?.id);
        const isInterviewer = myParticipant?.role === 'Interviewer';
        const isOnlyUserSoFar = (liveSession?.participants?.length || 0) < 2;
        if ((isInterviewer || isOnlyUserSoFar) && elapsed > 0 && elapsed % 5 === 0) {
           signalRRef.current.invoke('SendTimerSync', id, elapsed).catch(() => {});
        }
      }
    };

    update();
    const interval = setInterval(update, 1000);
    return () => clearInterval(interval);
  }, [syncedStartedAtMs, id, liveSession?.participants, user?.id]);

  // Load question list for picker (non-blocking, debounced)
  useEffect(() => {
    if (!isNonCodingInterview) return;
    if (!interviewType) return;

    const mappedQuestionType = interviewType === 'behavioral' ? 'Behavioral' : 'Product Management';
    const timer = setTimeout(() => {
      questionService.getQuestions({
        questionType: mappedQuestionType,
        search: pickerSearch || undefined,
        role: pickerRole || undefined,
        companies: pickerCompany ? [pickerCompany] : undefined,
        category: pickerCategory || undefined,
        page: 1,
        pageSize: 100,
      })
        .then((list) => {
          setQuestionList(list);
          if (list.length === 0) {
            setSelectedQuestionId(null);
            return;
          }
          if (!selectedQuestionId || !list.some((q) => q.id === selectedQuestionId)) {
            // Select a random question from the list instead of always the first one
            const randomIndex = Math.floor(Math.random() * list.length);
            setSelectedQuestionId(list[randomIndex].id);
          }
        })
        .catch(() => setQuestionList([]));
    }, 250);
    return () => clearTimeout(timer);
  }, [isNonCodingInterview, interviewType, pickerSearch, pickerRole, pickerCompany, pickerCategory, selectedQuestionId]);

  // Load active question detail (cache) (non-blocking)
  useEffect(() => {
    if (!selectedQuestionId) return;
    if (questionCache[selectedQuestionId]) return;

    setIsLoadingQuestion(true);
    questionService.getQuestionById(selectedQuestionId)
      .then((q) => {
        setQuestionCache((prev) => ({ ...prev, [selectedQuestionId]: q }));
      })
      .catch(() => {})
      .finally(() => setIsLoadingQuestion(false));
  }, [questionCache, selectedQuestionId]);

  const handleFinishInterview = async () => {
    if (!id) return;
    if (isEnding) return;

    // Show confirmation modal before ending session
    if (!window.confirm('Are you sure you want to end this interview session?')) {
      return;
    }

    setIsEnding(true);
    try {
      // First, reload session to get latest participant data
      const response = await api.get(`/peer-interviews/sessions/${id}`);
      const latestSession: any = response.data;
      if (latestSession && Array.isArray(latestSession.participants)) {
        setLiveSession(latestSession as LiveInterviewSession);
      }
      
      await peerInterviewService.endInterview(id);
      // Always show feedback after ending, even if session data changes
      setShowFeedback(true);
    } catch (e: any) {
      console.error('Error ending interview:', e);
      setError(e?.response?.data?.message || 'Failed to finish interview');
    } finally {
      setIsEnding(false);
    }
  };

  const handleSelectQuestion = (questionId: string) => {
    setSelectedQuestionId(questionId);
    setIsPickerOpen(false);
  };

  const handleSendChat = () => {
    if (!id) return;
    const msg = chatInput.trim();
    if (!msg) return;
    if (!user?.id) return;

    setChatInput('');
    signalRRef.current?.invoke('SendChatMessage', id, msg).catch(() => {});
  };

  const roleOptions = useMemo(() => ([
    'Software Engineer',
    'Data Engineer',
    'Data Scientist',
    'Machine Learning Engineer',
    'Engineering Manager',
    'Technical Program Manager',
    'Product Manager',
  ]), []);

  const companyOptions = useMemo(() => {
    const set = new Set<string>();
    questionList.forEach((q) => (q.companyTags || []).forEach((c) => set.add(c)));
    return Array.from(set).sort((a, b) => a.localeCompare(b));
  }, [questionList]);

  const categoryOptions = useMemo(() => {
    const set = new Set<string>();
    questionList.forEach((q) => {
      if (q.category) set.add(q.category);
    });
    return Array.from(set).sort((a, b) => a.localeCompare(b));
  }, [questionList]);

  if (loading) {
    return (
      <div className="peer-noncoding-session">
        <div className="peer-noncoding-pill" style={{ position: 'absolute', top: 16, left: 16 }}>Loading session…</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="peer-noncoding-session">
        <div className="peer-noncoding-pill" style={{ position: 'absolute', top: 16, left: 16 }}>{error}</div>
        <button className="peer-noncoding-btn" style={{ position: 'absolute', top: 64, left: 16 }} onClick={() => navigate(ROUTES.FIND_PEER)}>
          Back
        </button>
      </div>
    );
  }

  if (!isNonCodingInterview) {
    return (
      <div className="peer-noncoding-session">
        <div className="peer-noncoding-pill" style={{ position: 'absolute', top: 16, left: 16 }}>
          This session page is only for Product Management and Behavioral interviews.
        </div>
        <button className="peer-noncoding-btn" style={{ position: 'absolute', top: 64, left: 16 }} onClick={() => navigate(ROUTES.FIND_PEER)}>
          Back
        </button>
      </div>
    );
  }

  const activeQuestion = selectedQuestionId ? questionCache[selectedQuestionId] : undefined;
  const activeQuestionText = (activeQuestion?.description || activeQuestion?.title || '').trim();

  return (
    <div className="peer-noncoding-session">
      <div className="peer-noncoding-brand" aria-label="Vector">
        Vector
      </div>

      {videoError ? (
        <div className="peer-noncoding-pill peer-noncoding-pill-top" role="status" aria-live="polite">
          <i className="fa-solid fa-triangle-exclamation" aria-hidden="true"></i>
          {videoError}
        </div>
      ) : null}

      <div className="peer-noncoding-video">
        <VideoChat
          ref={videoChatRef}
          sessionId={id || ''}
          userId={user?.id || ''}
          peerUserId={partner?.id}
          onError={(msg) => setVideoError(msg)}
          showLocalVideo={false}
          showLocalPreview={true}
          overlayControls={false}
          hideControls={true}
          onStateChange={(s) => setVideoState(s)}
        />
      </div>

      {/* Bottom-center controls */}
      <div className="peer-noncoding-controls" aria-label="Call controls">
        <button
          type="button"
          className={`peer-noncoding-control-btn ${videoState.isAudioEnabled ? '' : 'danger'}`}
          aria-label={videoState.isAudioEnabled ? 'Mute microphone' : 'Unmute microphone'}
          onClick={() => videoChatRef.current?.toggleAudio().catch(() => {})}
        >
          <i className={`fa-solid ${videoState.isAudioEnabled ? 'fa-microphone' : 'fa-microphone-slash'}`} aria-hidden="true"></i>
        </button>
        <button
          type="button"
          className={`peer-noncoding-control-btn ${videoState.isVideoEnabled ? '' : 'danger'}`}
          aria-label={videoState.isVideoEnabled ? 'Turn off camera' : 'Turn on camera'}
          onClick={() => videoChatRef.current?.toggleVideo().catch(() => {})}
        >
          <i className={`fa-solid ${videoState.isVideoEnabled ? 'fa-video' : 'fa-video-slash'}`} aria-hidden="true"></i>
        </button>
        <button type="button" className="peer-noncoding-control-btn end" onClick={handleFinishInterview} disabled={isEnding} aria-label="End session">
          {isEnding ? 'Ending…' : 'End session'}
        </button>
      </div>

      {/* Bottom-left question + instructions */}
      <div className="peer-noncoding-left">
        {isInstructionsOpen ? (
          <div className="peer-noncoding-left-card" aria-label="Suggested question">
            <div className="left-card-title">
              <i className="fa-solid fa-wand-magic-sparkles" aria-hidden="true"></i>
              Suggested question to ask
            </div>
            <div className="left-card-question">
              {isLoadingQuestion && !activeQuestion ? 'Loading question…' : (activeQuestionText || 'No question selected yet.')}
            </div>
            {selectedQuestionId ? (
              <a className="left-card-link" href={`${ROUTES.QUESTIONS}/${selectedQuestionId}`} target="_blank" rel="noreferrer">
                View full question →
              </a>
            ) : null}
            <button type="button" className="left-card-primary" onClick={() => setIsPickerOpen(true)}>
              Try a different question
            </button>

            <button
              type="button"
              className="left-card-accordion"
              onClick={() => setIsInstructionsExpanded((v) => !v)}
              aria-expanded={isInstructionsExpanded}
            >
              Instructions
              <i className={`fa-solid ${isInstructionsExpanded ? 'fa-chevron-down' : 'fa-chevron-right'}`} aria-hidden="true"></i>
            </button>
            {isInstructionsExpanded ? (
              <div className="left-card-accordion-body">
                <ul>
                  <li>Plan for ~60 minutes total and swap roles halfway through.</li>
                  <li>Spend 2–3 minutes upfront aligning on the role/level and what “good” looks like.</li>
                  <li>Ask at least one follow-up to test depth (trade-offs, metrics, edge cases).</li>
                  <li>Reserve the last 8–10 minutes for feedback and one actionable improvement each.</li>
                </ul>
              </div>
            ) : null}

            <button
              type="button"
              className="left-card-accordion"
              onClick={() => setIsTroubleshootingExpanded((v) => !v)}
              aria-expanded={isTroubleshootingExpanded}
            >
              Troubleshooting
              <i className={`fa-solid ${isTroubleshootingExpanded ? 'fa-chevron-down' : 'fa-chevron-right'}`} aria-hidden="true"></i>
            </button>
            {isTroubleshootingExpanded ? (
              <div className="left-card-accordion-body">
                <ul>
                  <li>Close Zoom/Meet/Teams (and extra browser tabs) that could be holding your mic/camera, then refresh.</li>
                  <li>Check your browser site permissions and re-allow camera + microphone for Vector.</li>
                  <li>If audio/video is stuck, refresh the page (don’t click &quot;End session&quot;) or reopen the same URL.</li>
                  <li>Try toggling mic/camera off then on; ask your partner to do the same.</li>
                  <li>If the connection is choppy, switch to wired internet or move closer to your router.</li>
                  <li>Still blocked? Email <a href="mailto:practice@vecotr.com">practice@vecotr.com</a> and include your browser + OS.</li>
                </ul>
              </div>
            ) : null}
          </div>
        ) : null}

        <div className="peer-noncoding-left-bar">
          <button type="button" className="left-bar-btn" onClick={() => setIsInstructionsOpen((v) => !v)}>
            {isInstructionsOpen ? 'Hide instructions' : 'Show instructions'}
          </button>
          <span className="left-bar-status" aria-label="Time remaining">
            <i className="fa-regular fa-clock" aria-hidden="true"></i>
            {formatTime(timeRemainingSeconds)}
          </span>
          <span className={`left-bar-status ${videoState.hasRemoteStream ? 'ok' : ''}`} aria-label="Partner status">
            {videoState.hasRemoteStream ? 'Connected' : 'Waiting'}
          </span>
        </div>
      </div>

      {/* Left slide-in question picker */}
      {isPickerOpen ? (
        <div className="peer-noncoding-drawer-overlay" role="dialog" aria-label="Pick a question" onClick={() => setIsPickerOpen(false)}>
          <div className="peer-noncoding-drawer" onClick={(e) => e.stopPropagation()}>
            <div className="drawer-header">
              <div>
                <div className="drawer-title">Select a question to ask</div>
                <div className="drawer-subtitle">Search or browse and select a question.</div>
              </div>
              <button type="button" className="drawer-close" aria-label="Close" onClick={() => setIsPickerOpen(false)}>
                <i className="fa-solid fa-xmark" aria-hidden="true"></i>
              </button>
            </div>
            <div className="drawer-search">
              <i className="fa-solid fa-magnifying-glass" aria-hidden="true"></i>
              <input
                value={pickerSearch}
                onChange={(e) => setPickerSearch(e.target.value)}
                placeholder="Search for questions…"
                aria-label="Search questions"
              />
            </div>
            <div className="drawer-filters" aria-label="Filters">
              {pickerRole ? (
                <button type="button" className="drawer-chip removable" onClick={() => setPickerRole('')} aria-label="Clear role filter">
                  {pickerRole}
                  <i className="fa-solid fa-xmark" aria-hidden="true"></i>
                </button>
              ) : null}
              <select
                className="drawer-select"
                value={pickerRole}
                onChange={(e) => setPickerRole(e.target.value)}
                aria-label="Role"
              >
                <option value="">Role</option>
                {roleOptions.map((r) => (
                  <option key={r} value={r}>{r}</option>
                ))}
              </select>

              <select
                className="drawer-select"
                value={pickerCompany}
                onChange={(e) => setPickerCompany(e.target.value)}
                aria-label="Company"
              >
                <option value="">Company</option>
                {companyOptions.map((c) => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </select>

              <select
                className="drawer-select"
                value={pickerCategory}
                onChange={(e) => setPickerCategory(e.target.value)}
                aria-label="Category"
              >
                <option value="">Category</option>
                {categoryOptions.map((c) => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </select>
            </div>
            <div className="drawer-chips">
              <span className="drawer-chip">{interviewType === 'behavioral' ? 'Behavioral' : 'Product Management'}</span>
            </div>
            <div className="drawer-list" aria-label="Questions list">
              {questionList.map((q) => (
                <div key={q.id} className="drawer-item">
                  <div className="drawer-item-main">
                    <div className="drawer-item-title">{q.title}</div>
                    <a className="drawer-item-link" href={`${ROUTES.QUESTIONS}/${q.id}`} target="_blank" rel="noreferrer">
                      View full question →
                    </a>
                  </div>
                  <button type="button" className="drawer-item-select" onClick={() => handleSelectQuestion(q.id)}>
                    Select
                  </button>
                </div>
              ))}
              {questionList.length === 0 ? <div className="drawer-empty">No questions found.</div> : null}
            </div>
          </div>
        </div>
      ) : null}

      {/* Bottom-right chat */}
      {isChatOpen ? (
        <div className="peer-noncoding-chat" aria-label="Chat">
          <div className="chat-header">
            <div className="chat-title">Chat</div>
            <button type="button" className="chat-toggle" onClick={() => setIsChatOpen(false)} aria-label="Hide chat">
              Hide chat
            </button>
          </div>
          <div className="chat-messages" aria-label="Messages">
            {chatMessages.map((m, idx) => (
              <div key={`${m.timestamp}-${idx}`} className={`chat-msg ${m.userId === user?.id ? 'me' : 'them'}`}>
                {m.message}
              </div>
            ))}
          </div>
          <div className="chat-input">
            <input
              value={chatInput}
              onChange={(e) => setChatInput(e.target.value)}
              placeholder="Send a message"
              aria-label="Message"
              onKeyDown={(e) => {
                if (e.key === 'Enter') handleSendChat();
              }}
            />
            <button type="button" onClick={handleSendChat}>
              Send
            </button>
          </div>
        </div>
      ) : (
        <button type="button" className="peer-noncoding-chat-fab" onClick={() => setIsChatOpen(true)} aria-label="Show chat">
          Show chat
        </button>
      )}

      {showFeedback ? (
        <div className="peer-noncoding-modal">
          <div className="peer-noncoding-modal-card">
            {hasBothUsers && liveSession && partner?.id ? (
              <FeedbackForm
                liveSessionId={liveSession.id}
                opponentId={partner.id}
                opponentName={partner.firstName ? `${partner.firstName}${partner.lastName ? ` ${partner.lastName}` : ''}` : partner.email}
                interviewType={interviewType === 'behavioral' ? 'Behavioral' : 'Product Management'}
                date={liveSession.startedAt || liveSession.createdAt}
                onComplete={() => {
                  setShowFeedback(false);
                  window.location.assign(ROUTES.FIND_PEER);
                }}
                onCancel={() => {
                  setShowFeedback(false);
                  window.location.assign(ROUTES.FIND_PEER);
                }}
              />
            ) : (
              <div className="peer-noncoding-modal-empty">
                <div className="empty-feedback-icon">
                  <i className="fas fa-info-circle"></i>
                </div>
                <h3>Interview Ended</h3>
                <p>Your practice partner didn't join this session, so there's no feedback to provide.</p>
                <button 
                  className="empty-feedback-close-btn"
                  onClick={() => {
                    setShowFeedback(false);
                    window.location.assign(ROUTES.FIND_PEER);
                  }}
                >
                  Continue
                </button>
              </div>
            )}
          </div>
        </div>
      ) : null}
    </div>
  );
};

export default PeerInterviewSessionPage;

