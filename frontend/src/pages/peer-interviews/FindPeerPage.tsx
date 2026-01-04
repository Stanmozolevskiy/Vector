import React, { useState, useEffect, useRef } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { Navbar } from '../../components/layout/Navbar';
import { peerInterviewService } from '../../services/peerInterview.service';
import type { PeerInterviewSession, ScheduledInterviewSession } from '../../services/peerInterview.service';
import { FeedbackView, type FeedbackData } from '../../components/FeedbackView';
import { FeedbackForm } from '../../components/FeedbackForm';
import { NoFeedbackView } from '../../components/NoFeedbackView';
import { ROUTES } from '../../utils/constants';
import * as signalR from '@microsoft/signalr';
import api from '../../services/api';
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
  const [loading, setLoading] = useState(false);
  const [upcomingSessions, setUpcomingSessions] = useState<PeerInterviewSession[]>([]);
  const [pastSessions, setPastSessions] = useState<PeerInterviewSession[]>([]);
  const [showScheduleModal, setShowScheduleModal] = useState(false);
  const [scheduleData, setScheduleData] = useState<ScheduleModalData>({ step: 1 });
  const [availableTimeSlots, setAvailableTimeSlots] = useState<Date[]>([]);
  const [showConfirmationModal, setShowConfirmationModal] = useState(false);
  const [scheduledSession, setScheduledSession] = useState<PeerInterviewSession | null>(null);
  const [showFeedbackModal, setShowFeedbackModal] = useState(false);
  const [showFeedbackForm, setShowFeedbackForm] = useState(false);
  const [showNoFeedback, setShowNoFeedback] = useState(false);
  const [selectedSessionForFeedback, setSelectedSessionForFeedback] = useState<PeerInterviewSession | null>(null);
  const [feedbackData, setFeedbackData] = useState<FeedbackData | null>(null);
  const [loadingFeedback, setLoadingFeedback] = useState(false);
  const [feedbackStatus, setFeedbackStatus] = useState<any>(null);
  const [showMatchingModal, setShowMatchingModal] = useState(false);
  const [matchingStatus, setMatchingStatus] = useState<any>(null);
  const [matchingPollInterval, setMatchingPollInterval] = useState<ReturnType<typeof setInterval> | null>(null);
  const [countdownInterval, setCountdownInterval] = useState<ReturnType<typeof setInterval> | null>(null);
  const [estimatedTimeRemaining, setEstimatedTimeRemaining] = useState<number>(600); // 10 minutes default
  const matchSoundPlayedRef = useRef<boolean>(false);
  const [isConfirmingMatch, setIsConfirmingMatch] = useState(false);
  const [confirmationCountdown, setConfirmationCountdown] = useState<number | null>(null);
  const [confirmationTimeout, setConfirmationTimeout] = useState<ReturnType<typeof setTimeout> | null>(null);
  const [matchStartTime, setMatchStartTime] = useState<number | null>(null);
  const signalRConnectionRef = useRef<signalR.HubConnection | null>(null);
  const currentSessionIdRef = useRef<string | null>(null);
  
  // Dev mode: always show "Start interview" button
  const DEV_MODE = true;

  // Initialize SignalR connection
  useEffect(() => {
    const initializeSignalR = async () => {
      try {
        const accessToken = localStorage.getItem('accessToken');
        if (!accessToken) {
          return;
        }

        const baseUrl = (api.defaults.baseURL && typeof api.defaults.baseURL === 'string') 
          ? api.defaults.baseURL.replace('/api', '') 
          : 'http://localhost:5000';
        
        const connection = new signalR.HubConnectionBuilder()
          .withUrl(`${baseUrl}/api/collaboration?access_token=${accessToken}`, {
            transport: signalR.HttpTransportType.WebSockets,
          })
          .withAutomaticReconnect()
          .build();

        await connection.start();
        signalRConnectionRef.current = connection;
      } catch (error) {
        console.error('Failed to initialize SignalR:', error);
      }
    };

    initializeSignalR();

    return () => {
      if (signalRConnectionRef.current) {
        signalRConnectionRef.current.stop().catch(() => {});
        signalRConnectionRef.current = null;
      }
    };
  }, []);

  // On page load, expire any existing matching requests (user refreshed/closed page)
  useEffect(() => {
    const expireExistingRequests = async () => {
      try {
        const sessions = await peerInterviewService.getUpcomingSessions();
        for (const session of sessions) {
          try {
            const status = await peerInterviewService.getMatchingStatus(session.id);
            if (status && status.status !== 'Expired' && status.status !== 'Confirmed') {
              // User has an active request but page was refreshed/closed - expire it
              await peerInterviewService.expireAllRequestsForSession(session.id);
            }
          } catch (error: any) {
            // 204/404 means no request exists - this is fine
            if (error?.response?.status !== 204 && error?.response?.status !== 404) {
              console.error('Error checking matching status on page load:', error);
            }
          }
        }
      } catch (error) {
        console.error('Error expiring existing requests on page load:', error);
      }
    };

    expireExistingRequests();

    // Handle page unload/close
    const handleBeforeUnload = () => {
      if (currentSessionIdRef.current) {
        // SignalR disconnect will be handled by OnDisconnectedAsync
        // The backend OnDisconnectedAsync will clear presence
      }
    };

    window.addEventListener('beforeunload', handleBeforeUnload);
    window.addEventListener('pagehide', handleBeforeUnload);

    return () => {
      window.removeEventListener('beforeunload', handleBeforeUnload);
      window.removeEventListener('pagehide', handleBeforeUnload);
    };
  }, []);

  // Set/clear presence when matching modal opens/closes
  useEffect(() => {
    const updatePresence = async () => {
      if (!signalRConnectionRef.current || signalRConnectionRef.current.state !== signalR.HubConnectionState.Connected) {
        return;
      }

      const sessionId = currentSessionIdRef.current;
      if (!sessionId) {
        return;
      }

      try {
        if (showMatchingModal) {
          await signalRConnectionRef.current.invoke('SetMatchingModalOpen', sessionId);
        } else {
          await signalRConnectionRef.current.invoke('SetMatchingModalClosed', sessionId);
        }
      } catch (error) {
        console.error('Error updating presence:', error);
      }
    };

    updatePresence();
  }, [showMatchingModal]);

  useEffect(() => {
    loadSessions();
    generateTimeSlots();
    
    
    // Cleanup polling and timeouts on unmount
    return () => {
      if (matchingPollInterval) {
        clearInterval(matchingPollInterval);
      }
      if (countdownInterval) {
        clearInterval(countdownInterval);
      }
      if (confirmationTimeout) {
        clearTimeout(confirmationTimeout);
      }
      setMatchingPollInterval(null);
      setCountdownInterval(null);
      setConfirmationTimeout(null);
    };
  }, []);

  // Helper function to convert ScheduledInterviewSession to PeerInterviewSession
  const convertToLegacySession = (s: ScheduledInterviewSession): PeerInterviewSession => {
    // Get both participants from live session
    const participants = s.liveSession?.participants || [];
    const interviewerParticipant = participants.find((p: { role: string }) => p.role === 'Interviewer');
    const intervieweeParticipant = participants.find((p: { role: string }) => p.role === 'Interviewee');
    
    // Use participants from live session if available, otherwise fallback to session creator
    const interviewer = interviewerParticipant?.user || s.user;
    const interviewee = intervieweeParticipant?.user || undefined;
    
    // For upcoming sessions, use assignedQuestion if available, otherwise use liveSession activeQuestion
    // For past sessions, use liveSession questions
    const questionForUpcoming = s.assignedQuestion || s.liveSession?.activeQuestion;
    const questionIdForUpcoming = s.assignedQuestionId || s.liveSession?.activeQuestionId;
    
    return {
      id: s.liveSession?.id || s.id,
      status: (s.liveSession?.status || s.status) as any,
      scheduledTime: s.scheduledStartAt,
      duration: 60,
      interviewType: s.interviewType,
      practiceType: s.practiceType,
      interviewLevel: s.interviewLevel,
      createdAt: s.createdAt,
      updatedAt: s.updatedAt,
      interviewer: interviewer,
      interviewerId: interviewerParticipant?.userId || s.userId,
      interviewee: interviewee,
      intervieweeId: intervieweeParticipant?.userId,
      questionId: questionIdForUpcoming,
      question: questionForUpcoming,
      // Add first and second questions for display in Past interviews
      firstQuestionId: s.liveSession?.firstQuestionId,
      secondQuestionId: s.liveSession?.secondQuestionId,
      firstQuestion: s.liveSession?.firstQuestion,
      secondQuestion: s.liveSession?.secondQuestion,
      liveSessionId: s.liveSession?.id,
    };
  };

  const loadSessions = async () => {
    try {
      // Fetch upcoming and past sessions separately
      const [upcomingSessions, pastSessions] = await Promise.all([
        peerInterviewService.getUpcomingSessions(),
        peerInterviewService.getPastSessions(),
      ]);
      
      // Convert to legacy format using helper function
      const upcoming = upcomingSessions.map(convertToLegacySession).sort((a, b) => {
        const timeA = a.scheduledTime ? new Date(a.scheduledTime).getTime() : 0;
        const timeB = b.scheduledTime ? new Date(b.scheduledTime).getTime() : 0;
        return timeA - timeB;
      });

      const past = pastSessions.map(convertToLegacySession).sort((a, b) => {
        const timeA = a.scheduledTime ? new Date(a.scheduledTime).getTime() : 0;
        const timeB = b.scheduledTime ? new Date(b.scheduledTime).getTime() : 0;
        return timeB - timeA;
      });
      console.log("past sessions: " , past);

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
      // Check if session has a live session (both users already joined)
      // First check matching status to see if there's a live session
      try {
        const matchingStatus = await peerInterviewService.getMatchingStatus(sessionId);
        if (matchingStatus?.liveSessionId && matchingStatus.status === 'Confirmed') {
          // Both users confirmed and session exists - redirect to live session
          console.log('Session already active, redirecting to live session:', matchingStatus.liveSessionId);
          try {
            const liveSession = await peerInterviewService.getSession(matchingStatus.liveSessionId);
            if (liveSession.questionId) {
              window.location.href = `${ROUTES.QUESTIONS}/${liveSession.questionId}?session=${matchingStatus.liveSessionId}`;
            } else {
              window.location.href = `${ROUTES.QUESTIONS}?session=${matchingStatus.liveSessionId}`;
            }
            return;
          } catch (error) {
            console.error('Error getting live session:', error);
          }
        }
      } catch (error) {
        // No matching status or error - continue with normal flow
      }
      
      // Check if session has an interviewee
      const session = await peerInterviewService.getSession(sessionId);
      
      // Check if session was cancelled or reset - if so, allow re-matching
      if (session.status === 'Cancelled' || (session.status === 'Scheduled' && !session.intervieweeId)) {
        // Session was cancelled or reset, start matching process
        currentSessionIdRef.current = sessionId;
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
          // 204/404 means no matching request exists yet - this is expected and not an error
          // Only log if it's a different error
          if (error?.response?.status !== 204 && error?.response?.status !== 404 && error?.response?.status !== 403) {
            console.warn("GetMatchingStatus returned unexpected error:", error?.response?.status);
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
      currentSessionIdRef.current = sessionId;
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
        // 204/404 means no matching request exists yet - this is expected and not an error
        // Only log if it's a different error
        if (error?.response?.status !== 204 && error?.response?.status !== 404 && error?.response?.status !== 403) {
          console.warn("GetMatchingStatus returned unexpected error:", error?.response?.status);
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
        // Note: liveSessionId will be null until both users confirm
        if (status?.status === 'Matched' && previousStatus !== 'Matched' && !matchSoundPlayedRef.current) {
          // Match found with live session! Play sound effect (only once)
          playMatchSound();
          matchSoundPlayedRef.current = true;
          
          // Start 15-second confirmation countdown
          const matchTime = Date.now();
          setMatchStartTime(matchTime);
          setConfirmationCountdown(15);
          const timeout = setTimeout(() => {
            // 15 seconds elapsed, expire match and re-queue
            handleMatchTimeout(sessionId);
          }, 15000);
          setConfirmationTimeout(timeout);
        }

        // Update countdown timer
        const elapsed = Math.floor((Date.now() - startTime) / 1000);
        const remaining = Math.max(0, 600 - elapsed); // 10 minutes - elapsed
        setEstimatedTimeRemaining(remaining);

        // Check if match is ready (both confirmed)
        // In the new flow, LiveSessionId is set ONLY after both users confirm
        // Status is 'Confirmed' when both users have confirmed and live session is created
        const isConfirmed = status?.status === 'Confirmed';
        const hasLiveSession = status?.liveSessionId != null;
        
        // Both confirmed if status is 'Confirmed' and liveSessionId exists (backend creates session when both confirm)
        const bothConfirmed = isConfirmed && hasLiveSession;
        
        // Add logging to debug polling
        if (status) {
          console.log('POLLING: Status check - Status:', status.status, 'UserConfirmed:', status.userConfirmed, 'MatchedUserConfirmed:', status.matchedUserConfirmed, 'LiveSessionId:', status.liveSessionId, 'BothConfirmed:', bothConfirmed);
        }
        
        // If both confirmed, redirect immediately
        if (bothConfirmed) {
          // Both users confirmed - get the live session and redirect
          console.log('Polling detected both users confirmed! Status:', status?.status, 'UserConfirmed:', status?.userConfirmed, 'MatchedUserConfirmed:', status?.matchedUserConfirmed);
          
            clearInterval(interval);
            if (countdownInterval) {
              clearInterval(countdownInterval);
            }
            setMatchingPollInterval(null);
            setCountdownInterval(null);
            setShowMatchingModal(false);
          setIsConfirmingMatch(false);
          setConfirmationCountdown(null);
          setMatchStartTime(null);
          
          // Clear confirmation timeout if still active
          if (confirmationTimeout) {
            clearTimeout(confirmationTimeout);
            setConfirmationTimeout(null);
          }
          
          // Get the live session ID from the matching request
          // The matching request should have a liveSessionId when status is Confirmed
          let liveSessionId = status.liveSessionId;
          
          if (!liveSessionId) {
            // If liveSessionId not in status, try to get it from scheduled session
                    try {
              const scheduledSession = await peerInterviewService.getScheduledSession(sessionId);
              liveSessionId = scheduledSession.liveSessionId;
              console.log('Got liveSessionId from scheduled session:', liveSessionId);
            } catch (error) {
              console.warn('Error getting scheduled session during poll:', error);
            }
          }
          
          if (liveSessionId) {
            // Get the live session
            try {
              const liveSession = await peerInterviewService.getSession(liveSessionId);
              console.log('Got live session:', liveSession);
              if (liveSession.questionId) {
                console.log('Polling detected both confirmed, redirecting to question:', liveSession.questionId);
                window.location.href = `${ROUTES.QUESTIONS}/${liveSession.questionId}?session=${liveSessionId}`;
                return;
                    } else {
                console.log('Polling detected both confirmed, redirecting to questions list');
                window.location.href = `${ROUTES.QUESTIONS}?session=${liveSessionId}`;
                    return;
                  }
                } catch (error) {
              console.error('Error getting live session during poll:', error);
                }
              }
          
          // Fallback: try to get live session one more time by checking both matching requests
          console.log('Fallback: trying to find live session from matching request...');
          try {
            // Get matching status again to see if liveSessionId is now available
            const latestStatus = await peerInterviewService.getMatchingStatus(sessionId);
            if (latestStatus?.liveSessionId) {
              const liveSession = await peerInterviewService.getSession(latestStatus.liveSessionId);
              if (liveSession.questionId) {
                console.log('Found live session in fallback, redirecting:', liveSession.questionId);
                window.location.href = `${ROUTES.QUESTIONS}/${liveSession.questionId}?session=${latestStatus.liveSessionId}`;
                } else {
                console.log('Found live session in fallback, redirecting to questions list');
                window.location.href = `${ROUTES.QUESTIONS}?session=${latestStatus.liveSessionId}`;
                }
                return;
              }
            } catch (error) {
            console.error('Error in fallback redirect:', error);
              }
          
          // Last resort: wait a bit and try again
          console.log('Last resort: waiting 2 seconds and retrying...');
                      setTimeout(async () => {
                        try {
              const latestStatus = await peerInterviewService.getMatchingStatus(sessionId);
              if (latestStatus?.liveSessionId) {
                const liveSession = await peerInterviewService.getSession(latestStatus.liveSessionId);
                if (liveSession.questionId) {
                  window.location.href = `${ROUTES.QUESTIONS}/${liveSession.questionId}?session=${latestStatus.liveSessionId}`;
                          } else {
                  window.location.href = `${ROUTES.QUESTIONS}?session=${latestStatus.liveSessionId}`;
                }
                return;
                          }
            } catch (error) {
              console.error('Error in last resort redirect:', error);
            }
            // If still no live session found, show error
            console.error('Could not find live session after confirmation');
          }, 2000);
        } else if (status?.status === 'Matched') {
          // Match found but not both confirmed - update UI state and countdown
          const currentUserId = user?.id;
          const isRequestingUser = status.userId === currentUserId;
          const currentUserConfirmed = isRequestingUser 
            ? status.userConfirmed 
            : status.matchedUserConfirmed;
          
          // Update isConfirmingMatch to show waiting state if user has confirmed
          if (currentUserConfirmed) {
            setIsConfirmingMatch(true); // Show confirmed state (waiting for other user)
          }
          
          // Update confirmation countdown if match is found
          if (matchStartTime !== null) {
            // Calculate remaining time from when match was found (15 seconds from match time)
            const matchElapsed = Math.floor((Date.now() - matchStartTime) / 1000);
            const confirmationRemaining = Math.max(0, 15 - matchElapsed);
            setConfirmationCountdown(confirmationRemaining);
            
            // If countdown reaches 0, handle timeout
            if (confirmationRemaining === 0 && confirmationTimeout) {
              clearTimeout(confirmationTimeout);
              setConfirmationTimeout(null);
              handleMatchTimeout(sessionId);
            }
          }
        } else if (!status || status.status === 'Pending' || status.status === 'Expired') {
          // Still waiting, continue polling
          // Log queue status for debugging
          if (!status) {
            console.log('POLLING: No matching request found (may be creating new request after expiration, waiting for match...)');
          } else {
            console.log(`POLLING: Status=${status.status}, Waiting for match... RequestId=${status.id}, UserId=${status.userId}`);
          }
          // No action needed - just continue the poll interval
          // GetMatchingStatusAsync will trigger TryMatchAsync for pending requests
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
  
  // Countdown timer for confirmation countdown (updates every second)
  useEffect(() => {
    if (confirmationCountdown === null || confirmationCountdown <= 0) {
      return;
    }
    
    const interval = setInterval(() => {
      setConfirmationCountdown(prev => {
        if (prev === null || prev <= 1) {
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
    
    return () => clearInterval(interval);
  }, [confirmationCountdown]);

  // SIMPLIFIED: Expire match and re-queue users if timeout occurs
  const handleMatchTimeout = async (sessionId: string) => {
    console.log('TIMEOUT: Match confirmation timeout (15 seconds expired), expiring match and re-queuing...');
    
    // Check if both users have confirmed before timing out
    // If they have, don't timeout - let the polling handle the redirect
    try {
      const currentStatus = await peerInterviewService.getMatchingStatus(sessionId);
      if (currentStatus && currentStatus.status === 'Confirmed') {
        console.log('TIMEOUT: Both users confirmed, skipping timeout - polling will handle redirect');
        return; // Don't timeout if both confirmed
      }
      
      // If we have a matching request ID, expire it on the backend
      if (currentStatus?.id) {
        try {
          await peerInterviewService.expireMatch(currentStatus.id);
          console.log('TIMEOUT: Match expired on backend, users re-queued');
        } catch (error) {
          console.error('TIMEOUT: Error expiring match on backend:', error);
        }
      }
    } catch (error) {
      console.warn('TIMEOUT: Error checking match status before timeout:', error);
    }
    
    // Clear countdown and reset confirmation state
    setConfirmationCountdown(null);
    setMatchStartTime(null);
    setIsConfirmingMatch(false); // Reset confirmation state so user can confirm again
    if (confirmationTimeout) {
      clearTimeout(confirmationTimeout);
      setConfirmationTimeout(null);
    }
    
    // Reset matching status - polling will detect new match
    setMatchingStatus(null);
    matchSoundPlayedRef.current = false;
    
    // Polling will continue and detect when a new match is found
    // No need to manually start matching - user is already in queue
  };

  const handleConfirmMatch = async () => {
    if (!matchingStatus?.id) {
      console.error('Cannot confirm match: matchingStatus.id is missing');
      return;
    }

    // Clear countdown timeout since user is confirming
    if (confirmationTimeout) {
      clearTimeout(confirmationTimeout);
      setConfirmationTimeout(null);
    }
    setConfirmationCountdown(null);
    setMatchStartTime(null);

    setIsConfirmingMatch(true);
    try {
      // Set this user's readiness to true
      const result = await peerInterviewService.confirmMatch(matchingStatus.id);
      console.log('Confirm match result:', result);
      
      // Update matching status with the latest from backend
      setMatchingStatus(result.matchingRequest);

      // Check if both users have confirmed - if so, redirect immediately
      if (result.completed && result.session) {
        // Both users confirmed, session is ready - navigate immediately
        console.log('Both users confirmed, redirecting to session:', result.session.id);
        
        // Clear all intervals and timeouts
        if (matchingPollInterval) {
          clearInterval(matchingPollInterval);
          setMatchingPollInterval(null);
        }
        if (countdownInterval) {
          clearInterval(countdownInterval);
          setCountdownInterval(null);
        }
        setShowMatchingModal(false);
        setIsConfirmingMatch(false);
        setConfirmationCountdown(null);
        setMatchStartTime(null);
        
        // Use the session ID from the result
        const sessionId = result.session.id;
        
        if (sessionId && result.session.activeQuestionId) {
          // Session has question - redirect to question page
          console.log('Redirecting to question:', result.session.activeQuestionId);
          window.location.href = `${ROUTES.QUESTIONS}/${result.session.activeQuestionId}?session=${sessionId}`;
          return;
        } else if (sessionId) {
          // Session exists but no question yet - wait for backend to assign
          console.log('Session exists but no question yet, waiting...');
          setTimeout(async () => {
            try {
              const session = await peerInterviewService.getSession(sessionId);
              if (session.questionId) {
                console.log('Redirecting to question:', session.questionId);
                window.location.href = `${ROUTES.QUESTIONS}/${session.questionId}?session=${sessionId}`;
              } else {
                console.log('No question assigned, redirecting to questions list');
                window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
              }
            } catch (error) {
              console.error('Error getting session after confirm:', error);
              window.location.href = `${ROUTES.QUESTIONS}?session=${sessionId}`;
            }
          }, 1000);
          return;
        }
      } else if (result.completed && result.matchingRequest?.liveSessionId) {
        // Both users confirmed but session not in response, get it using liveSessionId
        console.log('Both confirmed, getting session from liveSessionId:', result.matchingRequest.liveSessionId);
        
        // Clear all intervals and timeouts
        if (matchingPollInterval) {
          clearInterval(matchingPollInterval);
          setMatchingPollInterval(null);
        }
        if (countdownInterval) {
          clearInterval(countdownInterval);
          setCountdownInterval(null);
        }
        setShowMatchingModal(false);
        setIsConfirmingMatch(false);
        setConfirmationCountdown(null);
        setMatchStartTime(null);
        
        try {
          const liveSession = await peerInterviewService.getSession(result.matchingRequest.liveSessionId);
          if (liveSession.questionId) {
            console.log('Redirecting to question:', liveSession.questionId);
            window.location.href = `${ROUTES.QUESTIONS}/${liveSession.questionId}?session=${result.matchingRequest.liveSessionId}`;
      } else {
            console.log('No question assigned, redirecting to questions list');
            window.location.href = `${ROUTES.QUESTIONS}?session=${result.matchingRequest.liveSessionId}`;
          }
          return;
        } catch (error) {
          console.error('Error getting live session:', error);
          window.location.href = `${ROUTES.QUESTIONS}?session=${result.matchingRequest.liveSessionId}`;
          return;
        }
      } else {
        // Only this user confirmed - waiting for other user
        // DO NOT redirect - wait for the other user to confirm
        // The polling will detect when both users confirm and redirect
        console.log('CONFIRM: This user confirmed, waiting for partner. Status:', result.matchingRequest.status, 'UserConfirmed:', result.matchingRequest.userConfirmed, 'LiveSessionId:', result.matchingRequest.liveSessionId);
        
        // Keep isConfirmingMatch as true to show "Confirmed" state (waiting for partner)
        // The polling will detect when both users confirm and redirect
        // Polling is already running and will detect when both users confirm
      }
    } catch (error: any) {
      // On error, reset the confirming state so user can try again
      setIsConfirmingMatch(false);
      
      // Don't show alert for network errors
      if (error?.code === 'ERR_NETWORK' || error?.message === 'Network Error') {
        console.warn('Network error during confirm match, but match may still be processing');
        // Continue - the polling will detect when match is confirmed
        return;
      }
      console.error('Error confirming match:', error);
      const errorMessage = error?.response?.data?.message || 'Failed to confirm match';
      console.error('Confirm match error:', errorMessage);
    }
  };

  const getPartnerLabel = (session: PeerInterviewSession): string => {
    if (!user) return 'Partner';
    
    // If current user is the interviewer, show interviewee's name
    if (session.interviewerId === user.id) {
      if (session.interviewee) {
        return `${session.interviewee.firstName} ${session.interviewee.lastName}`.trim() || session.interviewee.email || 'Interviewee';
      }
      return 'Interviewee';
    }
    
    // If current user is the interviewee, show interviewer's name
    if (session.interviewer) {
      return `${session.interviewer.firstName} ${session.interviewer.lastName}`.trim() || session.interviewer.email || 'Interviewer';
    }
    return 'Interviewer';
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
      
      // Schedule a new interview session - matching will happen when starting interview
      const session = await peerInterviewService.scheduleInterview({
        interviewType: scheduleData.interviewType!,
        practiceType: scheduleData.practiceType!,
        interviewLevel: scheduleData.interviewLevel!,
        scheduledStartAt: scheduleData.selectedTime.toISOString(),
      });

      // Load the full session details
      const fullSession = await peerInterviewService.getScheduledSession(session.id);
      
      // Convert to legacy format for compatibility
      const legacySession: PeerInterviewSession = {
        id: fullSession.id,
        status: fullSession.status as any,
        scheduledTime: fullSession.scheduledStartAt,
        duration: 60,
        interviewType: fullSession.interviewType,
        practiceType: fullSession.practiceType,
        interviewLevel: fullSession.interviewLevel,
        createdAt: fullSession.createdAt,
        updatedAt: fullSession.updatedAt,
        interviewer: fullSession.user,
        interviewerId: fullSession.userId,
        questionId: fullSession.liveSession?.activeQuestionId,
        question: fullSession.liveSession?.activeQuestion,
      };
      
      // Set session and close schedule modal first
      setScheduledSession(legacySession);
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
                      {session.firstQuestion || session.secondQuestion ? (
                        <div>
                          {session.firstQuestion && (
                            <div>
                              <Link 
                                to={`/questions/${session.firstQuestionId}`} 
                                className="question-link"
                              >
                                {session.firstQuestion.title}
                        </Link>
                            </div>
                          )}
                          {session.secondQuestion && (
                            <div>
                              <Link 
                                to={`/questions/${session.secondQuestionId}`} 
                                className="question-link"
                              >
                                {session.secondQuestion.title}
                              </Link>
                            </div>
                          )}
                        </div>
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
                        onClick={async () => {
                          if (!session.liveSessionId) {
                            alert('Session not found');
                            return;
                          }

                          setLoadingFeedback(true);
                            setSelectedSessionForFeedback(session);
                          
                          try {
                            // Get feedback status
                            const status = await peerInterviewService.getFeedbackStatus(session.liveSessionId);
                            setFeedbackStatus(status);

                            // If user hasn't submitted feedback for opponent, show feedback form
                            if (!status.hasUserSubmittedFeedback) {
                              setShowFeedbackForm(true);
                            } else {
                              // User has submitted feedback, check if opponent has left feedback
                              if (status.hasOpponentSubmittedFeedback && status.opponentFeedback) {
                                // Show opponent's feedback
                                const feedback = status.opponentFeedback;
                                const feedbackFormatted: FeedbackData = {
                                  interviewType: session.interviewType || 'Data Structures & Algorithms',
                                  date: session.scheduledTime 
                                    ? new Date(session.scheduledTime).toLocaleDateString('en-US', { 
                                        month: 'long', 
                                        day: 'numeric', 
                                        year: 'numeric' 
                                      })
                                    : 'Unknown date',
                                  problemSolving: {
                                    rating: feedback.problemSolvingRating || 0,
                                    description: feedback.problemSolvingDescription || ''
                                  },
                                  codingSkills: {
                                    rating: feedback.codingSkillsRating || 0,
                                    description: feedback.codingSkillsDescription || ''
                                  },
                                  communication: {
                                    rating: feedback.communicationRating || 0,
                                    description: feedback.communicationDescription || ''
                                  },
                                  thingsDoneWell: feedback.thingsDidWell || '',
                                  areasForImprovement: feedback.areasForImprovement || '',
                                  interviewerPerformance: {
                                    rating: feedback.interviewerPerformanceRating || 0,
                                    description: feedback.interviewerPerformanceDescription || ''
                                  }
                                };
                                setFeedbackData(feedbackFormatted);
                            setShowFeedbackModal(true);
                              } else {
                                // Opponent hasn't left feedback yet
                                setShowNoFeedback(true);
                          }
                            }
                          } catch (error: any) {
                            console.error('Error loading feedback status:', error);
                            alert('Failed to load feedback. Please try again.');
                          } finally {
                            setLoadingFeedback(false);
                          }
                        }}
                        disabled={loadingFeedback}
                      >
                        {loadingFeedback ? 'Loading...' : 'View feedback'}
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
              // Match Found State - Show confirmation button or waiting state
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
                    {(() => {
                      // Determine if current user has confirmed
                      // Current user is either the requesting user (userId) or matched user (matchedUserId)
                      const currentUserId = user?.id;
                      const isRequestingUser = matchingStatus.userId === currentUserId;
                      const currentUserConfirmed = isRequestingUser 
                        ? matchingStatus.userConfirmed 
                        : matchingStatus.matchedUserConfirmed;
                      const bothConfirmed = matchingStatus.userConfirmed && matchingStatus.matchedUserConfirmed;
                      
                      if (isConfirmingMatch && !currentUserConfirmed) {
                        // User is currently confirming (show spinner)
                        return (
                          <>
                            {confirmationCountdown !== null && confirmationCountdown > 0 && (
                              <div style={{ 
                                textAlign: 'center', 
                                marginBottom: '1rem',
                                fontSize: '0.875rem',
                                color: '#6b7280'
                              }}>
                                Please confirm within {confirmationCountdown} seconds
                              </div>
                            )}
                    <button
                      className="btn-join-interview"
                              disabled={true}
                            >
                              <span className="spinner" style={{ 
                                display: 'inline-block', 
                                width: '16px', 
                                height: '16px', 
                                border: '2px solid #ffffff',
                                borderTop: '2px solid transparent',
                                borderRadius: '50%',
                                animation: 'spin 1s linear infinite',
                                marginRight: '8px',
                                verticalAlign: 'middle'
                              }}></span>
                              Confirming...
                            </button>
                          </>
                        );
                      } else if (currentUserConfirmed && !bothConfirmed) {
                        // Current user has confirmed but waiting for other user
                        return (
                          <div style={{ 
                            textAlign: 'center',
                            padding: '1rem',
                            backgroundColor: '#f0fdf4',
                            border: '1px solid #86efac',
                            borderRadius: '8px',
                            color: '#166534'
                          }}>
                            <p style={{ margin: 0, fontWeight: 600 }}>✓ You're ready!</p>
                            <p style={{ margin: '0.5rem 0 0 0', fontSize: '0.875rem' }}>
                              Waiting for your partner to confirm...
                            </p>
                          </div>
                        );
                      } else {
                        // User hasn't confirmed yet - show button
                        return (
                          <>
                            {confirmationCountdown !== null && confirmationCountdown > 0 && (
                              <div style={{ 
                                textAlign: 'center', 
                                marginBottom: '1rem',
                                fontSize: '0.875rem',
                                color: '#6b7280'
                              }}>
                                Please confirm within {confirmationCountdown} seconds
                              </div>
                            )}
                            <button
                              className="btn-join-interview"
                              onClick={() => {
                                console.log('Join button clicked');
                                handleConfirmMatch();
                              }}
                              disabled={false}
                    >
                      Join your interview
                    </button>
                          </>
                        );
                      }
                    })()}
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
      {showFeedbackModal && selectedSessionForFeedback && feedbackData && (
        <FeedbackView
          feedback={feedbackData}
          onClose={() => {
            setShowFeedbackModal(false);
            setSelectedSessionForFeedback(null);
            setFeedbackData(null);
            setFeedbackStatus(null);
          }}
        />
      )}

      {/* Feedback Form Modal */}
      {showFeedbackForm && selectedSessionForFeedback && feedbackStatus && (
        <FeedbackForm
          liveSessionId={selectedSessionForFeedback.liveSessionId!}
          opponentId={feedbackStatus.opponentId!}
          opponentName={feedbackStatus.opponent ? `${feedbackStatus.opponent.firstName} ${feedbackStatus.opponent.lastName}` : undefined}
          interviewType={selectedSessionForFeedback.interviewType}
          date={selectedSessionForFeedback.scheduledTime 
            ? new Date(selectedSessionForFeedback.scheduledTime).toLocaleDateString('en-US', { 
                month: 'long', 
                day: 'numeric', 
                year: 'numeric' 
              })
            : undefined}
          onComplete={async () => {
            setShowFeedbackForm(false);
            // After submitting feedback, check if opponent has left feedback
            if (selectedSessionForFeedback.liveSessionId) {
              try {
                const status = await peerInterviewService.getFeedbackStatus(selectedSessionForFeedback.liveSessionId);
                if (status.hasOpponentSubmittedFeedback && status.opponentFeedback) {
                  // Show opponent's feedback
                  const feedback = status.opponentFeedback;
                  const feedbackFormatted: FeedbackData = {
                    interviewType: selectedSessionForFeedback.interviewType || 'Data Structures & Algorithms',
                    date: selectedSessionForFeedback.scheduledTime 
                      ? new Date(selectedSessionForFeedback.scheduledTime).toLocaleDateString('en-US', { 
                          month: 'long', 
                          day: 'numeric', 
                          year: 'numeric' 
                        })
                      : 'Unknown date',
                    problemSolving: {
                      rating: feedback.problemSolvingRating || 0,
                      description: feedback.problemSolvingDescription || ''
                    },
                    codingSkills: {
                      rating: feedback.codingSkillsRating || 0,
                      description: feedback.codingSkillsDescription || ''
                    },
                    communication: {
                      rating: feedback.communicationRating || 0,
                      description: feedback.communicationDescription || ''
                    },
                    thingsDoneWell: feedback.thingsDidWell || '',
                    areasForImprovement: feedback.areasForImprovement || '',
                    interviewerPerformance: {
                      rating: feedback.interviewerPerformanceRating || 0,
                      description: feedback.interviewerPerformanceDescription || ''
                    }
                  };
                  setFeedbackData(feedbackFormatted);
                  setShowFeedbackModal(true);
                } else {
                  // Opponent hasn't left feedback yet
                  setShowNoFeedback(true);
                }
              } catch (error) {
                console.error('Error checking feedback status after submission:', error);
                setShowNoFeedback(true);
              }
            }
            setSelectedSessionForFeedback(null);
            setFeedbackStatus(null);
          }}
          onCancel={() => {
            setShowFeedbackForm(false);
            setSelectedSessionForFeedback(null);
            setFeedbackStatus(null);
          }}
        />
      )}

      {/* No Feedback View */}
      {showNoFeedback && selectedSessionForFeedback && (
        <NoFeedbackView
          interviewType={selectedSessionForFeedback.interviewType}
          date={selectedSessionForFeedback.scheduledTime 
            ? new Date(selectedSessionForFeedback.scheduledTime).toLocaleDateString('en-US', { 
                month: 'long', 
                day: 'numeric', 
                year: 'numeric' 
              })
            : undefined}
          onClose={() => {
            setShowNoFeedback(false);
            setSelectedSessionForFeedback(null);
            setFeedbackStatus(null);
          }}
        />
      )}

    </div>
  );
};


export default FindPeerPage;
