import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import {
  Avatar,
  Box,
  Breadcrumbs,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Divider,
  IconButton,
  Link,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
// MUI 6.3.0 still ships the legacy Grid as the default `Grid` export; the
// `size={{ xs, md }}` API used throughout this file lives on Grid2.
import Grid from '@mui/material/Grid2';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import { useProject, useProjectMembers } from '@/features/projects/useProjects';
import {
  useChangeTaskAssignee,
  useChangeTaskPriority,
  useChangeTaskStatus,
  useDeleteTask,
  useTask,
  useTaskHistory,
  useUpdateTask,
} from './useTasks';
import { useAddComment, useComments, useDeleteComment } from '@/features/comments/useComments';
import { useAuth } from '@/features/auth/AuthContext';
import { PriorityChip, StatusChip } from '@/components/StatusPriorityChips';
import { EmptyState, ErrorState, LoadingState } from '@/components/StateViews';
import { formatDateTime, formatRelativeTime } from '@/utils/formatRelativeTime';
import { TASK_PRIORITIES, TASK_STATUSES, STATUS_LABELS, type TaskStatus } from '@/types/task';
import { ApiError } from '@/api/client';
import type { ProjectMember } from '@/types/project';

const FIELD_LABELS: Record<string, string> = {
  status: 'status',
  priority: 'priority',
  assigned_to: 'assignee',
};

/**
 * task_history stores raw values (the PascalCase enum name, or a bare user
 * GUID for assigned_to) - this turns them back into what a person actually
 * reads: "In Progress" instead of "InProgress", a name instead of a GUID.
 */
function formatHistoryValue(fieldName: string, value: string | null, members: ProjectMember[] | undefined): string {
  if (value === null || value === '') return 'Unassigned';

  if (fieldName === 'status' && value in STATUS_LABELS) {
    return STATUS_LABELS[value as TaskStatus];
  }

  if (fieldName === 'assigned_to') {
    return members?.find((m) => m.userId === value)?.name ?? 'a former member';
  }

  return value;
}

export function TaskDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { data: task, isPending, isError, error, refetch } = useTask(id);
  const { data: project } = useProject(task?.projectId);
  const { data: members } = useProjectMembers(task?.projectId);
  const { data: comments } = useComments(id);
  const { data: history } = useTaskHistory(id);

  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [commentText, setCommentText] = useState('');

  const changeStatus = useChangeTaskStatus(task?.projectId ?? '');
  const changePriority = useChangeTaskPriority(task?.projectId ?? '', id ?? '');
  const changeAssignee = useChangeTaskAssignee(task?.projectId ?? '', id ?? '');
  const deleteTask = useDeleteTask(task?.projectId ?? '');
  const addComment = useAddComment(id ?? '');
  const deleteComment = useDeleteComment(id ?? '');

  if (isPending) return <LoadingState label="Loading task…" />;
  if (isError) {
    return <ErrorState message={error instanceof ApiError ? error.message : 'Could not load this task.'} onRetry={() => refetch()} />;
  }

  const handleDelete = async () => {
    await deleteTask.mutateAsync(task.id);
    navigate(`/projects/${task.projectId}`);
  };

  const handleAddComment = async () => {
    if (!commentText.trim()) return;
    await addComment.mutateAsync(commentText.trim());
    setCommentText('');
  };

  return (
    <Box sx={{ maxWidth: 960, mx: 'auto' }}>
      <Breadcrumbs sx={{ mb: 1 }}>
        <Link component="button" variant="body2" onClick={() => navigate('/projects')} underline="hover">
          Projects
        </Link>
        <Link component="button" variant="body2" onClick={() => navigate(`/projects/${task.projectId}`)} underline="hover">
          {project?.name ?? '…'}
        </Link>
        <Typography variant="body2" color="text.primary">
          Task
        </Typography>
      </Breadcrumbs>

      <Stack direction="row" justifyContent="space-between" alignItems="flex-start" sx={{ mb: 2 }}>
        <Typography variant="h1" sx={{ flexGrow: 1 }}>
          {task.title}
        </Typography>
        <Stack direction="row" spacing={0.5}>
          <Tooltip title="Edit task">
            <IconButton onClick={() => setEditOpen(true)}>
              <EditOutlinedIcon />
            </IconButton>
          </Tooltip>
          <Tooltip title="Delete task">
            <IconButton color="error" onClick={() => setDeleteOpen(true)}>
              <DeleteOutlineIcon />
            </IconButton>
          </Tooltip>
        </Stack>
      </Stack>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 8 }}>
          <Paper variant="outlined" sx={{ p: 2.5, mb: 3 }}>
            <Typography variant="h3" sx={{ mb: 1 }}>
              Description
            </Typography>
            <Typography variant="body2" color={task.description ? 'text.primary' : 'text.secondary'} sx={{ whiteSpace: 'pre-wrap' }}>
              {task.description || 'No description provided.'}
            </Typography>
          </Paper>

          <Paper variant="outlined" sx={{ p: 2.5, mb: 3 }}>
            <Typography variant="h3" sx={{ mb: 2 }}>
              Comments
            </Typography>
            <Stack spacing={2} sx={{ mb: 2.5 }}>
              {!comments || comments.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  No comments yet.
                </Typography>
              ) : (
                comments.map((comment) => (
                  <Stack key={comment.id} direction="row" spacing={1.5}>
                    <Avatar sx={{ width: 32, height: 32, fontSize: 13, bgcolor: 'primary.main' }}>
                      {comment.userName.charAt(0).toUpperCase()}
                    </Avatar>
                    <Box sx={{ flexGrow: 1 }}>
                      <Stack direction="row" alignItems="center" spacing={1}>
                        <Typography variant="body2" fontWeight={600}>
                          {comment.userName}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {formatRelativeTime(comment.createdAt)}
                        </Typography>
                        {(comment.userId === user?.id) && (
                          <IconButton size="small" onClick={() => deleteComment.mutate(comment.id)} sx={{ ml: 'auto' }}>
                            <DeleteOutlineIcon fontSize="inherit" />
                          </IconButton>
                        )}
                      </Stack>
                      <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                        {comment.text}
                      </Typography>
                    </Box>
                  </Stack>
                ))
              )}
            </Stack>
            <Stack direction="row" spacing={1.5}>
              <TextField
                placeholder="Add a comment…"
                fullWidth
                size="small"
                multiline
                maxRows={4}
                value={commentText}
                onChange={(e) => setCommentText(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    handleAddComment();
                  }
                }}
              />
              <Button variant="contained" onClick={handleAddComment} disabled={addComment.isPending || !commentText.trim()}>
                Post
              </Button>
            </Stack>
          </Paper>

          <Paper variant="outlined" sx={{ p: 2.5 }}>
            <Typography variant="h3" sx={{ mb: 1.5 }}>
              Activity history
            </Typography>
            {!history || history.length === 0 ? (
              <EmptyState title="No activity yet" description="Changes to this task will show up here." />
            ) : (
              <Stack spacing={1.5} divider={<Divider flexItem />}>
                {history.map((entry) => {
                  const formatValue = (value: string | null) => formatHistoryValue(entry.fieldName, value, members);
                  return (
                    <Stack key={entry.id} direction="row" justifyContent="space-between" alignItems="center">
                      <Typography variant="body2">
                        <strong>{entry.changedByName}</strong> changed <strong>{FIELD_LABELS[entry.fieldName] ?? entry.fieldName}</strong>
                        {entry.oldValue && ` from "${formatValue(entry.oldValue)}"`} to &quot;{formatValue(entry.newValue)}&quot;
                      </Typography>
                      <Typography variant="caption" color="text.secondary" sx={{ whiteSpace: 'nowrap', ml: 2 }}>
                        {formatDateTime(entry.changedAt)}
                      </Typography>
                    </Stack>
                  );
                })}
              </Stack>
            )}
          </Paper>
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <Paper variant="outlined" sx={{ p: 2.5 }}>
            <Stack spacing={2.5}>
              <TextField
                select
                label="Status"
                size="small"
                fullWidth
                value={task.status}
                onChange={(e) => changeStatus.mutate({ taskId: task.id, status: e.target.value as typeof task.status })}
              >
                {TASK_STATUSES.map((s) => (
                  <MenuItem key={s} value={s}>
                    {STATUS_LABELS[s]}
                  </MenuItem>
                ))}
              </TextField>

              <TextField
                select
                label="Priority"
                size="small"
                fullWidth
                value={task.priority}
                onChange={(e) => changePriority.mutate(e.target.value as typeof task.priority)}
              >
                {TASK_PRIORITIES.map((p) => (
                  <MenuItem key={p} value={p}>
                    {p}
                  </MenuItem>
                ))}
              </TextField>

              <TextField
                select
                label="Assignee"
                size="small"
                fullWidth
                value={task.assignedToUserId ?? ''}
                onChange={(e) => changeAssignee.mutate(e.target.value || null)}
              >
                <MenuItem value="">Unassigned</MenuItem>
                {members?.map((m) => (
                  <MenuItem key={m.userId} value={m.userId}>
                    {m.name}
                  </MenuItem>
                ))}
              </TextField>

              <Divider />

              <Stack direction="row" spacing={1} alignItems="center">
                <Typography variant="body2" color="text.secondary" sx={{ width: 90 }}>
                  Status
                </Typography>
                <StatusChip status={task.status} />
              </Stack>
              <Stack direction="row" spacing={1} alignItems="center">
                <Typography variant="body2" color="text.secondary" sx={{ width: 90 }}>
                  Priority
                </Typography>
                <PriorityChip priority={task.priority} />
              </Stack>
              <Stack spacing={0.5}>
                <Typography variant="caption" color="text.secondary">
                  Created by {task.createdByName}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {formatRelativeTime(task.createdAt)}
                </Typography>
              </Stack>
            </Stack>
          </Paper>
        </Grid>
      </Grid>

      <EditTaskDialog open={editOpen} onClose={() => setEditOpen(false)} task={task} />

      <Dialog open={deleteOpen} onClose={() => setDeleteOpen(false)}>
        <DialogTitle>Delete this task?</DialogTitle>
        <DialogContent>
          <DialogContentText>This permanently deletes &quot;{task.title}&quot; and cannot be undone.</DialogContentText>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button onClick={() => setDeleteOpen(false)}>Cancel</Button>
          <Button color="error" variant="contained" onClick={handleDelete} disabled={deleteTask.isPending}>
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

const editSchema = z.object({
  title: z.string().min(1, 'Title is required').max(300),
  description: z.string().max(4000).optional(),
  dueDate: z.string().optional(),
});

type EditFormValues = z.infer<typeof editSchema>;

function EditTaskDialog({
  open,
  onClose,
  task,
}: {
  open: boolean;
  onClose: () => void;
  task: { id: string; projectId: string; title: string; description: string | null; dueDate: string | null };
}) {
  const updateTask = useUpdateTask(task.projectId, task.id);
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<EditFormValues>({
    resolver: zodResolver(editSchema),
    values: { title: task.title, description: task.description ?? '', dueDate: task.dueDate ?? '' },
  });

  const onSubmit = async (values: EditFormValues) => {
    await updateTask.mutateAsync({
      title: values.title,
      description: values.description || undefined,
      dueDate: values.dueDate || null,
    });
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Edit task</DialogTitle>
      <Stack component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
        <DialogContent>
          <Stack spacing={2.5}>
            <TextField label="Title" fullWidth error={!!errors.title} helperText={errors.title?.message} {...register('title')} />
            <TextField label="Description" fullWidth multiline minRows={3} {...register('description')} />
            <TextField
              label="Due date"
              type="date"
              fullWidth
              slotProps={{ inputLabel: { shrink: true } }}
              {...register('dueDate')}
            />
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button onClick={onClose}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={updateTask.isPending}>
            Save changes
          </Button>
        </DialogActions>
      </Stack>
    </Dialog>
  );
}
