import { apiClient } from './client';
import type { Project, ProjectMember, ProjectRole } from '@/types/project';

export interface CreateProjectPayload {
  name: string;
  description?: string;
}

export interface UpdateProjectPayload {
  name: string;
  description?: string;
}

export interface AddMemberPayload {
  email: string;
  role: ProjectRole;
}

export const projectsApi = {
  list: (includeArchived = false) =>
    apiClient.get<Project[]>('/projects', { params: { includeArchived } }).then((r) => r.data),

  getById: (id: string) => apiClient.get<Project>(`/projects/${id}`).then((r) => r.data),

  create: (payload: CreateProjectPayload) =>
    apiClient.post<Project>('/projects', payload).then((r) => r.data),

  update: (id: string, payload: UpdateProjectPayload) =>
    apiClient.put<Project>(`/projects/${id}`, payload).then((r) => r.data),

  archive: (id: string) => apiClient.delete(`/projects/${id}`).then(() => undefined),

  getMembers: (id: string) => apiClient.get<ProjectMember[]>(`/projects/${id}/members`).then((r) => r.data),

  addMember: (id: string, payload: AddMemberPayload) =>
    apiClient.post<ProjectMember>(`/projects/${id}/members`, payload).then((r) => r.data),

  removeMember: (id: string, userId: string) =>
    apiClient.delete(`/projects/${id}/members/${userId}`).then(() => undefined),
};
