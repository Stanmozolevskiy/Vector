import { useEffect, useState, useRef } from 'react';
import { useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { peerInterviewService } from '../services/peerInterview.service';
import { RejoinModal } from './RejoinModal';
import { ROUTES } from '../utils/constants';

export const SessionNotificationManager = () => {
  const { user } = useAuth();
  const location = useLocation();
  const [showRejoinModal, setShowRejoinModal] = useState(false);
  const [activeSession, setActiveSession] = useState<any>(null);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const lastNotificationTimeRef = useRef<number>(0);

  // Check if user is currently on a session page
  const isOnSessionPage = () => {
    const path = location.pathname;
    const searchParams = new URLSearchParams(location.search);
    return path.startsWith(ROUTES.QUESTIONS) && searchParams.has('session');
  };

  useEffect(() => {
    if (!user?.id) {
      return;
    }

    const checkActiveSession = async () => {
      try {
        // Only check if user is NOT on the session page
        if (isOnSessionPage()) {
          // Hide modal if user is on session page
          setShowRejoinModal(false);
          setActiveSession(null);
          return; // Don't show notification while on session page
        }

        // Get user's active sessions
        const sessions = await peerInterviewService.getMySessions();
        
        // Find in-progress sessions where user is involved
        const inProgressSessions = sessions.filter(
          (session) =>
            session.status === 'InProgress' &&
            session.intervieweeId && // Must have both interviewer and interviewee
            session.interviewerId &&
            (session.interviewerId === user.id || session.intervieweeId === user.id)
        );

        // If no in-progress sessions found, hide modal
        if (inProgressSessions.length === 0) {
          setShowRejoinModal(false);
          setActiveSession(null);
          return;
        }

        // Check each in-progress session to see if it meets all criteria
        let validSession = null;
        for (const session of inProgressSessions) {
          // First check if there's a confirmed match for this session
          // Both users must have confirmed readiness
          let matchingStatus = null;
          try {
            matchingStatus = await peerInterviewService.getMatchingStatus(session.id);
          } catch (error) {
            // If we can't check matching status (e.g., 404, 403), skip this session
            // This is expected for sessions without matching requests
            continue;
          }
          
          // Only consider this session if:
          // 1. Matching status exists and is not null/undefined
          // 2. Status is not "NoRequest" (meaning there's actually a match)
          // 3. Status is not "Cancelled"
          // 4. Both users have confirmed (userConfirmed and matchedUserConfirmed are both true)
          if (!matchingStatus || 
              !matchingStatus.status || 
              matchingStatus.status === 'NoRequest' ||
              matchingStatus.status === 'Cancelled' ||
              matchingStatus.userConfirmed !== true || 
              matchingStatus.matchedUserConfirmed !== true) {
            // Session doesn't meet confirmation criteria, skip it
            continue;
          }

          // Check if session started less than 1 hour ago
          // Use scheduledTime if available and in the past, otherwise use updatedAt as fallback
          // (updatedAt is when status changed to InProgress)
          const now = Date.now();
          let sessionStartTime: number;
          
          if (session.scheduledTime) {
            const scheduled = new Date(session.scheduledTime).getTime();
            // Use scheduledTime if it's in the past (session has started)
            // Otherwise use updatedAt (when status changed to InProgress)
            sessionStartTime = scheduled <= now ? scheduled : new Date(session.updatedAt).getTime();
          } else {
            sessionStartTime = new Date(session.updatedAt).getTime();
          }
          
          const oneHourAgo = now - (60 * 60 * 1000); // 1 hour in milliseconds
          
          if (sessionStartTime < oneHourAgo) {
            // Session started more than 1 hour ago, skip this session
            continue;
          }

          // All criteria met - this is a valid session
          validSession = session;
          break; // Found a valid session, stop checking
        }

        // If we found a valid session, show the modal
        if (validSession) {
          // Check if we should show notification (every 20 seconds)
          const now = Date.now();
          if (now - lastNotificationTimeRef.current >= 20000) {
            // Fetch full session details to ensure we have questionId
            try {
              const fullSession = await peerInterviewService.getSession(validSession.id);
              setActiveSession(fullSession);
            } catch (error) {
              // If fetch fails, use the session from list
              setActiveSession(validSession);
            }
            setShowRejoinModal(true);
            lastNotificationTimeRef.current = now;
          }
        } else {
          // No valid session found, hide modal if shown
          setShowRejoinModal(false);
          setActiveSession(null);
        }
      } catch (error) {
        // Silently fail - don't interrupt user experience
        console.error('Error checking active session:', error);
      }
    };

    // Check immediately
    checkActiveSession();

    // Set up interval to check every 20 seconds
    intervalRef.current = setInterval(checkActiveSession, 20000);

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, [user?.id, location.pathname, location.search]);

  const handleRejoin = async () => {
    setShowRejoinModal(false);
    if (!activeSession?.id) {
      return;
    }

    // Fetch full session details to ensure we have questionId
    try {
      const fullSession = await peerInterviewService.getSession(activeSession.id);
      if (fullSession.questionId) {
        window.location.href = `${ROUTES.QUESTIONS}/${fullSession.questionId}?session=${fullSession.id}`;
      } else {
        // If no questionId, redirect to questions page with session
        window.location.href = `${ROUTES.QUESTIONS}?session=${fullSession.id}`;
      }
    } catch (error) {
      // If fetch fails, try with existing session data
      if (activeSession.questionId) {
        window.location.href = `${ROUTES.QUESTIONS}/${activeSession.questionId}?session=${activeSession.id}`;
      } else {
        window.location.href = `${ROUTES.QUESTIONS}?session=${activeSession.id}`;
      }
    }
  };

  const handleFinishInterview = () => {
    setShowRejoinModal(false);
    if (activeSession?.id) {
      // Redirect to find peer page with survey for this session
      window.location.href = `${ROUTES.FIND_PEER}?session=${activeSession.id}&showSurvey=true`;
    } else {
      window.location.href = ROUTES.FIND_PEER;
    }
  };

  const handleFeedback = () => {
    setShowRejoinModal(false);
    // Navigate to feedback page or show survey
    window.location.href = ROUTES.FIND_PEER;
  };

  const handleClose = () => {
    setShowRejoinModal(false);
    // Reset notification timer so it can show again after 20 seconds
    lastNotificationTimeRef.current = Date.now();
  };

  if (!showRejoinModal || !activeSession) {
    return null;
  }

  return (
    <RejoinModal
      onRejoin={handleRejoin}
      onFeedback={handleFeedback}
      onFinishInterview={handleFinishInterview}
      onClose={handleClose}
    />
  );
};

