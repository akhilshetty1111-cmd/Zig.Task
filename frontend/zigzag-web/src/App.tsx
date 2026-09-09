import { useQuery } from '@tanstack/react-query';
import { Alert, Box, Chip, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material';

import { apiClient, ApiError } from '@/api/client';

/**
 * PHASE 1 SHELL.
 *
 * Deliberately minimal: it proves the whole chain is wired - Vite serves React,
 * MUI theming applies, TanStack Query runs, Axios reaches ASP.NET Core, and the
 * backend CORS policy accepts http://localhost:5173. Routing, layouts and the
 * real pages arrive from Phase 4 onward.
 */
export default function App() {
  const health = useQuery({
    queryKey: ['api-health'],
    queryFn: async () => {
      const { data } = await apiClient.get<string>('/health');
      return data;
    },
  });

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Stack spacing={3}>
        <Box>
          <Typography variant="h1" color="primary">
            ZigZag
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Project and task management
          </Typography>
        </Box>

        <Paper variant="outlined" sx={{ p: 3 }}>
          <Typography variant="h3" gutterBottom>
            Backend connectivity
          </Typography>

          {health.isPending && (
            <Stack direction="row" spacing={1.5} alignItems="center">
              <CircularProgress size={18} />
              <Typography variant="body2">Contacting the API…</Typography>
            </Stack>
          )}

          {health.isError && (
            <Alert severity="error">
              {health.error instanceof ApiError
                ? health.error.message
                : 'The API did not respond.'}
            </Alert>
          )}

          {health.isSuccess && (
            <Stack direction="row" spacing={1.5} alignItems="center">
              <Chip label={health.data} color="success" size="small" />
              <Typography variant="body2" color="text.secondary">
                API reachable at {import.meta.env.VITE_API_BASE_URL}
              </Typography>
            </Stack>
          )}
        </Paper>
      </Stack>
    </Container>
  );
}
