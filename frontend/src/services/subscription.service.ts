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

export interface Subscription {
  id: string;
  planType: string;
  status: string;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  price: number;
  currency: string;
  createdAt: string;
  cancelledAt?: string;
  plan?: SubscriptionPlan;
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

  getCurrentSubscription: async (): Promise<Subscription> => {
    const response = await api.get<Subscription>('/subscriptions/me');
    return response.data;
  },

  updateSubscription: async (planId: string): Promise<Subscription> => {
    const response = await api.put<Subscription>('/subscriptions/update', { planId });
    return response.data;
  },

  cancelSubscription: async (): Promise<void> => {
    await api.put('/subscriptions/cancel');
  },

  getInvoices: async (): Promise<any[]> => {
    const response = await api.get<any[]>('/subscriptions/invoices');
    return response.data;
  },
};

export default subscriptionService;

