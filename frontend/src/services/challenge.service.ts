import api from './api';

export interface DailyChallenge {
  id: string;
  date: string;
  questionId: string;
  difficulty: string;
  category: string;
  attemptCount: number;
  completionCount: number;
  isActive: boolean;
  question: {
    id: string;
    title: string;
    description: string;
    difficulty: string;
    category: string;
    questionType: string;
    tags: string[];
    acceptanceRate?: number;
  };
}

export interface UserChallengeAttempt {
  id: string;
  userId: string;
  challengeId: string;
  startedAt: string;
  completedAt?: string;
  isCompleted: boolean;
  timeSpentSeconds?: number;
  language?: string;
  code?: string;
  testCasesPassed: number;
  totalTestCases: number;
  coinsEarned: number;
  challenge: DailyChallenge;
}

export interface ChallengeStats {
  completedChallenges: number;
  totalAttempts: number;
  completionRate: number;
  totalCoinsEarned: number;
  averageCompletionTimeSeconds: number;
  currentStreak: number;
  longestStreak: number;
}

export interface DailyChallengeResponse {
  challenge: DailyChallenge;
  userAttempt?: UserChallengeAttempt;
}

export interface CompleteChallengeRequest {
  code: string;
  language: string;
  testCasesPassed: number;
  totalTestCases: number;
}

const challengeService = {
  async getDailyChallenge(date?: string): Promise<DailyChallengeResponse> {
    const params = date ? { date } : {};
    const response = await api.get<DailyChallengeResponse>('/challenges/daily', { params });
    return response.data;
  },

  async getPastChallenges(limit: number = 7): Promise<DailyChallenge[]> {
    const response = await api.get<DailyChallenge[]>('/challenges/past', {
      params: { limit },
    });
    return response.data;
  },

  async startChallenge(challengeId: string): Promise<UserChallengeAttempt> {
    const response = await api.post<UserChallengeAttempt>(`/challenges/${challengeId}/start`);
    return response.data;
  },

  async completeChallenge(
    challengeId: string,
    request: CompleteChallengeRequest
  ): Promise<UserChallengeAttempt> {
    const response = await api.post<UserChallengeAttempt>(
      `/challenges/${challengeId}/complete`,
      request
    );
    return response.data;
  },

  async getChallengeHistory(limit: number = 30): Promise<UserChallengeAttempt[]> {
    const response = await api.get<UserChallengeAttempt[]>('/challenges/history', {
      params: { limit },
    });
    return response.data;
  },

  async getChallengeStats(): Promise<ChallengeStats> {
    const response = await api.get<ChallengeStats>('/challenges/stats');
    return response.data;
  },
};

export default challengeService;
