import api from './api';

export interface PeerInterviewSession {
  id: string;
  interviewerId: string;
  intervieweeId: string;
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
  interviewer?: {
    id: string;
    firstName?: string;
    lastName?: string;
    email: string;
  };
  interviewee?: {
    id: string;
    firstName?: string;
    lastName?: string;
    email: string;
  };
  question?: {
    id: string;
    title: string;
    difficulty: string;
  };
}

export interface PeerInterviewMatch {
  id: string;
  userId: string;
  preferredDifficulty?: string;
  preferredCategories?: string;
  availability?: string;
  isAvailable: boolean;
  lastMatchDate?: string;
  createdAt: string;
  updatedAt: string;
  user?: {
    id: string;
    firstName?: string;
    lastName?: string;
    email: string;
  };
}

export interface FindMatchRequest {
  preferredDifficulty?: string;
  preferredCategories?: string[];
}

export interface CreateSessionRequest {
  interviewerId: string;
  intervieweeId?: string; // Can be undefined for matching pending
  questionId?: string;
  scheduledTime?: string;
  duration?: number;
  interviewType?: string;
  practiceType?: string;
  interviewLevel?: string;
}

export interface UpdateStatusRequest {
  status: string;
}

export interface UpdateMatchPreferencesRequest {
  preferredDifficulty?: string;
  preferredCategories?: string[];
  availability?: string;
  isAvailable?: boolean;
}

export const peerInterviewService = {
  // Find a peer match
  async findMatch(request: FindMatchRequest): Promise<PeerInterviewMatch> {
    const response = await api.post<PeerInterviewMatch>('/peer-interviews/find-match', request);
    return response.data;
  },

  // Create an interview session
  async createSession(request: CreateSessionRequest): Promise<PeerInterviewSession> {
    const response = await api.post<PeerInterviewSession>('/peer-interviews/sessions', request);
    return response.data;
  },

  // Get user's sessions
  async getMySessions(status?: string): Promise<PeerInterviewSession[]> {
    const params = status ? { status } : {};
    const response = await api.get<PeerInterviewSession[]>('/peer-interviews/sessions/me', { params });
    return response.data;
  },

  // Get session by ID
  async getSession(sessionId: string): Promise<PeerInterviewSession> {
    const response = await api.get<PeerInterviewSession>(`/peer-interviews/sessions/${sessionId}`);
    return response.data;
  },

  // Update session status
  async updateSessionStatus(sessionId: string, status: string): Promise<PeerInterviewSession> {
    const response = await api.put<PeerInterviewSession>(`/peer-interviews/sessions/${sessionId}/status`, { status });
    return response.data;
  },

  // Cancel session
  async cancelSession(sessionId: string): Promise<void> {
    await api.put(`/peer-interviews/sessions/${sessionId}/cancel`);
  },

  // Update match preferences
  async updateMatchPreferences(request: UpdateMatchPreferencesRequest): Promise<PeerInterviewMatch> {
    const response = await api.put<PeerInterviewMatch>('/peer-interviews/match-preferences', request);
    return response.data;
  },

  // Get match preferences
  async getMatchPreferences(): Promise<PeerInterviewMatch | null> {
    try {
      const response = await api.get<PeerInterviewMatch>('/peer-interviews/match-preferences');
      return response.data;
    } catch (error: any) {
      if (error?.response?.status === 404) {
        return null;
      }
      throw error;
    }
  },

  // Change question for the session (interviewer only)
  async changeQuestion(sessionId: string): Promise<PeerInterviewSession> {
    const response = await api.post<PeerInterviewSession>(`/peer-interviews/sessions/${sessionId}/change-question`);
    return response.data;
  },

  // Switch roles between interviewer and interviewee
  async switchRoles(sessionId: string): Promise<PeerInterviewSession> {
    const response = await api.post<PeerInterviewSession>(`/peer-interviews/sessions/${sessionId}/switch-roles`);
    return response.data;
  },

  // Start matching process
  async startMatching(sessionId: string): Promise<{ matchingRequest: any; matched: boolean; sessionComplete?: boolean }> {
    const response = await api.post<{ matchingRequest: any; matched: boolean; sessionComplete?: boolean }>(`/peer-interviews/sessions/${sessionId}/start-matching`);
    return response.data;
  },

  // Get matching status
  async getMatchingStatus(sessionId: string): Promise<any> {
    const response = await api.get(`/peer-interviews/sessions/${sessionId}/matching-status`);
    return response.data;
  },

  // Confirm match
  async confirmMatch(matchingRequestId: string): Promise<{ matchingRequest: any; session?: PeerInterviewSession; completed: boolean }> {
    const response = await api.post<{ matchingRequest: any; session?: PeerInterviewSession; completed: boolean }>(`/peer-interviews/matching-requests/${matchingRequestId}/confirm`);
    return response.data;
  },
};

