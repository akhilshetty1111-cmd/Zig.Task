import { apiClient } from './client';
import type { AuthResponse, User } from '@/types/auth';

export interface RegisterPayload {
  name: string;
  email: string;
  password: string;
}

export interface LoginPayload {
  email: string;
  password: string;
}

export const authApi = {
  register: (payload: RegisterPayload) =>
    apiClient.post<AuthResponse>('/auth/register', payload).then((r) => r.data),

  login: (payload: LoginPayload) =>
    apiClient.post<AuthResponse>('/auth/login', payload).then((r) => r.data),

  logout: () => apiClient.post('/auth/logout').then(() => undefined),

  me: () => apiClient.get<User>('/auth/me').then((r) => r.data),
};
