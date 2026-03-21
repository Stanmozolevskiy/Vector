import { useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { peerInterviewService } from '../../services/peerInterview.service';
import { ROUTES } from '../../utils/constants';

const getRedirectUrlForSession = (interviewType: string | undefined, liveSessionId: string, activeQuestionId?: string | null) => {
  const t = String(interviewType || '').trim().toLowerCase();

  if (t === 'system-design') {
    return ROUTES.SYSTEM_DESIGN_INTERVIEW.replace(':sessionId', liveSessionId);
  }

  if (t === 'behavioral' || t === 'product-management') {
    const encoded = encodeURIComponent(t);
    return `${ROUTES.PEER_INTERVIEW_SESSION.replace(':id', liveSessionId)}?type=${encoded}`;
  }

  if (activeQuestionId) {
    return `${ROUTES.QUESTIONS}/${activeQuestionId}?session=${liveSessionId}`;
  }
  return `${ROUTES.QUESTIONS}?session=${liveSessionId}`;
};

export const FriendInvitePage = () => {
  const { liveSessionId } = useParams<{ liveSessionId: string }>();
  const { isAuthenticated, isLoading, user } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!liveSessionId) {
      // No session ID, redirect to find page
      window.location.assign(ROUTES.FIND_PEER);
      return;
    }
    
    if (isLoading) return;

    const returnUrl = encodeURIComponent(`/friend-invite/${liveSessionId}`);

    if (!isAuthenticated) {
      // Save destination to session storage for post-registration redirects
      sessionStorage.setItem('pendingInviteRedirect', `/friend-invite/${liveSessionId}`);
      // Redirect to login with return URL
      navigate(`/login?returnUrl=${returnUrl}`, { replace: true });
      return;
    }

    let cancelled = false;
    (async () => {
      try {
        console.log('[FriendInvite] Attempting to join session:', liveSessionId);
        console.log('[FriendInvite] Current user:', user?.email);
        
        const joined = await peerInterviewService.joinFriendInterview(liveSessionId);
        
        console.log('[FriendInvite] Join successful:', joined.session.interviewType);
        
        if (cancelled) return;
        
        const redirect = getRedirectUrlForSession(
          joined.session.interviewType, 
          liveSessionId, 
          joined.session.activeQuestionId || undefined
        );
        
        console.log('[FriendInvite] Redirecting to:', redirect);
        
        // Use window.location.assign for clean navigation
        window.location.assign(redirect);
      } catch (error: any) {
        console.error('[FriendInvite] Join failed:', error);
        
        // If already a participant (409 or specific message), try to get the session directly
        if (error?.response?.status === 409 || error?.response?.status === 400) {
          console.log('[FriendInvite] User might already be a participant, trying to get session...');
          
          try {
            const session = await peerInterviewService.getSession(liveSessionId);
            console.log('[FriendInvite] Got session directly:', session.interviewType);
            
            if (cancelled) return;
            
            const redirect = getRedirectUrlForSession(
              session.interviewType,
              liveSessionId,
              session.questionId || undefined
            );
            
            console.log('[FriendInvite] Redirecting to:', redirect);
            window.location.assign(redirect);
            return;
          } catch (getError) {
            console.error('[FriendInvite] Failed to get session:', getError);
          }
        }
        
        // Fall back: if everything fails, route to find page
        if (!cancelled) {
          console.log('[FriendInvite] All attempts failed, redirecting to find page');
          window.location.assign(ROUTES.FIND_PEER);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [isAuthenticated, isLoading, liveSessionId, navigate, user]);

  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100vh' }}>
      <div style={{ textAlign: 'center', fontFamily: 'sans-serif' }}>
        <div className="loading-spinner">Joining session…</div>
      </div>
    </div>
  );
};

