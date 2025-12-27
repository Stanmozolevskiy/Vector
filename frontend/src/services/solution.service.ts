import api from './api';

export interface SubmitSolutionRequest {
  questionId: string;
  language: string;
  code: string;
}

export interface UserSolution {
  id: string;
  userId: string;
  questionId: string;
  questionTitle: string;
  language: string;
  code: string;
  status: string;
  executionTime: number;
  memoryUsed: number;
  testCasesPassed: number;
  totalTestCases: number;
  submittedAt: string;
  testCaseResults: SolutionSubmission[];
}

export interface SolutionSubmission {
  id: string;
  testCaseId: string;
  testCaseNumber: number;
  status: string;
  output?: string;
  expectedOutput?: string;
  errorMessage?: string;
  executionTime: number;
  memoryUsed: number;
}

export interface SolutionFilter {
  questionId?: string;
  language?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

export interface SolutionStatistics {
  totalSubmissions: number;
  acceptedSolutions: number;
  questionsSolved: number;
  averageExecutionTime: number;
  averageMemoryUsed: number;
  solutionsByLanguage: Record<string, number>;
  solutionsByStatus: Record<string, number>;
}

export const solutionService = {
  async submitSolution(request: SubmitSolutionRequest): Promise<void> {
    await api.post('/solutions', request);
    // No return data - just success/error
  },

  async getMySolutions(filter?: SolutionFilter): Promise<{ solutions: UserSolution[]; totalCount: number; page: number; pageSize: number }> {
    const response = await api.get<{ solutions: UserSolution[]; totalCount: number; page: number; pageSize: number }>('/solutions/me', {
      params: filter,
    });
    return response.data;
  },

  async getSolutionById(id: string): Promise<UserSolution> {
    const response = await api.get<UserSolution>(`/solutions/${id}`);
    return response.data;
  },

  async getSolutionsForQuestion(questionId: string): Promise<UserSolution[]> {
    const response = await api.get<UserSolution[]>(`/solutions/question/${questionId}`);
    return response.data;
  },

  async hasSolvedQuestion(questionId: string): Promise<boolean> {
    try {
      const response = await api.get<{ solved: boolean }>(`/solutions/question/${questionId}/solved`);
      return response.data.solved;
    } catch (error) {
      // If error (401, 404, etc.), assume not solved
      return false;
    }
  },

  async getStatistics(): Promise<SolutionStatistics> {
    const response = await api.get<SolutionStatistics>('/solutions/statistics');
    return response.data;
  },
};

