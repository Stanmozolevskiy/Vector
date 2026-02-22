import api from './api';

export interface Bookmark {
  id: string;
  questionId: string;
  notes?: string;
  createdAt: string;
}

export interface BookmarkedQuestion {
  id: string;
  title: string;
  difficulty: string;
  category: string;
  questionType: string;
  tags: string[];
  acceptanceRate: number;
}

const bookmarkService = {
  async bookmarkQuestion(questionId: string, notes?: string): Promise<Bookmark> {
    const response = await api.post(`/questions/${questionId}/bookmark`, { notes });
    return response.data;
  },

  async removeBookmark(questionId: string): Promise<void> {
    await api.delete(`/questions/${questionId}/bookmark`);
  },

  async getBookmarkedQuestions(): Promise<BookmarkedQuestion[]> {
    const response = await api.get('/questions/bookmarks');
    return response.data;
  },

  async isQuestionBookmarked(questionId: string): Promise<boolean> {
    const response = await api.get(`/questions/${questionId}/bookmark`);
    return response.data.isBookmarked;
  },
};

export default bookmarkService;
