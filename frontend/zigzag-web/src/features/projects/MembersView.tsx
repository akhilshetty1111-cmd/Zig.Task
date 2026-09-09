import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import {
  Avatar,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  MenuItem,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import { useAddProjectMember, useProjectMembers, useRemoveProjectMember } from './useProjects';
import { LoadingState } from '@/components/StateViews';
import { useAuth } from '@/features/auth/AuthContext';
import type { ProjectRole } from '@/types/project';

const PROJECT_ROLES: ProjectRole[] = ['Viewer', 'Member', 'Manager', 'Owner'];

const schema = z.object({
  email: z.string().min(1, 'Email is required').email('Enter a valid email address'),
  role: z.enum(['Viewer', 'Member', 'Manager', 'Owner']),
});

type FormValues = z.infer<typeof schema>;

export function MembersView({ projectId }: { projectId: string }) {
  const { user } = useAuth();
  const { data: members, isPending } = useProjectMembers(projectId);
  const addMember = useAddProjectMember(projectId);
  const removeMember = useRemoveProjectMember(projectId);
  const [dialogOpen, setDialogOpen] = useState(false);

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { role: 'Member' } });

  const handleClose = () => {
    reset();
    setDialogOpen(false);
  };

  const onSubmit = async (values: FormValues) => {
    await addMember.mutateAsync(values);
    handleClose();
  };

  if (isPending) return <LoadingState label="Loading members…" />;

  return (
    <>
      <Stack direction="row" justifyContent="flex-end" sx={{ mb: 2 }}>
        <Button variant="outlined" size="small" startIcon={<AddIcon />} onClick={() => setDialogOpen(true)}>
          Add member
        </Button>
      </Stack>

      <List sx={{ border: 1, borderColor: 'divider', borderRadius: 1.5, py: 0 }}>
        {members?.map((member, index) => (
          <ListItem
            key={member.userId}
            divider={index < members.length - 1}
            secondaryAction={
              <Tooltip title="Remove from project">
                <IconButton
                  edge="end"
                  size="small"
                  onClick={() => removeMember.mutate(member.userId)}
                  disabled={removeMember.isPending}
                >
                  <DeleteOutlineIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            }
          >
            <ListItemAvatar>
              <Avatar sx={{ bgcolor: 'primary.main' }}>{member.name.charAt(0).toUpperCase()}</Avatar>
            </ListItemAvatar>
            <ListItemText
              primary={
                <Stack direction="row" spacing={1} alignItems="center">
                  <Typography variant="body2" fontWeight={600}>
                    {member.name}
                  </Typography>
                  {member.userId === user?.id && <Typography variant="caption" color="text.secondary">(you)</Typography>}
                </Stack>
              }
              secondary={`${member.email} · ${member.role}`}
            />
          </ListItem>
        ))}
      </List>

      <Dialog open={dialogOpen} onClose={handleClose} fullWidth maxWidth="sm">
        <DialogTitle>Add member</DialogTitle>
        <Stack component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
          <DialogContent>
            <Stack spacing={2.5}>
              <TextField
                label="Email"
                type="email"
                autoFocus
                fullWidth
                error={!!errors.email}
                helperText={errors.email?.message}
                {...register('email')}
              />
              <Controller
                name="role"
                control={control}
                render={({ field }) => (
                  <TextField select label="Role" fullWidth {...field}>
                    {PROJECT_ROLES.map((role) => (
                      <MenuItem key={role} value={role}>
                        {role}
                      </MenuItem>
                    ))}
                  </TextField>
                )}
              />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 3 }}>
            <Button onClick={handleClose}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={addMember.isPending}>
              Add member
            </Button>
          </DialogActions>
        </Stack>
      </Dialog>
    </>
  );
}
