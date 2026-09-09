import { useNavigate } from 'react-router-dom';
import { Box, Paper, Stack, Typography } from '@mui/material';
// MUI 6.3.0 still ships the legacy Grid as the default `Grid` export; the
// `size={{ xs, sm, md }}` API used throughout this file lives on Grid2.
import Grid from '@mui/material/Grid2';
import FolderOutlinedIcon from '@mui/icons-material/FolderOutlined';
import AssignmentOutlinedIcon from '@mui/icons-material/AssignmentOutlined';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutline';
import PendingActionsIcon from '@mui/icons-material/PendingActions';
import ReportProblemOutlinedIcon from '@mui/icons-material/ReportProblemOutlined';
import PersonOutlineIcon from '@mui/icons-material/PersonOutline';
import { useAuth } from '@/features/auth/AuthContext';
import { useDashboard } from './useDashboard';
import { StatTile } from './StatTile';
import { PriorityBreakdown, StatusBreakdown } from './BreakdownBars';
import { EmptyState, ErrorState, LoadingState } from '@/components/StateViews';
import { formatRelativeTime } from '@/utils/formatRelativeTime';
import { ApiError } from '@/api/client';

export function DashboardPage() {
  const { user } = useAuth();
  const { data, isPending, isError, error, refetch } = useDashboard();
  const navigate = useNavigate();

  if (isPending) return <LoadingState label="Loading dashboard…" />;
  if (isError) {
    return <ErrorState message={error instanceof ApiError ? error.message : 'Could not load the dashboard.'} onRetry={() => refetch()} />;
  }

  return (
    <Box>
      <Typography variant="h1" sx={{ mb: 0.5 }}>
        Welcome back, {user?.name.split(' ')[0]}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Here&apos;s what&apos;s happening across your projects.
      </Typography>

      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid size={{ xs: 6, sm: 4, md: 2 }}>
          <StatTile label="Projects" value={data.totalProjects} icon={<FolderOutlinedIcon />} />
        </Grid>
        <Grid size={{ xs: 6, sm: 4, md: 2 }}>
          <StatTile label="Total tasks" value={data.totalTasks} icon={<AssignmentOutlinedIcon />} />
        </Grid>
        <Grid size={{ xs: 6, sm: 4, md: 2 }}>
          <StatTile label="Completed" value={data.completedTasks} icon={<CheckCircleOutlineIcon />} tone="success" />
        </Grid>
        <Grid size={{ xs: 6, sm: 4, md: 2 }}>
          <StatTile label="Pending" value={data.pendingTasks} icon={<PendingActionsIcon />} tone="warning" />
        </Grid>
        <Grid size={{ xs: 6, sm: 4, md: 2 }}>
          <StatTile label="Overdue" value={data.overdueTasks} icon={<ReportProblemOutlinedIcon />} tone="error" />
        </Grid>
        <Grid size={{ xs: 6, sm: 4, md: 2 }}>
          <StatTile label="Assigned to me" value={data.tasksAssignedToMe} icon={<PersonOutlineIcon />} />
        </Grid>
      </Grid>

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, md: 4 }}>
          <Paper variant="outlined" sx={{ p: 2.5, height: '100%' }}>
            <Typography variant="h3" sx={{ mb: 2 }}>
              Tasks by status
            </Typography>
            <StatusBreakdown data={data.tasksByStatus} />
          </Paper>
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <Paper variant="outlined" sx={{ p: 2.5, height: '100%' }}>
            <Typography variant="h3" sx={{ mb: 2 }}>
              Tasks by priority
            </Typography>
            <PriorityBreakdown data={data.tasksByPriority} />
          </Paper>
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <Paper variant="outlined" sx={{ p: 2.5, height: '100%' }}>
            <Typography variant="h3" sx={{ mb: 2 }}>
              Recent activity
            </Typography>
            {data.recentActivities.length === 0 ? (
              <EmptyState title="No recent activity" description="Changes across your projects will show up here." />
            ) : (
              <Stack spacing={1.5}>
                {data.recentActivities.slice(0, 8).map((activity) => (
                  <Box
                    key={activity.id}
                    onClick={() => navigate(`/tasks/${activity.taskId}`)}
                    sx={{ cursor: 'pointer', '&:hover': { color: 'primary.main' } }}
                  >
                    <Typography variant="body2">
                      <strong>{activity.changedByName}</strong> updated {activity.fieldName} on &quot;{activity.taskTitle}&quot;
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {formatRelativeTime(activity.changedAt)}
                    </Typography>
                  </Box>
                ))}
              </Stack>
            )}
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
}
