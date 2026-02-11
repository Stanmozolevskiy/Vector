import api from './api';

export interface UserCoins {
  userId: string;
  totalCoins: number;
  displayCoins: string;
  rank: number | null;
  displayRank: string | null;
}

export interface CoinTransaction {
  id: string;
  amount: number;
  activityType: string;
  description: string;
  createdAt: string;
  timeAgo: string;
}

export interface LeaderboardEntry {
  rank: number;
  userId: string;
  firstName: string;
  lastName: string;
  profilePictureUrl: string | null;
  totalCoins: number;
  displayCoins: string;
}

export interface AchievementDefinition {
  activityType: string;
  displayName: string;
  description: string | null;
  coinsAwarded: number;
  icon: string | null;
}

class CoinsService {
  /**
   * Get current user's coins and rank
   */
  async getMyCoins(): Promise<UserCoins> {
    const response = await api.get('/coins/me');
    return response.data;
  }

  /**
   * Get specific user's coins (public endpoint)
   */
  async getUserCoins(userId: string): Promise<UserCoins> {
    const response = await api.get(`/coins/user/${userId}`);
    return response.data;
  }

  /**
   * Get current user's transaction history (paginated)
   */
  async getMyTransactions(page: number = 1, pageSize: number = 50): Promise<CoinTransaction[]> {
    const response = await api.get('/coins/me/transactions', {
      params: { page, pageSize }
    });
    return response.data;
  }

  /**
   * Get leaderboard (top N users by coins)
   */
  async getLeaderboard(limit: number = 200): Promise<LeaderboardEntry[]> {
    const response = await api.get('/coins/leaderboard', {
      params: { limit }
    });
    return response.data;
  }

  /**
   * Get current user's rank
   */
  async getMyRank(): Promise<{ rank: number | null; displayRank: string | null }> {
    const response = await api.get('/coins/me/rank');
    return response.data;
  }

  /**
   * Get all ways to earn coins (achievement definitions)
   */
  async getAchievements(): Promise<AchievementDefinition[]> {
    const response = await api.get('/coins/achievements');
    return response.data;
  }
}

export default new CoinsService();
