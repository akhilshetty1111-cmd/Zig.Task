import { useDroppable } from '@dnd-kit/core';
import { Box, Chip, Paper, Stack, Typography } from '@mui/material';
import type { Task, TaskStatus } from '@/types/task';
import { STATUS_LABELS } from '@/types/task';
import { TaskCard } from './TaskCard';

export function BoardColumn({ status, tasks }: { status: TaskStatus; tasks: Task[] }) {
  const { setNodeRef, isOver } = useDroppable({ id: status });

  return (
    <Paper
      ref={setNodeRef}
      variant="outlined"
      sx={{
        width: 280,
        flexShrink: 0,
        p: 1.5,
        bgcolor: isOver ? 'action.hover' : 'background.paper',
        transition: 'background-color 0.15s',
        display: 'flex',
        flexDirection: 'column',
      }}
    >
      <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 1.5, px: 0.5 }}>
        <Typography variant="h3">{STATUS_LABELS[status]}</Typography>
        <Chip label={tasks.length} size="small" />
      </Stack>
      <Box sx={{ minHeight: 40, flexGrow: 1 }}>
        {tasks.map((task) => (
          <TaskCard key={task.id} task={task} />
        ))}
      </Box>
    </Paper>
  );
}
