export type TaskStatus = 'Todo' | 'InProgress' | 'InReview' | 'Done' | 'Blocked';
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Urgent';

export interface Task {
  id: string;
  projectId: string;
  title: string;
  description: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  assignedToUserId: string | null;
  assignedToName: string | null;
  createdByUserId: string;
  createdByName: string;
  dueDate: string | null;
  createdAt: string;
  updatedAt: string;
  completedAt: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface TaskHistoryEntry {
  id: string;
  changedByUserId: string;
  changedByName: string;
  fieldName: string;
  oldValue: string | null;
  newValue: string | null;
  changedAt: string;
}

export interface TaskFilters {
  status?: TaskStatus;
  priority?: TaskPriority;
  search?: string;
  assignedTo?: string;
  sortBy?: 'CreatedAt' | 'DueDate' | 'Priority' | 'Title';
  sortDescending?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

export const TASK_STATUSES: TaskStatus[] = ['Todo', 'InProgress', 'InReview', 'Done', 'Blocked'];
export const TASK_PRIORITIES: TaskPriority[] = ['Low', 'Medium', 'High', 'Urgent'];

export const STATUS_LABELS: Record<TaskStatus, string> = {
  Todo: 'To Do',
  InProgress: 'In Progress',
  InReview: 'In Review',
  Done: 'Done',
  Blocked: 'Blocked',
};
