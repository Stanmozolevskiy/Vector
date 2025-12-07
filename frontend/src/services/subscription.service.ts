import api from './api';

export interface SubscriptionPlan {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  billingPeriod: string;
  features: string[];
  isPopular: boolean;
  stripePriceId?: string;
}

const subscriptionService = {
  getPlans: async (): Promise<SubscriptionPlan[]> => {
    const response = await api.get<SubscriptionPlan[]>('/subscriptions/plans');
    return response.data;
  },

  getPlanById: async (planId: string): Promise<SubscriptionPlan> => {
    const response = await api.get<SubscriptionPlan>(`/subscriptions/plans/${planId}`);
    return response.data;
  },
};

export default subscriptionService;

