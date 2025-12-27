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

export interface QuestionFilter {
  search?: string;
  questionType?: string;
  category?: string;
  categories?: string[];
  difficulty?: string;
  difficulties?: string[];
  companies?: string[];
  tags?: string[];
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
  timeComplexityHint?: string;
  spaceComplexityHint?: string;
  acceptanceRate?: number;
}

export const questionService = {
  async getQuestions(filter?: QuestionFilter): Promise<QuestionList[]> {
    const response = await api.get<QuestionList[]>('/question', { params: filter });
    return response.data;
  },

  async getQuestionById(id: string): Promise<InterviewQuestion> {
    const response = await api.get<InterviewQuestion>(`/question/${id}`);
    return response.data;
  },

  async createQuestion(dto: CreateQuestionDto): Promise<InterviewQuestion> {
    const response = await api.post<InterviewQuestion>('/question', dto);
    return response.data;
  },

  async updateQuestion(id: string, dto: Partial<CreateQuestionDto>): Promise<InterviewQuestion> {
    const response = await api.put<InterviewQuestion>(`/question/${id}`, dto);
    return response.data;
  },

  async deleteQuestion(id: string): Promise<void> {
    await api.delete(`/question/${id}`);
  },

  async getTestCases(questionId: string, includeHidden = false): Promise<QuestionTestCase[]> {
    const response = await api.get<QuestionTestCase[]>(`/question/${questionId}/test-cases`, {
      params: { includeHidden },
    });
    return response.data;
  },

  async addTestCase(questionId: string, testCase: Omit<QuestionTestCase, 'id'>): Promise<QuestionTestCase> {
    const response = await api.post<QuestionTestCase>(`/question/${questionId}/test-cases`, testCase);
    return response.data;
  },

  async getSolutions(questionId: string, language?: string): Promise<QuestionSolution[]> {
    const response = await api.get<QuestionSolution[]>(`/question/${questionId}/solutions`, {
      params: { language },
    });
    return response.data;
  },

  async addSolution(questionId: string, solution: Omit<QuestionSolution, 'id' | 'createdAt'>): Promise<QuestionSolution> {
    const response = await api.post<QuestionSolution>(`/question/${questionId}/solutions`, solution);
    return response.data;
  },

  async getPendingQuestions(): Promise<QuestionList[]> {
    const response = await api.get<QuestionList[]>('/question/pending');
    return response.data;
  },

  async approveQuestion(questionId: string): Promise<InterviewQuestion> {
    const response = await api.post<InterviewQuestion>(`/question/${questionId}/approve`);
    return response.data;
  },

  async rejectQuestion(questionId: string, rejectionReason?: string): Promise<InterviewQuestion> {
    const response = await api.post<InterviewQuestion>(`/question/${questionId}/reject`, { rejectionReason });
    return response.data;
  },
};

