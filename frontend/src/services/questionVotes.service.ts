import api from './api';

export interface VoteRequest {
  voteType: number; // 1 = upvote, -1 = downvote
}

class QuestionVotesService {
  async voteQuestion(questionId: string, voteType: number): Promise<{ voteCount: number; message: string }> {
    const response = await api.post(`/questionvotes/questions/${questionId}/vote`, { voteType });
    return response.data;
  }

  async removeVote(questionId: string): Promise<void> {
    await api.delete(`/questionvotes/questions/${questionId}/vote`);
  }

  async getVoteCount(questionId: string): Promise<number> {
    const response = await api.get(`/questionvotes/questions/${questionId}/count`);
    return response.data.voteCount;
  }

  async getMyVote(questionId: string): Promise<number | null> {
    try {
      const response = await api.get(`/questionvotes/questions/${questionId}/my-vote`);
      return response.data.voteType;
    } catch (error) {
      return null;
    }
  }
}

export const questionVotesService = new QuestionVotesService();
