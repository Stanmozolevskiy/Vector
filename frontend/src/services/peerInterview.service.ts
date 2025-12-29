import api from './api';

// DTOs matching backend structure
export interface ScheduledInterviewSession {
  id: string;
  userId: string;
  interviewType: string;
  practiceType: string;
  interviewLevel: string;
  scheduledStartAt: string;
  status: 'Scheduled' | 'Cancelled' | 'Completed' | 'InProgress';
  liveSessionId?: string;
  createdAt: string;
  updatedAt: string;
  user?: UserDto;
  liveSession?: LiveInterviewSession;
}

export interface LiveInterviewSession {
  id: string;
  scheduledSessionId?: string;
  firstQuestionId?: string;
  secondQuestionId?: string;
  activeQuestionId?: string;
  status: 'InProgress' | 'Completed' | 'Cancelled';
  startedAt?: string;
  endedAt?: string;
  createdAt: string;
  updatedAt: string;
  firstQuestion?: QuestionSummary;
  secondQuestion?: QuestionSummary;
  activeQuestion?: QuestionSummary;
  participants?: Participant[];
}

export interface QuestionSummary {
  id: string;
  title: string;
  difficulty: string;
  questionType: string;
}

export interface Participant {
  id: string;
  userId: string;
  role: 'Interviewer' | 'Interviewee';
  isActive: boolean;
  joinedAt: string;
  user?: UserDto;
}

export interface UserDto {
  id: string;
  firstName?: string;
  lastName?: string;
  email: string;
}

export interface MatchingRequest {
  id: string;
  userId: string;
  scheduledSessionId: string;
  interviewType: string;
  practiceType: string;
  interviewLevel: string;
  scheduledStartAt: string;
  status: 'Pending' | 'Matched' | 'Confirmed' | 'Expired' | 'Cancelled';
  matchedUserId?: string;
  liveSessionId?: string;
  userConfirmed: boolean;
  matchedUserConfirmed: boolean;
  expiresAt: string;
  createdAt: string;
  updatedAt: string;
  user?: UserDto;
  matchedUser?: UserDto;
  scheduledSession?: ScheduledInterviewSession;
}

export interface StartMatchingResponse {
  matchingRequest: MatchingRequest;
  matched: boolean;
  sessionComplete?: boolean;
  session?: LiveInterviewSession;
}

export interface ConfirmMatchResponse {
  matchingRequest: MatchingRequest;
  completed: boolean;
  session?: LiveInterviewSession;
}

export interface SwitchRolesResponse {
  session: LiveInterviewSession;
  yourNewRole: string;
  partnerNewRole: string;
}

export interface ChangeQuestionResponse {
  session: LiveInterviewSession;
  newActiveQuestion?: QuestionSummary;
}

// Legacy interfaces for backward compatibility with existing frontend
export interface PeerInterviewSession {
  id: string;
  interviewerId?: string;
  intervieweeId?: string;
  questionId?: string;
  status: 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled';
  scheduledTime?: string;
  duration: number;
  sessionRecordingUrl?: string;
  interviewType?: string;
  practiceType?: string;
  interviewLevel?: string;
  createdAt: string;
  updatedAt: string;
  interviewer?: UserDto;
  interviewee?: UserDto;
  question?: QuestionSummary;
  // Include both questions for past interviews
  firstQuestion?: QuestionSummary;
  secondQuestion?: QuestionSummary;
  firstQuestionId?: string;
  secondQuestionId?: string;
}

// Request DTOs
export interface ScheduleInterviewRequest {
  interviewType: string;
  practiceType: string;
  interviewLevel: string;
  scheduledStartAt: string; // ISO 8601 date string
}

export interface ChangeQuestionRequest {
  questionId?: string;
}

export interface SubmitFeedbackRequest {
  liveSessionId: string;
  revieweeId: string;
  problemSolvingRating?: number;
  problemSolvingDescription?: string;
  codingSkillsRating?: number;
  codingSkillsDescription?: string;
  communicationRating?: number;
  communicationDescription?: string;
  thingsDidWell?: string;
  areasForImprovement?: string;
  interviewerPerformanceRating?: number;
  interviewerPerformanceDescription?: string;
}

export interface InterviewFeedback {
  id: string;
  liveSessionId: string;
  reviewerId: string;
  revieweeId: string;
  problemSolvingRating?: number;
  problemSolvingDescription?: string;
  codingSkillsRating?: number;
  codingSkillsDescription?: string;
  communicationRating?: number;
  communicationDescription?: string;
  thingsDidWell?: string;
  areasForImprovement?: string;
  interviewerPerformanceRating?: number;
  interviewerPerformanceDescription?: string;
  createdAt: string;
  updatedAt: string;
  reviewer?: UserDto;
  reviewee?: UserDto;
}

// Legacy request interfaces (deprecated but kept for compatibility)
export interface FindMatchRequest {
  preferredDifficulty?: string;
  preferredCategories?: string[];
}

export interface CreateSessionRequest {
  interviewerId: string;
  intervieweeId?: string;
  questionId?: string;
  scheduledTime?: string;
  duration?: number;
  interviewType?: string;
  practiceType?: string;
  interviewLevel?: string;
}

export const peerInterviewService = {
  /**
   * Schedule a new interview session
   */
  async scheduleInterview(request: ScheduleInterviewRequest): Promise<ScheduledInterviewSession> {
    const response = await api.post<ScheduledInterviewSession>('/peer-interviews/scheduled', request);
    return response.data;
  },

  /**
   * Get upcoming scheduled sessions for the current user
   */
  async getUpcomingSessions(): Promise<ScheduledInterviewSession[]> {
    const response = await api.get<ScheduledInterviewSession[]>('/peer-interviews/scheduled/upcoming');
    return response.data;
  },

  /**
   * Get a scheduled session by ID
   */
  async getScheduledSession(sessionId: string): Promise<ScheduledInterviewSession> {
    const response = await api.get<ScheduledInterviewSession>(`/peer-interviews/scheduled/${sessionId}`);
    return response.data;
  },

  /**
   * Cancel a scheduled session
   */
  async cancelScheduledSession(sessionId: string): Promise<void> {
    await api.post(`/peer-interviews/scheduled/${sessionId}/cancel`);
  },

  /**
   * Start matching process for a scheduled session
   */
  async startMatching(scheduledSessionId: string): Promise<StartMatchingResponse> {
    const response = await api.post<StartMatchingResponse>(
      `/peer-interviews/sessions/${scheduledSessionId}/start-matching`
    );
    return response.data;
  },

  /**
   * Get matching status for a scheduled session
   */
  async getMatchingStatus(scheduledSessionId: string): Promise<MatchingRequest | null> {
    try {
      const response = await api.get<MatchingRequest>(
        `/peer-interviews/sessions/${scheduledSessionId}/matching-status`
      );
      return response.data;
    } catch (error: any) {
      // 204 No Content means no matching request exists yet (valid state)
      // 404 also handled for backward compatibility
      if (error?.response?.status === 204 || error?.response?.status === 404) {
        return null;
      }
      throw error;
    }
  },

  /**
   * Confirm a match (user confirms readiness)
   */
  async confirmMatch(matchingRequestId: string): Promise<ConfirmMatchResponse> {
    const response = await api.post<ConfirmMatchResponse>(
      `/peer-interviews/matching-requests/${matchingRequestId}/confirm`
    );
    return response.data;
  },

  /**
   * Expire a match if not both confirmed within 15 seconds (re-queue users)
   */
  async expireMatch(matchingRequestId: string): Promise<void> {
    await api.post(`/peer-interviews/matching-requests/${matchingRequestId}/expire`);
  },

  /**
   * Get a live interview session by ID
   * Also works with scheduled session IDs - will return the live session if one exists
   */
  async getSession(sessionId: string): Promise<PeerInterviewSession> {
    try {
      // First try to get as live session
      const response = await api.get<LiveInterviewSession>(`/peer-interviews/sessions/${sessionId}`);
      const session = response.data;
      
      // Convert to legacy format for backward compatibility
      const interviewer = session.participants?.find(p => p.role === 'Interviewer');
      const interviewee = session.participants?.find(p => p.role === 'Interviewee');
      
      const legacy: PeerInterviewSession = {
        id: session.id,
        status: session.status as any,
        interviewerId: interviewer?.userId,
        intervieweeId: interviewee?.userId,
        questionId: session.activeQuestionId,
        duration: 60,
        createdAt: session.createdAt,
        updatedAt: session.updatedAt,
        interviewer: interviewer?.user,
        interviewee: interviewee?.user,
        question: session.activeQuestion,
      };

      // Try to get scheduled session info if available
      if (session.scheduledSessionId) {
        try {
          const scheduled = await this.getScheduledSession(session.scheduledSessionId);
          legacy.scheduledTime = scheduled.scheduledStartAt;
          legacy.interviewType = scheduled.interviewType;
          legacy.practiceType = scheduled.practiceType;
          legacy.interviewLevel = scheduled.interviewLevel;
        } catch {
          // Ignore if scheduled session not found
        }
      }

      return legacy;
    } catch (error: any) {
      // If not found as live session, try as scheduled session
      if (error?.response?.status === 404) {
        try {
          const scheduled = await this.getScheduledSession(sessionId);
          const legacy: PeerInterviewSession = {
            id: scheduled.id,
            status: scheduled.status as any,
            interviewerId: scheduled.userId,
            scheduledTime: scheduled.scheduledStartAt,
            duration: 60,
            interviewType: scheduled.interviewType,
            practiceType: scheduled.practiceType,
            interviewLevel: scheduled.interviewLevel,
            createdAt: scheduled.createdAt,
            updatedAt: scheduled.updatedAt,
            interviewer: scheduled.user,
            questionId: scheduled.liveSession?.activeQuestionId,
            question: scheduled.liveSession?.activeQuestion,
          };

          // If there's a live session, populate additional fields
          if (scheduled.liveSession) {
            const interviewer = scheduled.liveSession.participants?.find(p => p.role === 'Interviewer');
            const interviewee = scheduled.liveSession.participants?.find(p => p.role === 'Interviewee');
            
            legacy.id = scheduled.liveSession.id;
            legacy.interviewerId = interviewer?.userId;
            legacy.intervieweeId = interviewee?.userId;
            legacy.interviewer = interviewer?.user;
            legacy.interviewee = interviewee?.user;
            legacy.questionId = scheduled.liveSession.activeQuestionId;
            legacy.question = scheduled.liveSession.activeQuestion;
            legacy.status = scheduled.liveSession.status as any;
          }

          return legacy;
        } catch {
          throw error; // Re-throw original error if scheduled session also not found
        }
      }
      throw error;
    }
  },

  /**
   * Switch roles between interviewer and interviewee
   */
  async switchRoles(sessionId: string): Promise<SwitchRolesResponse> {
    const response = await api.post<SwitchRolesResponse>(`/peer-interviews/sessions/${sessionId}/switch-roles`);
    return response.data;
  },

  /**
   * Change the active question in a session (interviewer only)
   */
  async changeQuestion(sessionId: string, questionId?: string): Promise<ChangeQuestionResponse> {
    const response = await api.post<ChangeQuestionResponse>(
      `/peer-interviews/sessions/${sessionId}/change-question`,
      questionId ? { questionId } : {}
    );
    return response.data;
  },

  /**
   * End an interview session
   */
  async endInterview(sessionId: string): Promise<LiveInterviewSession> {
    const response = await api.post<LiveInterviewSession>(`/peer-interviews/sessions/${sessionId}/end`);
    return response.data;
  },

  /**
   * Submit feedback for a session
   */
  async submitFeedback(request: SubmitFeedbackRequest): Promise<InterviewFeedback> {
    const response = await api.post<InterviewFeedback>('/peer-interviews/feedback', request);
    return response.data;
  },

  /**
   * Get all feedback for a session
   */
  async getSessionFeedback(sessionId: string): Promise<InterviewFeedback[]> {
    const response = await api.get<InterviewFeedback[]>(`/peer-interviews/sessions/${sessionId}/feedback`);
    return response.data;
  },

  /**
   * Get a specific feedback by ID
   */
  async getFeedback(feedbackId: string): Promise<InterviewFeedback> {
    const response = await api.get<InterviewFeedback>(`/peer-interviews/feedback/${feedbackId}`);
    return response.data;
  },

  // Legacy methods for backward compatibility with existing frontend code
  /**
   * @deprecated Use getUpcomingSessions instead
   * Get user's sessions (legacy method) - maps new API to legacy format
   * NOTE: This method only returns scheduled sessions from getUpcomingSessions()
   * It does NOT include live sessions - use getSession(sessionId) for live sessions
   */
  async getMySessions(status?: string): Promise<PeerInterviewSession[]> {
    try {
      // Map from new structure to legacy structure
      const sessions = await this.getUpcomingSessions();
      
      // Convert ScheduledInterviewSession[] to PeerInterviewSession[]
      const legacySessions: PeerInterviewSession[] = sessions
        .filter(s => !status || s.status === status)
        .map(s => {
          // Start with scheduled session as base
          const legacy: PeerInterviewSession = {
            id: s.id, // Use scheduled session ID initially
            status: s.status as any,
            scheduledTime: s.scheduledStartAt,
            duration: 60, // Default duration
            interviewType: s.interviewType,
            practiceType: s.practiceType,
            interviewLevel: s.interviewLevel,
            createdAt: s.createdAt,
            updatedAt: s.updatedAt,
            interviewer: s.user,
            interviewerId: s.userId,
            questionId: s.liveSession?.activeQuestionId,
            question: s.liveSession?.activeQuestion,
          };

          // If there's a live session, use its ID and populate additional fields
          if (s.liveSession) {
            const interviewer = s.liveSession.participants?.find(p => p.role === 'Interviewer');
            const interviewee = s.liveSession.participants?.find(p => p.role === 'Interviewee');
            
            legacy.id = s.liveSession.id; // Use live session ID instead
            legacy.interviewerId = interviewer?.userId;
            legacy.intervieweeId = interviewee?.userId;
            legacy.interviewer = interviewer?.user;
            legacy.interviewee = interviewee?.user;
            legacy.questionId = s.liveSession.activeQuestionId;
            legacy.question = s.liveSession.activeQuestion;
            legacy.status = s.liveSession.status as any;
            legacy.createdAt = s.liveSession.createdAt;
            legacy.updatedAt = s.liveSession.updatedAt;
          }

          return legacy;
        });

      return legacySessions;
    } catch (error) {
      console.error('Error getting sessions:', error);
      return [];
    }
  },

  /**
   * @deprecated Use scheduleInterview instead
   * Create an interview session (legacy method) - maps to new scheduleInterview API
   */
  async createSession(_request: CreateSessionRequest): Promise<PeerInterviewSession> {
    // Map legacy request to new API structure
    if (!_request.interviewType || !_request.practiceType || !_request.interviewLevel || !_request.scheduledTime) {
      throw new Error('Missing required fields: interviewType, practiceType, interviewLevel, scheduledTime');
    }

    // Use the new scheduleInterview API
    const scheduledSession = await this.scheduleInterview({
      interviewType: _request.interviewType,
      practiceType: _request.practiceType,
      interviewLevel: _request.interviewLevel,
      scheduledStartAt: _request.scheduledTime,
    });

    // Convert ScheduledInterviewSession to legacy PeerInterviewSession format
    const legacy: PeerInterviewSession = {
      id: scheduledSession.id,
      status: scheduledSession.status as any,
      scheduledTime: scheduledSession.scheduledStartAt,
      duration: 60, // Default duration
      interviewType: scheduledSession.interviewType,
      practiceType: scheduledSession.practiceType,
      interviewLevel: scheduledSession.interviewLevel,
      createdAt: scheduledSession.createdAt,
      updatedAt: scheduledSession.updatedAt,
      interviewer: scheduledSession.user,
      interviewerId: scheduledSession.userId,
      questionId: scheduledSession.liveSession?.activeQuestionId,
      question: scheduledSession.liveSession?.activeQuestion,
    };

    // If there's a live session, use its ID and populate additional fields
    if (scheduledSession.liveSession) {
      const interviewer = scheduledSession.liveSession.participants?.find(p => p.role === 'Interviewer');
      const interviewee = scheduledSession.liveSession.participants?.find(p => p.role === 'Interviewee');
      
      legacy.id = scheduledSession.liveSession.id;
      legacy.interviewerId = interviewer?.userId;
      legacy.intervieweeId = interviewee?.userId;
      legacy.interviewer = interviewer?.user;
      legacy.interviewee = interviewee?.user;
      legacy.questionId = scheduledSession.liveSession.activeQuestionId;
      legacy.question = scheduledSession.liveSession.activeQuestion;
      legacy.status = scheduledSession.liveSession.status as any;
    }

    return legacy;
  },

  /**
   * @deprecated Not supported in new backend
   * Find a peer match (legacy method)
   */
  async findMatch(_request: FindMatchRequest): Promise<any> {
    throw new Error('findMatch is not supported. Use scheduleInterview and startMatching instead.');
  },

  /**
   * @deprecated Use cancelScheduledSession instead
   * Cancel session (legacy method)
   */
  async cancelSession(sessionId: string): Promise<void> {
    // Try to cancel as scheduled session first
    try {
      await this.cancelScheduledSession(sessionId);
      return;
    } catch (error: any) {
      // If not found as scheduled session, it might be a live session
      // Live sessions cannot be cancelled, only ended
      throw error;
    }
  },

  /**
   * @deprecated Use endInterview instead
   * Update session status (legacy method)
   */
  async updateSessionStatus(sessionId: string, status: string): Promise<PeerInterviewSession> {
    if (status === 'Completed' || status === 'Cancelled') {
      const session = await this.endInterview(sessionId);
      return session as PeerInterviewSession;
    }
    throw new Error('updateSessionStatus is deprecated. Use endInterview instead.');
  },

  /**
   * @deprecated Not supported in new backend
   * Update match preferences (legacy method)
   */
  async updateMatchPreferences(_request: any): Promise<any> {
    throw new Error('updateMatchPreferences is not supported in the new backend.');
  },

  /**
   * @deprecated Not supported in new backend
   * Get match preferences (legacy method)
   */
  async getMatchPreferences(): Promise<any> {
    throw new Error('getMatchPreferences is not supported in the new backend.');
  },
};
