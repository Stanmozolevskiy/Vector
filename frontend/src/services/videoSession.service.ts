import api from './api';

export interface VideoSession {
  id: string;
  sessionId: string;
  token: string;
  status: string;
  signalingData?: string;
  createdAt: string;
  endedAt?: string;
}

export interface CreateVideoSessionRequest {
  sessionId: string;
}

export interface WebRTCOfferRequest {
  offer: string;
}

export interface WebRTCAnswerRequest {
  answer: string;
}

export interface WebRTCIceCandidateRequest {
  candidate: string;
  sdpMLineIndex?: number;
  sdpMid?: string;
}

export const videoSessionService = {
  async createVideoSession(sessionId: string): Promise<VideoSession> {
    const response = await api.post<VideoSession>('/video-sessions/create', { sessionId });
    return response.data;
  },

  async getVideoSessionToken(sessionId: string): Promise<{ token: string; videoSessionId: string }> {
    const response = await api.get<{ token: string; videoSessionId: string }>(`/video-sessions/${sessionId}/token`);
    return response.data;
  },

  async getVideoSessionByToken(token: string): Promise<VideoSession> {
    const response = await api.get<VideoSession>(`/video-sessions/token/${token}`);
    return response.data;
  },

  async sendOffer(videoSessionId: string, offer: string): Promise<void> {
    await api.post(`/video-sessions/${videoSessionId}/offer`, { offer });
  },

  async sendAnswer(videoSessionId: string, answer: string): Promise<void> {
    await api.post(`/video-sessions/${videoSessionId}/answer`, { answer });
  },

  async sendIceCandidate(videoSessionId: string, candidate: string, sdpMLineIndex?: number, sdpMid?: string): Promise<void> {
    await api.post(`/video-sessions/${videoSessionId}/ice-candidate`, {
      candidate,
      sdpMLineIndex,
      sdpMid,
    });
  },

  async getSignalingData(videoSessionId: string): Promise<{ signalingData?: string }> {
    const response = await api.get<{ signalingData?: string }>(`/video-sessions/${videoSessionId}/signaling`);
    return response.data;
  },

  async endVideoSession(videoSessionId: string): Promise<void> {
    await api.post(`/video-sessions/${videoSessionId}/end`);
  },
};







