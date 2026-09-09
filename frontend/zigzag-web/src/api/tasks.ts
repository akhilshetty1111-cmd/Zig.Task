import { apiClient } from './client';
import type { PagedResult, Task, TaskFilters, TaskHistoryEntry, TaskPriority, TaskStatus } from '@/types/task';

export interface CreateTaskPayload {
  projectId: string;
  title: string;
  description?: string;
  priority?: TaskPriority;
  assignedToUserId?: string;
  dueDate?: string;
}

export interface UpdateTaskPayload {
  title: string;
  description?: string;
  dueDate?: string | null;
}

export const tasksApi = {
  list: (projectId: string, filters: TaskFilters = {}) =>
    apiClient
      .get<PagedResult<Task>>('/tasks', { params: { projectId, ...filters } })
      .then((r) => r.data),

  getById: (id: string) => apiClient.get<Task>(`/tasks/${id}`).then((r) => r.data),

  create: (payload: CreateTaskPayload) => apiClient.post<Task>('/tasks', payload).then((r) => r.data),

  update: (id: string, payload: UpdateTaskPayload) =>
    apiClient.put<Task>(`/tasks/${id}`, payload).then((r) => r.data),

  delete: (id: string) => apiClient.delete(`/tasks/${id}`).then(() => undefined),

  changeStatus: (id: string, status: TaskStatus) =>
    apiClient.patch<Task>(`/tasks/${id}/status`, { status }).then((r) => r.data),

  changeAssignee: (id: string, assignedToUserId: string | null) =>
    apiClient.patch<Task>(`/tasks/${id}/assignee`, { assignedToUserId }).then((r) => r.data),

  changePriority: (id: string, priority: TaskPriority) =>
    apiClient.patch<Task>(`/tasks/${id}/priority`, { priority }).then((r) => r.data),

  getHistory: (id: string) => apiClient.get<TaskHistoryEntry[]>(`/tasks/${id}/history`).then((r) => r.data),
};
