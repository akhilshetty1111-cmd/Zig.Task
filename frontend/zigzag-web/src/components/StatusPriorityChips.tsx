import Chip from '@mui/material/Chip';
import type { TaskPriority, TaskStatus } from '@/types/task';
import { STATUS_LABELS } from '@/types/task';

const STATUS_COLOR: Record<TaskStatus, 'default' | 'info' | 'warning' | 'success' | 'error'> = {
  Todo: 'default',
  InProgress: 'info',
  InReview: 'warning',
  Done: 'success',
  Blocked: 'error',
};

const PRIORITY_COLOR: Record<TaskPriority, 'default' | 'info' | 'warning' | 'error'> = {
  Low: 'default',
  Medium: 'info',
  High: 'warning',
  Urgent: 'error',
};

export function StatusChip({ status, size = 'small' }: { status: TaskStatus; size?: 'small' | 'medium' }) {
  return <Chip label={STATUS_LABELS[status]} color={STATUS_COLOR[status]} size={size} variant="outlined" />;
}

export function PriorityChip({ priority, size = 'small' }: { priority: TaskPriority; size?: 'small' | 'medium' }) {
  return <Chip label={priority} color={PRIORITY_COLOR[priority]} size={size} />;
}
