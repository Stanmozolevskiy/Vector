import React, { useState, useEffect, useRef } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { Navbar } from '../../components/layout/Navbar';
import { peerInterviewService } from '../../services/peerInterview.service';
import type { PeerInterviewSession } from '../../services/peerInterview.service';
import { FeedbackView, type FeedbackData } from '../../components/FeedbackView';
import { InterviewSurvey } from '../../components/InterviewSurvey';
import { ROUTES } from '../../utils/constants';
import '../../styles/find-peer.css';

interface ScheduleModalData {
  step: number;
  interviewType?: string;
  practiceType?: string;
  interviewLevel?: string;
  selectedTime?: Date;
}

const INTERVIEW_TYPES = [
  { id: 'data-structures-algorithms', name: 'Data Structures & Algorithms', icon: '</>', description: 'Practice coding questions.' },
  { id: 'system-design', name: 'System Design', icon: '⚙️', description: 'Practice designing technical architectures.' },
  { id: 'behavioral', name: 'Behavioral', icon: '💬', description: 'Practice questions about your work experiences.' },
  { id: 'product-management', name: 'Product Management', icon: '👥', description: 'Practice product sense, estimation, and more.' },
  { id: 'sql', name: 'SQL (Beta)', icon: '🗄️', description: 'Practice writing and optimizing SQL queries.', beta: true },
  { id: 'data-science-ml', name: 'Data Science & ML (Beta)', icon: '📊', description: 'Practice using data to answer questions and design systems.', beta: true },
];

const PRACTICE_TYPES = [
  { id: 'peers', name: 'Practice with peers', icon: '👥', description: 'Free mock interviews with other users where you take turns asking questions.' },
  { id: 'friend', name: 'Practice with a friend', icon: '👤', description: 'Invite a friend and practice on your own schedule at any time.' },
  { id: 'expert', name: 'Expert mock interview', icon: '✓', description: 'Get interviewed 1-1 by an expert coach with FAANG+ experience.', external: true },
];

const INTERVIEW_LEVELS = [
  { id: 'beginner', name: 'Beginner', description: 'I am new to peer mock interviews.' },
  { id: 'intermediate', name: 'Intermediate', description: 'I have done several interviews already.' },
  { id: 'advanced', name: 'Advanced', description: 'I am a Jedi Master of mock interviews.' },
];

const FindPeerPage: React.FC = () => {
  const { user } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const [loading, setLoading] = useState(false);
  const [upcomingSessions, setUpcomingSessions] = useState<PeerInterviewSession[]>([]);
  const [pastSessions, setPastSessions] = useState<PeerInterviewSession[]>([]);
  const [showScheduleModal, setShowScheduleModal] = useState(false);
  const [scheduleData, setScheduleData] = useState<ScheduleModalData>({ step: 1 });
  const [availableTimeSlots, setAvailableTimeSlots] = useState<Date[]>([]);
  const [showConfirmationModal, setShowConfirmationModal] = useState(false);
  const [scheduledSession, setScheduledSession] = useState<PeerInterviewSession | null>(null);
  const [showFeedbackModal, setShowFeedbackModal] = useState(false);
  const [selectedSessionForFeedback, setSelectedSessionForFeedback] = useState<PeerInterviewSession | null>(null);
  const [showMatchingModal, setShowMatchingModal] = useState(false);
  const [matchingStatus, setMatchingStatus] = useState<any>(null);
  const [matchingPollInterval, setMatchingPollInterval] = useState<ReturnType<typeof setInterval> | null>(null);
  const [countdownInterval, setCountdownInterval] = useState<ReturnType<typeof setInterval> | null>(null);
  const [estimatedTimeRemaining, setEstimatedTimeRemaining] = useState<number>(600); // 10 minutes default
  const matchSoundPlayedRef = useRef<boolean>(false);
  const [showSurvey, setShowSurvey] = useState(false);
  const [surveySessionId, setSurveySessionId] = useState<string | null>(null);
  
  // Dev mode: always show "Start interview" button
  const DEV_MODE = true;

  useEffect(() => {
    loadSessions();
    generateTimeSlots();
    
    // Check if survey should be shown from URL parameter
    const sessionIdFromUrl = searchParams.get('session');
    const showSurveyFromUrl = searchParams.get('showSurvey');
    if (sessionIdFromUrl && showSurveyFromUrl === 'true') {
      setSurveySessionId(sessionIdFromUrl);
      setShowSurvey(true);
      // Clean up URL parameters
      setSearchParams({}, { replace: true });
    }
    
    // Cleanup polling on unmount
    return () => {
      if (matchingPollInterval) {
        clearInterval(matchingPollInterval);
      }
      if (countdownInterval) {
        clearInterval(countdownInterval);
      }
      setMatchingPollInterval(null);
      setCountdownInterval(null);
    };
  }, []);

  const loadSessions = async () => {
    try {
      const sessions = await peerInterviewService.getMySessions();
      const now = new Date();
      
      const upcoming = sessions.filter(s => {
        if (s.status === 'Cancelled') return false;
        const scheduledTime = s.scheduledTime ? new Date(s.scheduledTime) : null;
        return scheduledTime && scheduledTime > now;
      }).sort((a, b) => {
        const timeA = a.scheduledTime ? new Date(a.scheduledTime).getTime() : 0;
        const timeB = b.scheduledTime ? new Date(b.scheduledTime).getTime() : 0;
        return timeA - timeB;
      });

      const past = sessions.filter(s => {
        if (s.status === 'Cancelled') return false;
        const scheduledTime = s.scheduledTime ? new Date(s.scheduledTime) : null;
        return !scheduledTime || scheduledTime <= now || s.status === 'Completed';
      }).sort((a, b) => {
        const timeA = a.scheduledTime ? new Date(a.scheduledTime).getTime() : 0;
        const timeB = b.scheduledTime ? new Date(b.scheduledTime).getTime() : 0;
        return timeB - timeA;
      });

      setUpcomingSessions(upcoming);
      setPastSessions(past);
    } catch (error) {
      console.error('Error loading sessions:', error);
    }
  };

  const generateTimeSlots = () => {
    const slots: Date[] = [];
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    
    // Generate slots for 7 days (starting from next hour for today)
    const currentHour = now.getHours();
    const startHour = currentHour < 23 ? currentHour + 1 : 0;
    
    for (let day = 0; day < 7; day++) {
      const date = new Date(today);
      date.setDate(date.getDate() + day);
      const start = day === 0 ? startHour : 9; // Today starts from next hour, other days from 9 AM
      
      for (let hour = start; hour < 24; hour += 2) { // Every 2 hours
        if (hour >= 9 && hour <= 23) {
          const slot = new Date(date);
          slot.setHours(hour, 0, 0, 0);
          slots.push(slot);
        }
      }
    }
    
    setAvailableTimeSlots(slots);
  };

  const formatDate = (dateString?: string): string => {
    if (!dateString) return 'TBD';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric', 
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    });
  };

  const getTimeUntil = (dateString?: string): string => {
    if (!dateString) return '';
    const date = new Date(dateString);
    const now = new Date();
    const diff = date.getTime() - now.getTime();
    
    if (diff <= 0) return '• Live now';
    
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    const seconds = Math.floor((diff % (1000 * 60)) / 1000);
    
    return `• Live in ${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
  };

  const getMinutesUntil = (scheduledTime?: string): number => {
    if (!scheduledTime) return Infinity;
    
    const now = new Date();
    const scheduled = new Date(scheduledTime);
    const diff = scheduled.getTime() - now.getTime();
    
    if (diff <= 0) return 0;
    
    return Math.floor(diff / (1000 * 60));
  };

  const canStartInterview = (session: PeerInterviewSession): boolean => {
    if (DEV_MODE) return true; // Always show in dev mode
    const minutesUntil = getMinutesUntil(session.scheduledTime);
    return minutesUntil <= 10 && minutesUntil >= 0;
  };

  const handleStartInterview = async (sessionId: string) => {
    try {
      // Check if session has an interviewee
      const session = await peerInterviewService.getSession(sessionId);
      
      // Check if session was cancelled or reset - if so, allow re-matching
      if (session.status === 'Cancelled' || (session.status === 'Scheduled' && !session.intervieweeId)) {
        // Session was cancelled or reset, start matching process
        setShowMatchingModal(true);
        
        // Check if there's already a matching request
        try {
          const existingStatus = await peerInterviewService.getMatchingStatus(sessionId);
          if (existingStatus && existingStatus.status === 'Matched') {
            // Already matched, just need to confirm
            setMatchingStatus(existingStatus);
            startMatchingPoll(sessionId);
            return;
          }
        } catch (error: any) {
          // If error is 403 Forbid, it might mean the user is the matched user
          // Try to continue with starting matching - the backend will handle it
          if (error?.response?.status === 403) {
            console.warn("GetMatchingStatus returned 403, continuing with start matching");
          }
        }
        
        // Start matching process
        try {
          const result = await peerInterviewService.startMatching(sessionId);
          if (result.sessionComplete) {
            // Session is already complete, navigate directly
            // Get the session to find questionId
            try {
              const completedSession = await peerInterviewService.getSession(sessionId);
              if (completedSession.questionId) {
                window.location.href = `${ROUTES.QUESTIONS}/${completedSession.questionId}?session=${sessionId}`;
              } else {
                window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
              }
            } catch {
              window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
            }
            return;
          }
          
          setMatchingStatus(result.matchingRequest);
          startMatchingPoll(sessionId);
        } catch (error: any) {
          // Handle errors gracefully
          if (error?.response?.data?.message?.includes('already has an interviewee')) {
            // Session already has interviewee, navigate directly
            if (session.questionId) {
              window.location.href = `${ROUTES.QUESTIONS}/${session.questionId}?session=${sessionId}`;
            } else {
              window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
            }
            return;
          }
          throw error;
        }
        return;
      }
      
      if (session.intervieweeId && session.status === 'InProgress') {
        // Session already has an interviewee, navigate directly to question page
        if (session.questionId) {
          window.location.href = `${ROUTES.QUESTIONS}/${session.questionId}?session=${sessionId}`;
        } else {
          // Wait for backend to assign question
          setTimeout(async () => {
            try {
              const updatedSession = await peerInterviewService.getSession(sessionId);
              if (updatedSession.questionId) {
                window.location.href = `${ROUTES.QUESTIONS}/${updatedSession.questionId}?session=${sessionId}`;
              } else {
                window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
              }
            } catch {
              window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
            }
          }, 1000);
        }
        return;
      }

      // No interviewee yet, start matching process
      setShowMatchingModal(true);
      
      // Check if there's already a matching request
      try {
        const existingStatus = await peerInterviewService.getMatchingStatus(sessionId);
        if (existingStatus && existingStatus.status === 'Matched') {
          // Already matched, just need to confirm
          setMatchingStatus(existingStatus);
          startMatchingPoll(sessionId);
          return;
        }
      } catch (error: any) {
        // If error is 403 Forbid, it might mean the user is the matched user
        // Try to continue with starting matching - the backend will handle it
        if (error?.response?.status === 403) {
          console.warn("GetMatchingStatus returned 403, continuing with start matching");
        }
        // No existing status, continue with starting matching
      }
      
      // Start matching
      try {
        const result = await peerInterviewService.startMatching(sessionId);
        
        // Check if session is already complete
        if (result.sessionComplete) {
          setShowMatchingModal(false);
          // Navigate to question page with session parameter
          try {
            const updatedSession = await peerInterviewService.getSession(sessionId);
            if (updatedSession.questionId) {
              window.location.href = `${ROUTES.QUESTIONS}/${updatedSession.questionId}?session=${sessionId}`;
            } else {
              // No question yet - wait for backend to assign
              setTimeout(async () => {
                try {
                  const updatedSession = await peerInterviewService.getSession(sessionId);
                  if (updatedSession.questionId) {
                    window.location.href = `${ROUTES.QUESTIONS}/${updatedSession.questionId}?session=${sessionId}`;
                  } else {
                    window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
                  }
                } catch {
                  window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
                }
              }, 1000);
            }
          } catch {
            window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
          }
          return;
        }
        
        setMatchingStatus(result.matchingRequest);
        
        // Start polling for confirmation
        startMatchingPoll(sessionId);
      } catch (error: any) {
        // If error is "Session already has an interviewee", navigate directly
        if (error?.response?.data?.message?.includes('already has an interviewee')) {
          try {
            const updatedSession = await peerInterviewService.getSession(sessionId);
            if (updatedSession.intervieweeId) {
              setShowMatchingModal(false);
              if (updatedSession.questionId) {
                window.location.href = `${ROUTES.QUESTIONS}/${updatedSession.questionId}?session=${sessionId}`;
              } else {
                // No question yet - wait for backend to assign
                setTimeout(async () => {
                  try {
                    const updatedSession = await peerInterviewService.getSession(sessionId);
                    if (updatedSession.questionId) {
                      window.location.href = `${ROUTES.QUESTIONS}/${updatedSession.questionId}?session=${sessionId}`;
                    } else {
                      window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
                    }
                  } catch {
                    window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
                  }
                }, 1000);
              }
              return;
            }
          } catch {
            // Fall through to show error
          }
        }
        
        // Handle network errors gracefully - don't break the flow
        if (error?.code === 'ERR_NETWORK' || error?.message === 'Network Error') {
          console.warn('Network error during startMatching, but continuing with polling:', error);
          // Continue with polling - the polling will handle finding the match
          startMatchingPoll(sessionId);
          return;
        }
        
        throw error; // Re-throw to show error alert for other errors
      }
    } catch (error: any) {
      console.error('Error starting interview:', error);
      
      // Don't show alert for network errors - they're often transient
      if (error?.code === 'ERR_NETWORK' || error?.message === 'Network Error') {
        console.warn('Network error during interview start, but session may still work. Continuing...');
        // Try to continue - maybe the session is already set up
        try {
          const session = await peerInterviewService.getSession(sessionId);
          if (session.intervieweeId) {
            // Session is ready, navigate directly to question page
            setShowMatchingModal(false);
            if (session.questionId) {
              window.location.href = `${ROUTES.QUESTIONS}/${session.questionId}?session=${sessionId}`;
            } else {
              // No question yet - wait for backend to assign
              setTimeout(async () => {
                try {
                  const updatedSession = await peerInterviewService.getSession(sessionId);
                  if (updatedSession.questionId) {
                    window.location.href = `${ROUTES.QUESTIONS}/${updatedSession.questionId}?session=${sessionId}`;
                  } else {
                    window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
                  }
                } catch {
                  window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
                }
              }, 1000);
            }
            return;
          }
          // No interviewee yet, but start polling anyway - might work
          setShowMatchingModal(true);
          startMatchingPoll(sessionId);
          return;
        } catch {
          // If we can't get session, just close modal and let user try again
          setShowMatchingModal(false);
          return;
        }
      }
      
      // Only show alert for non-network errors
      const errorMessage = error?.response?.data?.message || error?.message || 'Failed to start interview';
      console.error('Non-network error:', errorMessage);
      // Don't use alert - just log and close modal
      setShowMatchingModal(false);
    }
  };

  // Play sound effect when match is found
  const playMatchSound = () => {
    try {
      // Create audio context for sound effect
      const audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
      const oscillator = audioContext.createOscillator();
      const gainNode = audioContext.createGain();
      
      oscillator.connect(gainNode);
      gainNode.connect(audioContext.destination);
      
      // Create a pleasant notification sound (two-tone chime)
      oscillator.frequency.setValueAtTime(800, audioContext.currentTime);
      oscillator.frequency.setValueAtTime(1000, audioContext.currentTime + 0.1);
      
      gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
      gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.3);
      
      oscillator.start(audioContext.currentTime);
      oscillator.stop(audioContext.currentTime + 0.3);
    } catch (error) {
      console.warn('Could not play match sound:', error);
    }
  };

  const startMatchingPoll = (sessionId: string) => {
    // Clear existing intervals
    if (matchingPollInterval) {
      clearInterval(matchingPollInterval);
    }
    if (countdownInterval) {
      clearInterval(countdownInterval);
    }

    // Reset sound played flag for new matching attempt
    matchSoundPlayedRef.current = false;

    // Start countdown timer (10 minutes)
    const startTime = Date.now();
    setEstimatedTimeRemaining(600); // 10 minutes in seconds

    // Update countdown timer every second
    const countdown = setInterval(() => {
      const elapsed = Math.floor((Date.now() - startTime) / 1000);
      const remaining = Math.max(0, 600 - elapsed); // 10 minutes - elapsed
      setEstimatedTimeRemaining(remaining);
    }, 1000);
    setCountdownInterval(countdown);

    const interval = setInterval(async () => {
      try {
        const status = await peerInterviewService.getMatchingStatus(sessionId);
        const previousStatus = matchingStatus?.status;
        setMatchingStatus(status);

        // Check if status changed from Pending/NoRequest to Matched
        // Only play sound once per match
        if (status.status === 'Matched' && previousStatus !== 'Matched' && !matchSoundPlayedRef.current) {
          // Match found! Play sound effect (only once)
          playMatchSound();
          matchSoundPlayedRef.current = true;
        }

        // Update countdown timer
        const elapsed = Math.floor((Date.now() - startTime) / 1000);
        const remaining = Math.max(0, 600 - elapsed); // 10 minutes - elapsed
        setEstimatedTimeRemaining(remaining);

        // Check if match is completed (both confirmed)
        if (status.status === 'Matched' || status.status === 'Confirmed') {
          // Check if both confirmed
          if (status.userConfirmed && status.matchedUserConfirmed) {
            // Match completed, navigate to session
            clearInterval(interval);
            if (countdownInterval) {
              clearInterval(countdownInterval);
            }
            setMatchingPollInterval(null);
            setCountdownInterval(null);
            setShowMatchingModal(false);
            
            // Try to get the session - it might be the current sessionId or the matched session
            // After completion, session1 becomes primary and session2 is cancelled
            // We need to find the active session (the one with status InProgress)
            let activeSessionId = sessionId;
            
            try {
              const session = await peerInterviewService.getSession(sessionId);
              
              // Check if this session is active (has interviewee and is InProgress)
              if (session.intervieweeId && session.status === 'InProgress') {
                // This is the active session
                activeSessionId = session.id;
                if (session.questionId) {
                  window.location.href = `${ROUTES.QUESTIONS}/${session.questionId}?session=${activeSessionId}`;
                } else {
                  // No question yet - wait for backend to assign
                  setTimeout(async () => {
                    try {
                      const updatedSession = await peerInterviewService.getSession(activeSessionId);
                      if (updatedSession.questionId) {
                        window.location.href = `${ROUTES.QUESTIONS}/${updatedSession.questionId}?session=${activeSessionId}`;
                      } else {
                        window.location.href = `${ROUTES.QUESTIONS}?session=${activeSessionId}`;
                      }
                    } catch {
                      window.location.href = `${ROUTES.QUESTIONS}?session=${activeSessionId}`;
                    }
                  }, 1000);
                }
                return;
              }
              
              // If current session is cancelled, we need to find the primary session
              // The primary session is the one from the matched request
              if (session.status === 'Cancelled' && status.matchedRequest) {
                // Try to get the session from the matched request
                // The matched request's session is the primary one
                try {
                  // Get sessions to find the active one
                  const allSessions = await peerInterviewService.getMySessions();
                  const activeSession = allSessions.find(s => 
                    s.intervieweeId && 
                    s.status === 'InProgress' &&
                    (s.interviewerId === user?.id || s.intervieweeId === user?.id)
                  );
                  
                  if (activeSession) {
                    activeSessionId = activeSession.id;
                    if (activeSession.questionId) {
                      // Both users should go to the SAME question
                      window.location.href = `${ROUTES.QUESTIONS}/${activeSession.questionId}?session=${activeSessionId}`;
                    } else {
                      // No question yet - wait for backend to assign
                      setTimeout(async () => {
                        try {
                          const updatedSession = await peerInterviewService.getSession(activeSessionId);
                          if (updatedSession.questionId) {
                            window.location.href = `${ROUTES.QUESTIONS}/${updatedSession.questionId}?session=${activeSessionId}`;
                          } else {
                            window.location.href = `${ROUTES.QUESTIONS}?session=${activeSessionId}`;
                          }
                        } catch {
                          window.location.href = `${ROUTES.QUESTIONS}?session=${activeSessionId}`;
                        }
                      }, 1000);
                    }
                    return;
                  }
                } catch (error) {
                  console.error('Error finding active session:', error);
                }
              }
            } catch (error) {
              console.warn('Could not get session by current sessionId:', error);
            }
            
            // Last resort: try to get user's sessions and find the active one
            try {
              const allSessions = await peerInterviewService.getMySessions();
              const activeSession = allSessions.find(s => 
                s.intervieweeId && 
                s.status === 'InProgress' &&
                (s.interviewerId === user?.id || s.intervieweeId === user?.id)
              );
              
              if (activeSession) {
                if (activeSession.questionId) {
                  // Both users should go to the SAME question
                  window.location.href = `${ROUTES.QUESTIONS}/${activeSession.questionId}?session=${activeSession.id}`;
                } else {
                  // No question yet - wait for backend to assign
                  setTimeout(async () => {
                    try {
                      const updatedSession = await peerInterviewService.getSession(activeSession.id);
                      if (updatedSession.questionId) {
                        window.location.href = `${ROUTES.QUESTIONS}/${updatedSession.questionId}?session=${activeSession.id}`;
                      } else {
                        window.location.href = `${ROUTES.QUESTIONS}?session=${activeSession.id}`;
                      }
                    } catch {
                      window.location.href = `${ROUTES.QUESTIONS}?session=${activeSession.id}`;
                    }
                  }, 1000);
                }
                return;
              }
            } catch (error) {
              console.error('Error getting user sessions:', error);
            }
            
            // Final fallback: try to navigate with current sessionId
            window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
          }
        } else if (status.status === 'Pending' || !status.status || status.status === 'NoRequest') {
          // Still waiting, try to find a match (only if not already matched)
          if (status.status !== 'Matched') {
            try {
              const result = await peerInterviewService.startMatching(sessionId);
              if (result.matched) {
                setMatchingStatus(result.matchingRequest);
              }
            } catch (error: any) {
              // If error is "Session already has an interviewee", check if we can navigate
              if (error?.response?.data?.message?.includes('already has an interviewee')) {
                try {
                  const session = await peerInterviewService.getSession(sessionId);
                  if (session.intervieweeId && session.status === 'InProgress') {
                    clearInterval(interval);
                    if (countdownInterval) {
                      clearInterval(countdownInterval);
                    }
                    setMatchingPollInterval(null);
                    setCountdownInterval(null);
                    setShowMatchingModal(false);
                    // Navigate to question page - both users should go to SAME question
                    if (session.questionId) {
                      window.location.href = `${ROUTES.QUESTIONS}/${session.questionId}?session=${sessionId}`;
                    } else {
                      // Wait for backend to assign question
                      setTimeout(async () => {
                        try {
                          const updatedSession = await peerInterviewService.getSession(sessionId);
                          if (updatedSession.questionId) {
                            window.location.href = `${ROUTES.QUESTIONS}/${updatedSession.questionId}?session=${sessionId}`;
                          } else {
                            window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
                          }
                        } catch {
                          window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
                        }
                      }, 1000);
                    }
                  }
                } catch {
                  // Ignore, continue polling
                }
              }
              // Otherwise ignore errors, continue polling
            }
          }
        }
      } catch (error) {
        // Ignore errors, continue polling
      }
    }, 2000); // Poll every 2 seconds

    // Store both intervals so we can clear them later
    setMatchingPollInterval(interval);
    
    // Store countdown interval reference (we'll clear it when clearing matchingPollInterval)
    // Note: We clear countdownInterval when clearing the matching poll interval
  };

  const handleConfirmMatch = async () => {
    if (!matchingStatus?.id) return;

    try {
      const result = await peerInterviewService.confirmMatch(matchingStatus.id);
      setMatchingStatus(result.matchingRequest);

      // NEW BEHAVIOR: Redirect immediately after confirming
      // The backend returns session1 (the merged session) so both users join the same session
      if (result.session) {
        // Session is available, navigate immediately
        if (matchingPollInterval) {
          clearInterval(matchingPollInterval);
          setMatchingPollInterval(null);
        }
        if (countdownInterval) {
          clearInterval(countdownInterval);
          setCountdownInterval(null);
        }
        setShowMatchingModal(false);
        
        // Use the session ID from the result (this is session1, the merged session)
        // Both users will be redirected to the same session with the same question
        const sessionId = result.session.id;
        if (sessionId && result.session.questionId) {
          // Session has question - redirect to question page
          // Both users will go to the same question in the same session
          window.location.href = `${ROUTES.QUESTIONS}/${result.session.questionId}?session=${sessionId}`;
        } else if (sessionId) {
          // Session exists but no question yet - wait for backend to assign
          setTimeout(async () => {
            try {
              const session = await peerInterviewService.getSession(sessionId);
              if (session.questionId) {
                // Both users will be redirected to the same question
                window.location.href = `${ROUTES.QUESTIONS}/${session.questionId}?session=${sessionId}`;
              } else {
                // Fallback: redirect to questions list with session
                window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
              }
            } catch {
              window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
            }
          }, 1000);
        }
      } else {
        // No session yet - continue polling, it will redirect when session is ready
        setMatchingStatus(result.matchingRequest);
      }
    } catch (error: any) {
      // Don't show alert for network errors
      if (error?.code === 'ERR_NETWORK' || error?.message === 'Network Error') {
        console.warn('Network error during confirm match, but match may still be processing');
        // Continue - the polling will detect when match is confirmed
        return;
      }
      console.error('Error confirming match:', error);
      // Only show alert for non-network errors
      const errorMessage = error?.response?.data?.message || 'Failed to confirm match';
      console.error('Confirm match error:', errorMessage);
      // Don't use alert - just log
    }
  };

  const getPartnerLabel = (session: PeerInterviewSession): string => {
    if (!user) return 'Partner';
    return session.interviewerId === user.id ? 'Interviewee' : 'Interviewer';
  };

  const handleScheduleClick = () => {
    setScheduleData({ step: 1 });
    setShowScheduleModal(true);
  };

  const handleNextStep = () => {
    if (scheduleData.step < 4) {
      setScheduleData({ ...scheduleData, step: scheduleData.step + 1 });
    } else {
      // Step 4 is the last step - submit directly
      handleScheduleSubmit();
    }
  };

  const handleBackStep = () => {
    if (scheduleData.step > 1) {
      setScheduleData({ ...scheduleData, step: scheduleData.step - 1 });
    } else {
      setShowScheduleModal(false);
    }
  };

  const handleScheduleSubmit = async () => {
    if (!user || !scheduleData.selectedTime) return;

    try {
      setLoading(true);
      
      // Create a session without an interviewee - matching will happen when starting interview
      const session = await peerInterviewService.createSession({
        interviewerId: user.id,
        // intervieweeId is undefined - will be matched when starting interview
        scheduledTime: scheduleData.selectedTime.toISOString(),
        duration: 45,
        interviewType: scheduleData.interviewType,
        practiceType: scheduleData.practiceType,
        interviewLevel: scheduleData.interviewLevel,
      });

      // Load the full session details
      const fullSession = await peerInterviewService.getSession(session.id);
      
      // Set session and close schedule modal first
      setScheduledSession(fullSession || session); // Fallback to created session if getSession fails
      setShowScheduleModal(false);
      setScheduleData({ step: 1 });
      
      // Wait a bit for schedule modal to close, then show confirmation
      setTimeout(() => {
        setShowConfirmationModal(true);
      }, 300);
      
      await loadSessions();
    } catch (error: any) {
      console.error('Error creating session:', error);
      // Don't show alert for network errors
      if (error?.code !== 'ERR_NETWORK' && error?.message !== 'Network Error') {
        const errorMessage = error?.response?.data?.message || 'Failed to schedule interview';
        console.error('Schedule interview error:', errorMessage);
        // Could show a toast notification here instead of alert
      }
    } finally {
      setLoading(false);
    }
  };

  const handleCancelSession = async (sessionId: string) => {
    if (!confirm('Are you sure you want to cancel this session?')) return;
    
    try {
      await peerInterviewService.cancelSession(sessionId);
      await loadSessions();
      
      // Reset matching state to allow re-matching
      setShowMatchingModal(false);
      setMatchingStatus(null);
      matchSoundPlayedRef.current = false;
      if (matchingPollInterval) {
        clearInterval(matchingPollInterval);
        setMatchingPollInterval(null);
      }
      if (countdownInterval) {
        clearInterval(countdownInterval);
        setCountdownInterval(null);
      }
    } catch (error: any) {
      // Don't show alert for network errors
      if (error?.code !== 'ERR_NETWORK' && error?.message !== 'Network Error') {
        const errorMessage = error?.response?.data?.message || 'Failed to cancel session';
        console.error('Cancel session error:', errorMessage);
        // Could show a toast notification here instead of alert
      }
    }
  };


  const renderModalContent = () => {
    switch (scheduleData.step) {
      case 1: // Select interview type
        return (
          <div className="modal-step modal-step-interview-type">
            <h2>Select your interview type</h2>
            <div className="interview-type-grid">
              {INTERVIEW_TYPES.map(type => (
                <div
                  key={type.id}
                  className={`interview-type-card ${scheduleData.interviewType === type.id ? 'selected' : ''}`}
                  onClick={() => setScheduleData({ ...scheduleData, interviewType: type.id })}
                >
                  <div className="type-icon">{type.icon}</div>
                  <div className="type-name">
                    {type.name}
                    {type.beta && <span className="beta-tag">Beta</span>}
                  </div>
                  <div className="type-description">{type.description}</div>
                </div>
              ))}
            </div>
          </div>
        );

      case 2: // Select practice type
        return (
          <div className="modal-step">
            <h2>Select your practice type</h2>
            <div className="practice-type-list">
              {PRACTICE_TYPES.map(type => (
                <div
                  key={type.id}
                  className={`practice-type-card ${scheduleData.practiceType === type.id ? 'selected' : ''}`}
                  onClick={() => setScheduleData({ ...scheduleData, practiceType: type.id })}
                >
                  <div className="type-icon">{type.icon}</div>
                  <div className="type-content">
                    <div className="type-name">
                      {type.name}
                      {type.external && <i className="fas fa-external-link-alt"></i>}
                    </div>
                    <div className="type-description">{type.description}</div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        );

      case 3: // Choose interview level
        return (
          <div className="modal-step">
            <h2>Choose your interview level</h2>
            <p className="modal-subtitle">This will be used to help match you with the best partner.</p>
            <div className="interview-level-list">
              {INTERVIEW_LEVELS.map(level => (
                <div
                  key={level.id}
                  className={`interview-level-card ${scheduleData.interviewLevel === level.id ? 'selected' : ''}`}
                  onClick={() => setScheduleData({ ...scheduleData, interviewLevel: level.id })}
                >
                  <div className="level-name">{level.name}</div>
                  <div className="level-description">{level.description}</div>
                </div>
              ))}
            </div>
          </div>
        );

      case 4: // Select time
        // Get already scheduled times to filter them out
        const scheduledTimes = upcomingSessions
          .filter(s => s.scheduledTime)
          .map(s => {
            const time = new Date(s.scheduledTime!);
            // Round to nearest 15 minutes for comparison
            time.setMinutes(Math.floor(time.getMinutes() / 15) * 15);
            time.setSeconds(0);
            time.setMilliseconds(0);
            return time.getTime();
          });

        // Filter out already scheduled times
        const availableSlots = availableTimeSlots.filter(slot => {
          const slotTime = new Date(slot);
          slotTime.setSeconds(0);
          slotTime.setMilliseconds(0);
          const slotTimeMs = slotTime.getTime();
          return !scheduledTimes.includes(slotTimeMs);
        });

        // Regroup filtered slots
        const filteredGroups = availableSlots.reduce((acc, slot) => {
          const dateKey = slot.toDateString();
          if (!acc[dateKey]) {
            acc[dateKey] = [];
          }
          acc[dateKey].push(slot);
          return acc;
        }, {} as Record<string, Date[]>);

        const sortedDates = Object.keys(filteredGroups).sort((a, b) => 
          new Date(a).getTime() - new Date(b).getTime()
        );

        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const tomorrow = new Date(today);
        tomorrow.setDate(tomorrow.getDate() + 1);

        const timeGroups = sortedDates.map(dateKey => {
          const date = new Date(dateKey);
          const dateTime = date.getTime();
          
          let label = '';
          if (dateTime === today.getTime()) {
            label = 'Today';
          } else if (dateTime === tomorrow.getTime()) {
            label = 'Tomorrow';
          } else {
            label = date.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' });
          }
          
          return {
            label,
            date,
            slots: filteredGroups[dateKey].sort((a, b) => a.getTime() - b.getTime())
          };
        });

        return (
          <div className="modal-step">
            <h2>Select a time to practice</h2>
            <p className="modal-subtitle">All times shown in your local timezone (EST)</p>
            <div className="time-selection">
              {timeGroups.length > 0 ? (
                timeGroups.map((group, groupIdx) => (
                  <div key={groupIdx} className="time-group">
                    <h3>{group.label}</h3>
                    <div className="time-slots">
                      {group.slots.map((slot, idx) => (
                        <button
                          key={idx}
                          className={`time-slot ${scheduleData.selectedTime?.getTime() === slot.getTime() ? 'selected' : ''}`}
                          onClick={() => setScheduleData({ ...scheduleData, selectedTime: slot })}
                        >
                          {slot.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}
                        </button>
                      ))}
                    </div>
                  </div>
                ))
              ) : (
                <p style={{ color: '#6b7280', textAlign: 'center', padding: '2rem' }}>
                  No available time slots. Please try a different date range.
                </p>
              )}
            </div>
          </div>
        );

      default:
        return null;
    }
  };

  return (
    <div className="find-peer-page">
      <Navbar />
      <div className="find-peer-container">
        {/* Hero Section */}
        <div className="hero-section">
          <h1 className="hero-title">Practice mock interviews with peers and AI</h1>
          <p className="hero-description">
            Join thousands of tech candidates practicing interviews to land jobs. Practice real questions over video chat in a collaborative environment with helpful AI feedback.
          </p>
          <div className="hero-actions">
            <button 
              onClick={handleScheduleClick} 
              className="btn-primary"
            >
              Schedule peer mock interview
            </button>
            <button className="btn-secondary">
              Start an AI interview
            </button>
          </div>
        </div>

        {/* Upcoming Interviews */}
        {upcomingSessions.length > 0 && (
          <div className="sessions-section">
            <div className="section-header">
              <h2>Upcoming interviews</h2>
              <a href="#" className="test-av-link">Test AV</a>
            </div>
            <table className="sessions-table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Type</th>
                  <th>Question you'll ask</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {upcomingSessions.map(session => (
                  <tr key={session.id}>
                    <td>
                      <span className="date-with-icon">
                        <i className="fas fa-calendar"></i>
                        {formatDate(session.scheduledTime)}
                      </span>
                    </td>
                    <td>{session.interviewType || 'N/A'}</td>
                    <td>
                      {session.question ? (
                        <Link to={`/questions/${session.questionId}`} className="question-link">
                          {session.question.title}
                        </Link>
                      ) : (
                        <span className="no-question">Not selected</span>
                      )}
                    </td>
                    <td>
                      <div className="session-actions">
                        {canStartInterview(session) ? (
                          <button
                            onClick={() => handleStartInterview(session.id)}
                            className="btn-start-interview"
                          >
                            Start interview
                          </button>
                        ) : (
                          <span className="live-indicator">{getTimeUntil(session.scheduledTime)}</span>
                        )}
                        {/* Only show cancel button if session doesn't have an interviewee (not matched yet) */}
                        {!session.intervieweeId && (
                          <button
                            onClick={() => handleCancelSession(session.id)}
                            className="btn-cancel-link"
                          >
                            Cancel session
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Past Interviews */}
        <div className="sessions-section">
          <h2>Past interviews</h2>
          <table className="sessions-table">
            <thead>
              <tr>
                <th>Date</th>
                <th>Type</th>
                <th>Questions</th>
                <th>Partner</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {pastSessions.length === 0 ? (
                <tr>
                  <td colSpan={5} className="no-sessions">No past interviews yet</td>
                </tr>
              ) : (
                pastSessions.map(session => (
                  <tr key={session.id}>
                    <td>{formatDate(session.scheduledTime)}</td>
                    <td>{session.interviewType || 'N/A'}</td>
                    <td>
                      {session.question ? (
                        <Link to={`/questions/${session.questionId}`} className="question-link">
                          {session.question.title}
                        </Link>
                      ) : (
                        <span className="no-question">n/a</span>
                      )}
                    </td>
                    <td>
                      <div className="partner-info">
                        <div className="partner-avatar">{getPartnerLabel(session).substring(0, 2).toUpperCase()}</div>
                        <button className="btn-partner-name">{getPartnerLabel(session)}</button>
                      </div>
                    </td>
                    <td>
                      <button 
                        className="btn-view-feedback"
                        onClick={() => {
                          // Check if survey is completed
                          const surveyCompleted = localStorage.getItem(`survey_completed_${session.id}`);
                          if (!surveyCompleted) {
                            // Show survey instead of feedback
                            setSurveySessionId(session.id);
                            setShowSurvey(true);
                          } else {
                            // Show feedback
                            setSelectedSessionForFeedback(session);
                            setShowFeedbackModal(true);
                          }
                        }}
                      >
                        {localStorage.getItem(`survey_completed_${session.id}`) ? 'View feedback' : 'Complete survey'}
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Confirmation Modal */}
      {showConfirmationModal && !showScheduleModal && (
        <div className="modal-overlay" onClick={(e) => e.target === e.currentTarget && setShowConfirmationModal(false)}>
          <div className="modal-content confirmation-modal">
            <button className="modal-close" onClick={() => setShowConfirmationModal(false)}>
              <i className="fas fa-times"></i>
            </button>
            <div className="confirmation-content">
              <div className="confirmation-icon">
                <i className="fas fa-check-circle"></i>
              </div>
              <h2>Your interview is confirmed!</h2>
              
              <div className="confirmation-details">
                <div className="confirmation-item">
                  <i className="fas fa-clock"></i>
                  <div>
                    <p>
                      Your {scheduledSession?.interviewType || scheduleData.interviewType || 'interview'} interview is{' '}
                      {scheduledSession?.scheduledTime || scheduleData.selectedTime
                        ? new Date(scheduledSession?.scheduledTime || scheduleData.selectedTime!.toISOString()).toLocaleDateString('en-US', { 
                            weekday: 'long', 
                            month: 'long', 
                            day: 'numeric',
                            hour: 'numeric',
                            minute: '2-digit'
                          })
                        : 'TBD'}
                      .{' '}
                      <a href="#" className="link-calendar">Add to your calendar</a> so you don't forget.
                    </p>
                  </div>
                </div>
                
                {scheduledSession?.question && (
                  <div className="confirmation-item">
                    <i className="fas fa-book-open"></i>
                    <div>
                      <p>
                        Your assigned question to ask is{' '}
                        <Link to={`/questions/${scheduledSession.questionId}`} className="link-question">
                          {scheduledSession.question.title}
                        </Link>
                        . Take a moment to review the question beforehand.
                      </p>
                    </div>
                  </div>
                )}
                
                <div className="confirmation-item">
                  <i className="fas fa-shield-alt"></i>
                  <div>
                    <p>
                      Review our <a href="#" className="link-guidelines">community guidelines</a> and be respectful to your partner. Please arrive on time and take turns.
                    </p>
                  </div>
                </div>
                
                <div className="confirmation-item">
                  <i className="fas fa-microphone"></i>
                  <div>
                    <p>Make sure you have a working camera and microphone.</p>
                  </div>
                </div>
              </div>
              
              <div className="confirmation-actions">
                <button className="btn-add-calendar">
                  Add to Calendar
                </button>
                <button 
                  className="btn-schedule-another"
                  onClick={() => {
                    setShowConfirmationModal(false);
                    handleScheduleClick();
                  }}
                >
                  Schedule another
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Schedule Modal */}
      {showScheduleModal && (
        <div className="modal-overlay" onClick={(e) => e.target === e.currentTarget && setShowScheduleModal(false)}>
          <div className="modal-content">
            <button className="modal-close" onClick={() => setShowScheduleModal(false)}>
              <i className="fas fa-times"></i>
            </button>
            {renderModalContent()}
            <div className="modal-actions">
              <button onClick={handleBackStep} className="btn-modal-back">
                {scheduleData.step === 1 ? 'Cancel' : 'Back'}
              </button>
              <button
                onClick={handleNextStep}
                disabled={
                  (scheduleData.step === 1 && !scheduleData.interviewType) ||
                  (scheduleData.step === 2 && !scheduleData.practiceType) ||
                  (scheduleData.step === 3 && !scheduleData.interviewLevel) ||
                  (scheduleData.step === 4 && !scheduleData.selectedTime) ||
                  loading
                }
                className="btn-modal-next"
              >
                {scheduleData.step === 4 ? (loading ? 'Scheduling...' : 'Schedule') : 'Next'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Matching Modal */}
      {showMatchingModal && (
        <div className="modal-overlay" onClick={() => {}}>
          <div className="modal-content matching-modal" onClick={(e) => e.stopPropagation()}>
            {!matchingStatus || matchingStatus.status === 'Pending' || matchingStatus.status === 'NoRequest' ? (
              // Waiting Room State
              <>
                <div className="matching-modal-header">
                  <h2>Waiting for your partner...</h2>
                </div>
                <div className="matching-modal-body waiting-room">
                  <div className="waiting-spinner">
                    <div className="spinner-circle"></div>
                  </div>
                  <div className="estimated-time">
                    <p className="time-label">Estimated time remaining:</p>
                    <p className="time-value">
                      {String(Math.floor(estimatedTimeRemaining / 60)).padStart(2, '0')}:
                      {String(estimatedTimeRemaining % 60).padStart(2, '0')}
                    </p>
                  </div>
                </div>
              </>
            ) : matchingStatus.status === 'Matched' ? (
              // Match Found State
              <>
                <div className="matching-modal-header">
                  <h2>You got matched!</h2>
                </div>
                <div className="matching-modal-body match-found">
                  <div className="match-avatar">
                    <div className="avatar-circle">
                      <i className="fas fa-user"></i>
                    </div>
                  </div>
                  <div className="match-actions">
                    <button
                      className="btn-join-interview"
                      onClick={handleConfirmMatch}
                    >
                      Join your interview
                    </button>
                  </div>
                </div>
              </>
            ) : (
              // Other status
              <>
                <div className="matching-modal-header">
                  <h2>Finding a Match...</h2>
                </div>
                <div className="matching-modal-body">
                  <p>Status: {matchingStatus.status}</p>
                </div>
              </>
            )}
          </div>
        </div>
      )}

      {/* Feedback Modal */}
      {showFeedbackModal && selectedSessionForFeedback && (
        <FeedbackView
          feedback={generateFeedbackData(selectedSessionForFeedback)}
          onClose={() => {
            setShowFeedbackModal(false);
            setSelectedSessionForFeedback(null);
          }}
        />
      )}

      {/* Survey Modal */}
      {showSurvey && surveySessionId && (
        <InterviewSurvey
          sessionId={surveySessionId}
          onComplete={() => {
            setShowSurvey(false);
            setSurveySessionId(null);
            loadSessions(); // Reload to refresh UI
          }}
        />
      )}
    </div>
  );
};

// Generate feedback data from session (template for all interviews)
const generateFeedbackData = (session: PeerInterviewSession): FeedbackData => {
  const date = session.scheduledTime 
    ? new Date(session.scheduledTime).toLocaleDateString('en-US', { 
        month: 'long', 
        day: 'numeric', 
        year: 'numeric' 
      })
    : 'Unknown date';

  return {
    interviewType: session.interviewType || 'Data Structures & Algorithms',
    date: date,
    problemSolving: {
      rating: 4,
      description: 'Demonstrated good problem-solving approach. Thought through the problem systematically and asked clarifying questions before jumping into code.'
    },
    codingSkills: {
      rating: 4,
      description: 'Solid coding skills with clean implementation. Code was well-structured and followed best practices. Minor improvements could be made in edge case handling.'
    },
    communication: {
      rating: 5,
      description: 'Excellent communication throughout the interview. Clearly explained thought process and reasoning. Engaged well with the problem and asked thoughtful questions.'
    },
    thingsDoneWell: 'Strong problem-solving approach, clear communication, and good code structure. Demonstrated understanding of data structures and algorithms.',
    areasForImprovement: 'Consider practicing more edge cases and optimizing solutions further. Work on time complexity analysis and space optimization techniques.',
    interviewerPerformance: {
      rating: 5,
      description: 'Great interview experience. The interviewer provided helpful hints when needed and created a supportive environment.'
    }
  };
};

export default FindPeerPage;
