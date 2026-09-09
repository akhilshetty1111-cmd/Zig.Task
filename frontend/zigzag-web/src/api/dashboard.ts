import { apiClient } from './client';
import type { Dashboard } from '@/types/dashboard';

export const dashboardApi = {
  get: () => apiClient.get<Dashboard>('/dashboard').then((r) => r.data),
};
