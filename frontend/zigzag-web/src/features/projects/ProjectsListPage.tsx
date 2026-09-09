import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  Chip,
  Stack,
  Typography,
} from '@mui/material';
// MUI 6.3.0 still ships the legacy Grid as the default `Grid` export; the
// `size={{ xs, sm, md }}` API used throughout this file lives on Grid2.
import Grid from '@mui/material/Grid2';
import AddIcon from '@mui/icons-material/Add';
import FolderOutlinedIcon from '@mui/icons-material/FolderOutlined';
import { useProjects } from './useProjects';
import { CreateProjectDialog } from './CreateProjectDialog';
import { EmptyState, ErrorState, LoadingState } from '@/components/StateViews';
import { ApiError } from '@/api/client';

export function ProjectsListPage() {
  const [dialogOpen, setDialogOpen] = useState(false);
  const navigate = useNavigate();
  const { data: projects, isPending, isError, error, refetch } = useProjects();

  return (
    <Box>
      <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 3 }}>
        <Typography variant="h1">Projects</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setDialogOpen(true)}>
          New project
        </Button>
      </Stack>

      {isPending && <LoadingState label="Loading projects…" />}

      {isError && (
        <ErrorState
          message={error instanceof ApiError ? error.message : 'Could not load projects.'}
          onRetry={() => refetch()}
        />
      )}

      {projects && projects.length === 0 && (
        <EmptyState
          icon={<FolderOutlinedIcon sx={{ fontSize: 48 }} />}
          title="No projects yet"
          description="Create your first project to start organizing tasks."
          action={
            <Button variant="contained" startIcon={<AddIcon />} onClick={() => setDialogOpen(true)}>
              New project
            </Button>
          }
        />
      )}

      {projects && projects.length > 0 && (
        <Grid container spacing={2}>
          {projects.map((project) => (
            <Grid key={project.id} size={{ xs: 12, sm: 6, md: 4 }}>
              <Card variant="outlined">
                <CardActionArea onClick={() => navigate(`/projects/${project.id}`)} sx={{ height: '100%' }}>
                  <CardContent>
                    <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
                      <Typography variant="h3" sx={{ mb: 0.5 }}>
                        {project.name}
                      </Typography>
                      {project.isArchived && <Chip label="Archived" size="small" />}
                    </Stack>
                    <Typography
                      variant="body2"
                      color="text.secondary"
                      sx={{ mb: 2, minHeight: 40, display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}
                    >
                      {project.description || 'No description'}
                    </Typography>
                    <Stack direction="row" justifyContent="space-between" alignItems="center">
                      <Typography variant="caption" color="text.secondary">
                        Owner: {project.ownerName}
                      </Typography>
                      <Chip label={`${project.memberCount} member${project.memberCount === 1 ? '' : 's'}`} size="small" variant="outlined" />
                    </Stack>
                  </CardContent>
                </CardActionArea>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      <CreateProjectDialog open={dialogOpen} onClose={() => setDialogOpen(false)} />
    </Box>
  );
}
