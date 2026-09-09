import type { TaskPriority, TaskStatus } from './task';

export interface StatusCount {
  status: TaskStatus;
  count: number;
}

export interface PriorityCount {
  priority: TaskPriority;
  count: number;
}

export interface RecentActivity {
  id: string;
  taskId: string;
  taskTitle: string;
  changedByUserId: string;
  changedByName: string;
  fieldName: string;
  oldValue: string | null;
  newValue: string | null;
  changedAt: string;
}

export interface Dashboard {
  totalProjects: number;
  totalTasks: number;
  completedTasks: number;
  pendingTasks: number;
  overdueTasks: number;
  tasksAssignedToMe: number;
  tasksByStatus: StatusCount[];
  tasksByPriority: PriorityCount[];
  recentActivities: RecentActivity[];
}
