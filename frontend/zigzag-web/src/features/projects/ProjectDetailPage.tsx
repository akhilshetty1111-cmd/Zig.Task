import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Breadcrumbs,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Link,
  Stack,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import ArchiveOutlinedIcon from '@mui/icons-material/ArchiveOutlined';
import { useArchiveProject, useProject } from './useProjects';
import { BoardView } from '@/features/board/BoardView';
import { TaskListView } from '@/features/tasks/TaskListView';
import { TaskFormDialog } from '@/features/tasks/TaskFormDialog';
import { MembersView } from './MembersView';
import { ErrorState, LoadingState } from '@/components/StateViews';
import { ApiError } from '@/api/client';

type ProjectTab = 'board' | 'tasks' | 'members';

export function ProjectDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: project, isPending, isError, error, refetch } = useProject(id);
  const archiveProject = useArchiveProject();
  const [tab, setTab] = useState<ProjectTab>('board');
  const [taskDialogOpen, setTaskDialogOpen] = useState(false);
  const [archiveDialogOpen, setArchiveDialogOpen] = useState(false);

  if (isPending) return <LoadingState label="Loading project…" />;
  if (isError) {
    return (
      <ErrorState message={error instanceof ApiError ? error.message : 'Could not load this project.'} onRetry={() => refetch()} />
    );
  }

  const handleArchive = async () => {
    await archiveProject.mutateAsync(project.id);
    setArchiveDialogOpen(false);
    navigate('/projects');
  };

  return (
    <Box>
      <Breadcrumbs sx={{ mb: 1 }}>
        <Link component="button" variant="body2" onClick={() => navigate('/projects')} underline="hover">
          Projects
        </Link>
        <Typography variant="body2" color="text.primary">
          {project.name}
        </Typography>
      </Breadcrumbs>

      <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ sm: 'center' }} spacing={1.5} sx={{ mb: 2 }}>
        <Stack direction="row" alignItems="center" spacing={1.5}>
          <Typography variant="h1">{project.name}</Typography>
          {project.isArchived && <Chip label="Archived" size="small" />}
        </Stack>
        <Stack direction="row" spacing={1}>
          <Button variant="outlined" color="inherit" startIcon={<ArchiveOutlinedIcon />} onClick={() => setArchiveDialogOpen(true)} disabled={project.isArchived}>
            Archive
          </Button>
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setTaskDialogOpen(true)}>
            New task
          </Button>
        </Stack>
      </Stack>

      {project.description && (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {project.description}
        </Typography>
      )}

      <Tabs value={tab} onChange={(_e, value) => setTab(value)} sx={{ mb: 2.5, borderBottom: 1, borderColor: 'divider' }}>
        <Tab label="Board" value="board" />
        <Tab label="Tasks" value="tasks" />
        <Tab label="Members" value="members" />
      </Tabs>

      {tab === 'board' && <BoardView projectId={project.id} />}
      {tab === 'tasks' && <TaskListView projectId={project.id} />}
      {tab === 'members' && <MembersView projectId={project.id} />}

      <TaskFormDialog projectId={project.id} open={taskDialogOpen} onClose={() => setTaskDialogOpen(false)} />

      <Dialog open={archiveDialogOpen} onClose={() => setArchiveDialogOpen(false)}>
        <DialogTitle>Archive this project?</DialogTitle>
        <DialogContent>
          <DialogContentText>
            &quot;{project.name}&quot; will be hidden from your project list. Its tasks, comments and history are kept and
            nothing is deleted.
          </DialogContentText>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button onClick={() => setArchiveDialogOpen(false)}>Cancel</Button>
          <Button color="warning" variant="contained" onClick={handleArchive} disabled={archiveProject.isPending}>
            Archive
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
