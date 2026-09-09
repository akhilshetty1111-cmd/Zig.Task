import { DndContext, PointerSensor, useSensor, useSensors, type DragEndEvent } from '@dnd-kit/core';
import { Box, Stack } from '@mui/material';
import { TASK_STATUSES, type TaskStatus } from '@/types/task';
import { useChangeTaskStatus, useTasks } from '@/features/tasks/useTasks';
import { BoardColumn } from './BoardColumn';
import { ErrorState, LoadingState } from '@/components/StateViews';
import { ApiError } from '@/api/client';

export function BoardView({ projectId }: { projectId: string }) {
  const { data, isPending, isError, error, refetch } = useTasks(projectId, {
    // 100, not "unbounded": GetTasksQueryValidator caps pageSize at 100 on the
    // backend. Confirmed the hard way - an earlier value of 200 here made
    // every single board load fail its own request validation. A project
    // with more tasks than this needs real column-level pagination, which is
    // future work; 100 is a reasonable ceiling for now.
    pageSize: 100,
    sortBy: 'CreatedAt',
    sortDescending: false,
  });
  const changeStatus = useChangeTaskStatus(projectId);

  // A small activation distance so a click-through to the task detail page
  // still works - without it, every click is interpreted as a drag start.
  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));

  if (isPending) return <LoadingState label="Loading board…" />;
  if (isError) {
    return (
      <ErrorState message={error instanceof ApiError ? error.message : 'Could not load the board.'} onRetry={() => refetch()} />
    );
  }

  const tasksByStatus = (status: TaskStatus) => data.items.filter((t) => t.status === status);

  const handleDragEnd = (event: DragEndEvent) => {
    const taskId = event.active.id as string;
    const newStatus = event.over?.id as TaskStatus | undefined;
    if (!newStatus) return;

    const task = data.items.find((t) => t.id === taskId);
    if (!task || task.status === newStatus) return;

    changeStatus.mutate({ taskId, status: newStatus });
  };

  return (
    <DndContext sensors={sensors} onDragEnd={handleDragEnd}>
      <Box sx={{ overflowX: 'auto', pb: 1 }}>
        <Stack direction="row" spacing={2} sx={{ minWidth: 'min-content' }}>
          {TASK_STATUSES.map((status) => (
            <BoardColumn key={status} status={status} tasks={tasksByStatus(status)} />
          ))}
        </Stack>
      </Box>
    </DndContext>
  );
}
