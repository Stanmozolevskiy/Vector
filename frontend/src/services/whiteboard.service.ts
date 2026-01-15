import api from './api';

export interface WhiteboardData {
  id: string;
  questionId?: string;
  elements: string;
  appState: string;
  files: string;
  createdAt: string;
  updatedAt: string;
}

export interface SaveWhiteboardDataDto {
  questionId?: string;
  elements: string;
  appState: string;
  files: string;
}

export const whiteboardService = {
  async getWhiteboardData(questionId?: string, partnerUserId?: string): Promise<WhiteboardData | null> {
    try {
      const params: any = questionId ? { questionId } : {};
      if (partnerUserId) {
        params.partnerUserId = partnerUserId;
      }
      const response = await api.get<WhiteboardData>('/whiteboard', { params });
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 404) {
        return null; // Whiteboard not found - return null instead of throwing
      }
      throw error;
    }
  },

  async getSessionWhiteboard(sessionId: string): Promise<WhiteboardData | null> {
    try {
      const response = await api.get<WhiteboardData>('/whiteboard', { params: { sessionId } });
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 404) {
        return null;
      }
      throw error;
    }
  },

  async saveWhiteboardData(data: SaveWhiteboardDataDto, userId?: string): Promise<WhiteboardData> {
    const params: any = {};
    if (userId) {
      params.userId = userId;
    }
    const response = await api.post<WhiteboardData>('/whiteboard', data, { params });
    return response.data;
  },

  async saveSessionWhiteboard(sessionId: string, data: Omit<SaveWhiteboardDataDto, 'questionId'>): Promise<WhiteboardData> {
    const response = await api.post<WhiteboardData>(
      '/whiteboard',
      { ...data },
      { params: { sessionId } }
    );
    return response.data;
  },

  async deleteWhiteboardData(questionId?: string): Promise<void> {
    const params = questionId ? { questionId } : {};
    await api.delete('/whiteboard', { params });
  },
};
