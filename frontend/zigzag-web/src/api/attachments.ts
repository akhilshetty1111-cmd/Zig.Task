import { apiClient } from './client';
import type { Attachment } from '@/types/attachment';

export const attachmentsApi = {
  list: (taskId: string) => apiClient.get<Attachment[]>(`/tasks/${taskId}/attachments`).then((r) => r.data),

  upload: (taskId: string, file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    return apiClient
      .post<Attachment>(`/tasks/${taskId}/attachments`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then((r) => r.data);
  },

  delete: (taskId: string, attachmentId: string) =>
    apiClient.delete(`/tasks/${taskId}/attachments/${attachmentId}`).then(() => undefined),
};
