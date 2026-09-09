export interface Notification {
  id: string;
  type: string;
  title: string;
  message: string | null;
  taskId: string | null;
  projectId: string | null;
  isRead: boolean;
  createdAt: string;
}
