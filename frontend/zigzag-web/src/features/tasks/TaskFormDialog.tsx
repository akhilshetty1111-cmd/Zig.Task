import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  TextField,
} from '@mui/material';
import { useCreateTask } from './useTasks';
import { useProjectMembers } from '@/features/projects/useProjects';
import { TASK_PRIORITIES } from '@/types/task';

const schema = z.object({
  title: z.string().min(1, 'Title is required').max(300),
  description: z.string().max(4000).optional(),
  priority: z.enum(['Low', 'Medium', 'High', 'Urgent']),
  assignedToUserId: z.string().optional(),
  dueDate: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

export function TaskFormDialog({
  projectId,
  open,
  onClose,
}: {
  projectId: string;
  open: boolean;
  onClose: () => void;
}) {
  const createTask = useCreateTask(projectId);
  const { data: members } = useProjectMembers(open ? projectId : undefined);

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { priority: 'Medium', assignedToUserId: '', dueDate: '' },
  });

  const handleClose = () => {
    reset();
    onClose();
  };

  const onSubmit = async (values: FormValues) => {
    await createTask.mutateAsync({
      projectId,
      title: values.title,
      description: values.description || undefined,
      priority: values.priority,
      assignedToUserId: values.assignedToUserId || undefined,
      dueDate: values.dueDate || undefined,
    });
    handleClose();
  };

  return (
    <Dialog open={open} onClose={handleClose} fullWidth maxWidth="sm">
      <DialogTitle>New task</DialogTitle>
      <Stack component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
        <DialogContent>
          <Stack spacing={2.5}>
            <TextField
              label="Title"
              autoFocus
              fullWidth
              error={!!errors.title}
              helperText={errors.title?.message}
              {...register('title')}
            />
            <TextField
              label="Description"
              fullWidth
              multiline
              minRows={3}
              error={!!errors.description}
              helperText={errors.description?.message}
              {...register('description')}
            />
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
              <Controller
                name="priority"
                control={control}
                render={({ field }) => (
                  <TextField select label="Priority" fullWidth {...field}>
                    {TASK_PRIORITIES.map((p) => (
                      <MenuItem key={p} value={p}>
                        {p}
                      </MenuItem>
                    ))}
                  </TextField>
                )}
              />
              <TextField
                label="Due date"
                type="date"
                fullWidth
                slotProps={{ inputLabel: { shrink: true } }}
                {...register('dueDate')}
              />
            </Stack>
            <Controller
              name="assignedToUserId"
              control={control}
              render={({ field }) => (
                <TextField select label="Assignee" fullWidth {...field}>
                  <MenuItem value="">Unassigned</MenuItem>
                  {members?.map((m) => (
                    <MenuItem key={m.userId} value={m.userId}>
                      {m.name}
                    </MenuItem>
                  ))}
                </TextField>
              )}
            />
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button onClick={handleClose}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={createTask.isPending}>
            Create task
          </Button>
        </DialogActions>
      </Stack>
    </Dialog>
  );
}
