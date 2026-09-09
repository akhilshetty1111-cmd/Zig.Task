import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { tasksApi, type CreateTaskPayload, type UpdateTaskPayload } from '@/api/tasks';
import type { PagedResult, Task, TaskFilters, TaskPriority, TaskStatus } from '@/types/task';
import { ApiError } from '@/api/client';

function useApiErrorSnackbar() {
  const { enqueueSnackbar } = useSnackbar();
  return (err: unknown, fallback: string) =>
    enqueueSnackbar(err instanceof ApiError ? err.message : fallback, { variant: 'error' });
}

export function useTasks(projectId: string | undefined, filters: TaskFilters) {
  return useQuery({
    queryKey: ['tasks', projectId, filters],
    queryFn: () => tasksApi.list(projectId!, filters),
    enabled: !!projectId,
    placeholderData: (previous) => previous,
  });
}

export function useTask(id: string | undefined) {
  return useQuery({
    queryKey: ['tasks', 'detail', id],
    queryFn: () => tasksApi.getById(id!),
    enabled: !!id,
  });
}

export function useTaskHistory(id: string | undefined) {
  return useQuery({
    queryKey: ['tasks', 'detail', id, 'history'],
    queryFn: () => tasksApi.getHistory(id!),
    enabled: !!id,
  });
}

function invalidateTaskQueries(queryClient: ReturnType<typeof useQueryClient>, projectId: string, taskId?: string) {
  queryClient.invalidateQueries({ queryKey: ['tasks', projectId] });
  if (taskId) queryClient.invalidateQueries({ queryKey: ['tasks', 'detail', taskId] });
  queryClient.invalidateQueries({ queryKey: ['dashboard'] });
}

export function useCreateTask(projectId: string) {
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();
  const showError = useApiErrorSnackbar();

  return useMutation({
    mutationFn: (payload: CreateTaskPayload) => tasksApi.create(payload),
    onSuccess: () => {
      invalidateTaskQueries(queryClient, projectId);
      enqueueSnackbar('Task created', { variant: 'success' });
    },
    onError: (err) => showError(err, 'Could not create the task.'),
  });
}

export function useUpdateTask(projectId: string, taskId: string) {
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();
  const showError = useApiErrorSnackbar();

  return useMutation({
    mutationFn: (payload: UpdateTaskPayload) => tasksApi.update(taskId, payload),
    onSuccess: () => {
      invalidateTaskQueries(queryClient, projectId, taskId);
      enqueueSnackbar('Task updated', { variant: 'success' });
    },
    onError: (err) => showError(err, 'Could not update the task.'),
  });
}

export function useDeleteTask(projectId: string) {
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();
  const showError = useApiErrorSnackbar();

  return useMutation({
    mutationFn: (taskId: string) => tasksApi.delete(taskId),
    onSuccess: () => {
      invalidateTaskQueries(queryClient, projectId);
      enqueueSnackbar('Task deleted', { variant: 'success' });
    },
    onError: (err) => showError(err, 'Could not delete the task.'),
  });
}

/**
 * Optimistic: the Kanban board must feel instant on drag-drop rather than
 * waiting a round trip before the card visually moves. Rolls back to the
 * previous cache snapshot if the request fails.
 */
export function useChangeTaskStatus(projectId: string) {
  const queryClient = useQueryClient();
  const showError = useApiErrorSnackbar();

  return useMutation({
    mutationFn: ({ taskId, status }: { taskId: string; status: TaskStatus }) => tasksApi.changeStatus(taskId, status),
    onMutate: async ({ taskId, status }) => {
      await queryClient.cancelQueries({ queryKey: ['tasks', projectId] });
      const previous = queryClient.getQueriesData<PagedResult<Task>>({ queryKey: ['tasks', projectId] });

      queryClient.setQueriesData<PagedResult<Task>>({ queryKey: ['tasks', projectId] }, (old) => {
        if (!old) return old;
        return { ...old, items: old.items.map((t) => (t.id === taskId ? { ...t, status } : t)) };
      });

      return { previous };
    },
    onError: (err, _vars, context) => {
      context?.previous.forEach(([key, data]) => queryClient.setQueryData<PagedResult<Task> | undefined>(key, data));
      showError(err, 'Could not move the task.');
    },
    onSettled: (_data, _err, { taskId }) => invalidateTaskQueries(queryClient, projectId, taskId),
  });
}

export function useChangeTaskAssignee(projectId: string, taskId: string) {
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();
  const showError = useApiErrorSnackbar();

  return useMutation({
    mutationFn: (assignedToUserId: string | null) => tasksApi.changeAssignee(taskId, assignedToUserId),
    onSuccess: () => {
      invalidateTaskQueries(queryClient, projectId, taskId);
      enqueueSnackbar('Assignee updated', { variant: 'success' });
    },
    onError: (err) => showError(err, 'Could not change the assignee.'),
  });
}

export function useChangeTaskPriority(projectId: string, taskId: string) {
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();
  const showError = useApiErrorSnackbar();

  return useMutation({
    mutationFn: (priority: TaskPriority) => tasksApi.changePriority(taskId, priority),
    onSuccess: () => {
      invalidateTaskQueries(queryClient, projectId, taskId);
      enqueueSnackbar('Priority updated', { variant: 'success' });
    },
    onError: (err) => showError(err, 'Could not change the priority.'),
  });
}
