import api from './api';

export interface QuestionComment {
  id: string;
  questionId: string;
  userId: string;
  userName: string;
  userProfilePictureUrl?: string;
  content: string;
  upvoteCount: number;
  upvotes?: number;
  hasUpvoted: boolean;
  isEdited?: boolean;
  createdAt: string;
  updatedAt?: string;
  parentCommentId?: string;
  commentType?: 'feedback' | 'tip' | 'question';
  replies: QuestionComment[];
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
  commentType?: string;
  parentCommentId?: string;
}

export interface UpdateCommentRequest {
  content: string;
}

class CommentsService {
  async createComment(
    questionId: string, 
    content: string, 
    commentType?: string, 
    parentCommentId?: string
  ): Promise<QuestionComment> {
    const response = await api.post(`/comments/questions/${questionId}`, { 
      content,
      commentType,
      parentCommentId
    });
    // Transform response to match expected format
    const comment = response.data;
    return {
      ...comment,
      userName: comment.user?.firstName || 'Unknown User',
      userProfilePictureUrl: comment.user?.profilePictureUrl,
      upvoteCount: comment.upvotes || comment.votes?.length || 0,
      hasUpvoted: false,
      replies: []
    };
  }

  async getComments(questionId: string): Promise<QuestionComment[]> {
    const response = await api.get(`/comments/questions/${questionId}`);
    // Transform backend response to match expected format
    return response.data.map((comment: any) => ({
      ...comment,
      userName: comment.user?.firstName || 'Unknown User',
      userProfilePictureUrl: comment.user?.profilePictureUrl,
      upvoteCount: comment.upvotes || comment.votes?.length || 0,
      hasUpvoted: false, // TODO: Check if current user has upvoted
      replies: []
    }));
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

  async downvoteComment(commentId: string): Promise<void> {
    await api.post(`/comments/${commentId}/downvote`);
  }

  async removeDownvote(commentId: string): Promise<void> {
    await api.delete(`/comments/${commentId}/downvote`);
  }
}

export const commentsService = new CommentsService();
