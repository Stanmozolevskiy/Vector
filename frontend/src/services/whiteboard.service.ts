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
  async getWhiteboardData(questionId?: string): Promise<WhiteboardData | null> {
    try {
      const params = questionId ? { questionId } : {};
      const response = await api.get<WhiteboardData>('/whiteboard', { params });
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 404) {
        return null; // Whiteboard not found - return null instead of throwing
      }
      throw error;
    }
  },

  async saveWhiteboardData(data: SaveWhiteboardDataDto): Promise<WhiteboardData> {
    const response = await api.post<WhiteboardData>('/whiteboard', data);
    return response.data;
  },

  async deleteWhiteboardData(questionId?: string): Promise<void> {
    const params = questionId ? { questionId } : {};
    await api.delete('/whiteboard', { params });
  },
};
