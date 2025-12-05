import api from './api';

export interface CoachApplication {
  id: string;
  userId: string;
  userEmail: string;
  userName: string;
  motivation: string;
  experience?: string;
  specialization?: string;
  imageUrls?: string[];
  status: 'pending' | 'approved' | 'rejected';
  adminNotes?: string;
  reviewedBy?: string;
  reviewerName?: string;
  reviewedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface SubmitCoachApplicationDto {
  motivation: string;
  experience?: string;
  specialization?: string;
  imageUrls?: string[];
}

export interface ReviewCoachApplicationDto {
  status: 'approved' | 'rejected';
  adminNotes?: string;
}

export const coachService = {
  // Submit a coach application
  submitApplication: async (data: SubmitCoachApplicationDto): Promise<CoachApplication> => {
    const response = await api.post<CoachApplication>('/coach/apply', data);
    return response.data;
  },

  // Get current user's application
  getMyApplication: async (): Promise<CoachApplication | null> => {
    try {
      const response = await api.get<CoachApplication>('/coach/my-application');
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 404) {
        return null;
      }
      throw error;
    }
  },

  // Admin: Get all coach applications
  getAllApplications: async (): Promise<CoachApplication[]> => {
    const response = await api.get<{ applications: CoachApplication[] }>('/admin/coach-applications');
    return response.data.applications;
  },

  // Admin: Get pending coach applications
  getPendingApplications: async (): Promise<CoachApplication[]> => {
    const response = await api.get<{ applications: CoachApplication[] }>('/admin/coach-applications/pending');
    return response.data.applications;
  },

  // Admin: Review (approve/reject) application
  reviewApplication: async (applicationId: string, data: ReviewCoachApplicationDto): Promise<CoachApplication> => {
    const response = await api.post<CoachApplication>(`/admin/coach-applications/${applicationId}/review`, data);
    return response.data;
  },

  // Upload image for coach application
  uploadImage: async (file: File): Promise<string> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post<{ imageUrl: string }>('/coach/upload-image', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    console.log('Upload response:', response.data);
    const imageUrl = response.data?.imageUrl || response.data;
    if (!imageUrl || typeof imageUrl !== 'string') {
      throw new Error('Invalid response format from server');
    }
    return imageUrl;
  },
};

