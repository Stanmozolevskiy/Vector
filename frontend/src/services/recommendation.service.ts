import api from './api';

export interface RecommendedQuestion {
  id: string;
  title: string;
  description: string;
  difficulty: string;
  category: string;
  questionType: string;
  tags: string[];
  acceptanceRate?: number;
}

export interface PersonalizedSet {
  recommendedDifficulty: string;
  weakCategories: string[];
  recommendedQuestions: RecommendedQuestion[];
  weakAreaQuestions: RecommendedQuestion[];
  analytics: {
    totalSolved: number;
    easySolved: number;
    mediumSolved: number;
    hardSolved: number;
    accuracyRate: number;
  };
}

const recommendationService = {
  async getRecommendations(limit: number = 10): Promise<RecommendedQuestion[]> {
    const response = await api.get<RecommendedQuestion[]>('/challenges/recommendations', {
      params: { limit },
    });
    return response.data;
  },

  async getPersonalizedSet(): Promise<PersonalizedSet> {
    const response = await api.get<PersonalizedSet>('/challenges/personalized');
    return response.data;
  },

  async getWeakAreaQuestions(limit: number = 5): Promise<RecommendedQuestion[]> {
    const response = await api.get<RecommendedQuestion[]>('/challenges/weak-areas', {
      params: { limit },
    });
    return response.data;
  },
};

export default recommendationService;
