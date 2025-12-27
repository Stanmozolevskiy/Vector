import api from './api';

export interface LearningAnalytics {
  userId: string;
  questionsSolved: number;
  questionsByCategory: Record<string, number>;
  questionsByDifficulty: Record<string, number>;
  averageExecutionTime: number;
  averageMemoryUsed: number;
  successRate: number;
  currentStreak: number;
  longestStreak: number;
  lastActivityDate: string | null;
  totalSubmissions: number;
  solutionsByLanguage: Record<string, number>;
}

export interface CategoryProgress {
  category: string;
  questionsSolved: number;
  totalQuestions: number;
  completionPercentage: number;
  averageExecutionTime: number;
  averageMemoryUsed: number;
}

export interface DifficultyProgress {
  difficulty: string;
  questionsSolved: number;
  totalQuestions: number;
  completionPercentage: number;
  averageExecutionTime: number;
  averageMemoryUsed: number;
}

class AnalyticsService {
  /**
   * Get current user's learning analytics
   */
  async getUserAnalytics(): Promise<LearningAnalytics> {
    const response = await api.get<LearningAnalytics>('/analytics/me');
    return response.data;
  }

  /**
   * Get progress for a specific category
   */
  async getCategoryProgress(category: string): Promise<CategoryProgress> {
    const response = await api.get<CategoryProgress>(`/analytics/category/${encodeURIComponent(category)}`);
    return response.data;
  }

  /**
   * Get progress for a specific difficulty level
   */
  async getDifficultyProgress(difficulty: string): Promise<DifficultyProgress> {
    const response = await api.get<DifficultyProgress>(`/analytics/difficulty/${encodeURIComponent(difficulty)}`);
    return response.data;
  }
}

export const analyticsService = new AnalyticsService();

