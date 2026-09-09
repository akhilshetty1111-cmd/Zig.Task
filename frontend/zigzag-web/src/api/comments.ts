import { apiClient } from './client';
import type { Comment } from '@/types/comment';

export const commentsApi = {
  list: (taskId: string) => apiClient.get<Comment[]>(`/tasks/${taskId}/comments`).then((r) => r.data),

  add: (taskId: string, text: string) =>
    apiClient.post<Comment>(`/tasks/${taskId}/comments`, { text }).then((r) => r.data),

  delete: (taskId: string, commentId: string) =>
    apiClient.delete(`/tasks/${taskId}/comments/${commentId}`).then(() => undefined),
};
