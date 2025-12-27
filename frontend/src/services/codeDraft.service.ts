import api from './api';

export interface CodeDraft {
  id: string;
  questionId: string;
  language: string;
  code: string;
  updatedAt: string;
}

export interface SaveCodeDraftRequest {
  questionId: string;
  language: string;
  code: string;
}

class CodeDraftService {
  async getCodeDraft(questionId: string, language: string): Promise<CodeDraft | null> {
    try {
      const response = await api.get<CodeDraft>(`/code-drafts/${questionId}/${language}`);
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 404) {
        return null; // Draft not found is not an error
      }
      throw error;
    }
  }

  async saveCodeDraft(request: SaveCodeDraftRequest): Promise<CodeDraft> {
    const response = await api.post<CodeDraft>('/code-drafts', request);
    return response.data;
  }

  async deleteCodeDraft(questionId: string, language: string): Promise<void> {
    await api.delete(`/code-drafts/${questionId}/${language}`);
  }
}

export const codeDraftService = new CodeDraftService();

