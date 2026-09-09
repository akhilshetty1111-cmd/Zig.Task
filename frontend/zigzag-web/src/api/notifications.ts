import { apiClient } from './client';
import type { Notification } from '@/types/notification';

export const notificationsApi = {
  list: (unreadOnly = false) =>
    apiClient.get<Notification[]>('/notifications', { params: { unreadOnly } }).then((r) => r.data),

  markRead: (id: string) => apiClient.patch(`/notifications/${id}/read`).then(() => undefined),
};
