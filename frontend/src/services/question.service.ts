import api from './api';

export interface InterviewQuestion {
  id: string;
  title: string;
  description: string;
  difficulty: 'Easy' | 'Medium' | 'Hard';
  questionType: string;
  category: string;
  companyTags?: string[];
  tags?: string[];
  constraints?: string;
  examples?: Example[];
  hints?: string[];
  videoUrl?: string;
  roleTags?: string[];
  relatedQuestions?: RelatedQuestion[];
  relatedQuestionIds?: string[];
  relatedCourseIds?: string[];
  timeComplexityHint?: string;
  spaceComplexityHint?: string;
  acceptanceRate?: number;
  isActive: boolean;
  approvalStatus?: string;
  approvedBy?: string;
  approvedAt?: string;
  rejectionReason?: string;
  createdBy?: string;
  createdAt: string;
  updatedAt: string;
}

export interface Example {
  input?: string;
  output?: string;
  explanation?: string;
}

export interface QuestionList {
  id: string;
  title: string;
  difficulty: string;
  questionType: string;
  category: string;
  tags?: string[];
  companyTags?: string[];
  acceptanceRate?: number;
  isActive: boolean;
  approvalStatus?: string;
}

export interface RelatedQuestion {
  id: string;
  title: string;
}

export interface QuestionComment {
  id: string;
  questionId: string;
  userId: string;
  userName: string;
  userProfilePictureUrl?: string;
  content: string;
  createdAt: string;
  parentCommentId?: string;
  upvoteCount: number;
  upvotes?: number; // Alternative field name used by some endpoints
  hasUpvoted: boolean;
  commentType?: 'feedback' | 'tip' | 'question';
  replies: QuestionComment[];
}

export interface QuestionFilter {
  search?: string;
  questionType?: string;
  category?: string;
  categories?: string[];
  difficulty?: string;
  difficulties?: string[];
  companies?: string[];
  tags?: string[];
  role?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}

export interface QuestionTestCase {
  id: string;
  testCaseNumber: number;
  input: string;
  expectedOutput: string;
  isHidden: boolean;
  explanation?: string;
}

export interface QuestionSolution {
  id: string;
  language: string;
  code: string;
  explanation?: string;
  timeComplexity?: string;
  spaceComplexity?: string;
  isOfficial: boolean;
  createdAt: string;
}

export interface CreateQuestionDto {
  title: string;
  description: string;
  difficulty: string;
  questionType: string;
  category: string;
  companyTags?: string[];
  tags?: string[];
  constraints?: string;
  examples?: Example[];
  hints?: string[];
  videoUrl?: string;
  roleTags?: string[];
  relatedQuestionIds?: string[];
  relatedCourseIds?: string[];
  timeComplexityHint?: string;
  spaceComplexityHint?: string;
  acceptanceRate?: number;
}

export const questionService = {
  async getQuestions(filter?: QuestionFilter): Promise<QuestionList[]> {
    const response = await api.get<QuestionList[]>('/questions', { params: filter });
    return response.data;
  },

  async getQuestionById(id: string): Promise<InterviewQuestion> {
    const response = await api.get<InterviewQuestion>(`/questions/${id}`);
    return response.data;
  },

  async createQuestion(dto: CreateQuestionDto): Promise<InterviewQuestion> {
    const response = await api.post<InterviewQuestion>('/questions', dto);
    return response.data;
  },

  async submitQuestion(dto: CreateQuestionDto): Promise<InterviewQuestion> {
    const response = await api.post<InterviewQuestion>('/questions/submit', dto);
    return response.data;
  },

  async uploadQuestionVideo(file: File): Promise<string> {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post<{ videoUrl: string }>('/questions/upload-video', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data.videoUrl;
  },

  async updateQuestion(id: string, dto: Partial<CreateQuestionDto>): Promise<InterviewQuestion> {
    const response = await api.put<InterviewQuestion>(`/questions/${id}`, dto);
    return response.data;
  },

  async deleteQuestion(id: string): Promise<void> {
    await api.delete(`/questions/${id}`);
  },

  async getTestCases(questionId: string, includeHidden = false): Promise<QuestionTestCase[]> {
    const response = await api.get<QuestionTestCase[]>(`/questions/${questionId}/test-cases`, {
      params: { includeHidden },
    });
    return response.data;
  },

  async addTestCase(questionId: string, testCase: Omit<QuestionTestCase, 'id'>): Promise<QuestionTestCase> {
    const response = await api.post<QuestionTestCase>(`/questions/${questionId}/test-cases`, testCase);
    return response.data;
  },

  async getSolutions(questionId: string, language?: string): Promise<QuestionSolution[]> {
    const response = await api.get<QuestionSolution[]>(`/questions/${questionId}/solutions`, {
      params: { language },
    });
    return response.data;
  },

  async addSolution(questionId: string, solution: Omit<QuestionSolution, 'id' | 'createdAt'>): Promise<QuestionSolution> {
    const response = await api.post<QuestionSolution>(`/questions/${questionId}/solutions`, solution);
    return response.data;
  },

  async getPendingQuestions(): Promise<QuestionList[]> {
    const response = await api.get<QuestionList[]>('/questions/pending');
    return response.data;
  },

  async approveQuestion(questionId: string): Promise<InterviewQuestion> {
    const response = await api.post<InterviewQuestion>(`/questions/${questionId}/approve`);
    return response.data;
  },

  async rejectQuestion(questionId: string, rejectionReason?: string): Promise<InterviewQuestion> {
    const response = await api.post<InterviewQuestion>(`/questions/${questionId}/reject`, { rejectionReason });
    return response.data;
  },

  async getQuestionComments(questionId: string, page = 1, pageSize = 25, sort: 'hot' | 'top' | 'new' = 'hot'): Promise<QuestionComment[]> {
    const response = await api.get<QuestionComment[]>(`/questions/${questionId}/comments`, {
      params: { page, pageSize, sort },
    });
    return response.data;
  },

  async addQuestionComment(questionId: string, content: string, parentCommentId?: string): Promise<QuestionComment> {
    const response = await api.post<QuestionComment>(`/questions/${questionId}/comments`, { content, parentCommentId });
    return response.data;
  },

  async toggleCommentUpvote(questionId: string, commentId: string): Promise<{ commentId: string; upvoteCount: number; hasUpvoted: boolean }> {
    const response = await api.post<{ commentId: string; upvoteCount: number; hasUpvoted: boolean }>(`/questions/${questionId}/comments/${commentId}/upvote`);
    return response.data;
  },
};

