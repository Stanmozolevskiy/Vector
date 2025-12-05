import api from './api';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  emailVerified: boolean;
  createdAt: string;
  updatedAt: string;
}

export const adminService = {
  // Update user role
  updateUserRole: async (userId: string, role: string): Promise<void> => {
    await api.put(`/admin/users/${userId}/role`, { role });
  },

  // Delete user
  deleteUser: async (userId: string): Promise<void> => {
    await api.delete(`/admin/users/${userId}`);
  },
};

