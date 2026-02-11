import api from './api';

export interface QuestionComment {
  id: string;
  questionId: string;
  userId: string;
  content: string;
  upvoteCount?: number;
  isEdited: boolean;
  createdAt: string;
  updatedAt: string;
  user?: {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    profilePictureUrl?: string;
  };
  votes?: any[];
}

export interface CreateCommentRequest {
  content: string;
}

export interface UpdateCommentRequest {
  content: string;
}

class CommentsService {
  async createComment(questionId: string, content: string): Promise<QuestionComment> {
    const response = await api.post(`/comments/questions/${questionId}`, { content });
    return response.data;
  }

  async getComments(questionId: string): Promise<QuestionComment[]> {
    const response = await api.get(`/comments/questions/${questionId}`);
    return response.data;
  }

  async updateComment(commentId: string, content: string): Promise<QuestionComment> {
    const response = await api.put(`/comments/${commentId}`, { content });
    return response.data;
  }

  async deleteComment(commentId: string): Promise<void> {
    await api.delete(`/comments/${commentId}`);
  }

  async upvoteComment(commentId: string): Promise<void> {
    await api.post(`/comments/${commentId}/upvote`);
  }

  async removeUpvote(commentId: string): Promise<void> {
    await api.delete(`/comments/${commentId}/upvote`);
  }
}

export const commentsService = new CommentsService();
