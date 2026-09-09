import { useNavigate } from 'react-router-dom';
import { useDraggable } from '@dnd-kit/core';
import { CSS } from '@dnd-kit/utilities';
import { Avatar, Card, CardContent, Stack, Tooltip, Typography } from '@mui/material';
import EventOutlinedIcon from '@mui/icons-material/EventOutlined';
import type { Task } from '@/types/task';
import { PriorityChip } from '@/components/StatusPriorityChips';
import { formatDate } from '@/utils/formatRelativeTime';

export function TaskCard({ task }: { task: Task }) {
  const navigate = useNavigate();
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({ id: task.id });

  const overdue = task.dueDate && task.status !== 'Done' && new Date(task.dueDate) < new Date(new Date().toDateString());

  return (
    <Card
      ref={setNodeRef}
      variant="outlined"
      {...attributes}
      {...listeners}
      onClick={() => navigate(`/tasks/${task.id}`)}
      sx={{
        mb: 1.5,
        cursor: 'grab',
        opacity: isDragging ? 0.4 : 1,
        transform: CSS.Translate.toString(transform),
        '&:hover': { borderColor: 'primary.main' },
      }}
    >
      <CardContent sx={{ p: 1.5, '&:last-child': { pb: 1.5 } }}>
        <Typography variant="body2" fontWeight={600} sx={{ mb: 1 }}>
          {task.title}
        </Typography>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Stack direction="row" spacing={0.75} alignItems="center">
            <PriorityChip priority={task.priority} />
            {task.dueDate && (
              <Tooltip title={overdue ? 'Overdue' : 'Due date'}>
                <Stack direction="row" spacing={0.25} alignItems="center" sx={{ color: overdue ? 'error.main' : 'text.secondary' }}>
                  <EventOutlinedIcon sx={{ fontSize: 14 }} />
                  <Typography variant="caption">{formatDate(task.dueDate)}</Typography>
                </Stack>
              </Tooltip>
            )}
          </Stack>
          {task.assignedToName && (
            <Tooltip title={task.assignedToName}>
              <Avatar sx={{ width: 24, height: 24, fontSize: 12, bgcolor: 'primary.main' }}>
                {task.assignedToName.charAt(0).toUpperCase()}
              </Avatar>
            </Tooltip>
          )}
        </Stack>
      </CardContent>
    </Card>
  );
}
