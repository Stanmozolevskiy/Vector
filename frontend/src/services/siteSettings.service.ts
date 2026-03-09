import axios from 'axios';

const baseURL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export interface DashboardVideoSettings {
  url: string;
  title: string;
  description: string;
}

/** Public API - no auth required */
export const siteSettingsService = {
  getDashboardVideo: async (): Promise<DashboardVideoSettings> => {
    const { data } = await axios.get<DashboardVideoSettings>(
      `${baseURL}/SiteSettings/dashboard-video`
    );
    return data;
  },
};
