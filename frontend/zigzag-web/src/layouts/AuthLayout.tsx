import { Outlet } from 'react-router-dom';
import { Box, Paper, Stack, Typography } from '@mui/material';

export function AuthLayout() {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'background.default',
        p: 2,
      }}
    >
      <Stack spacing={3} sx={{ width: '100%', maxWidth: 420 }}>
        <Stack alignItems="center" spacing={0.5}>
          <Typography variant="h1" color="primary" sx={{ fontWeight: 800 }}>
            ZigZag
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Project and task management
          </Typography>
        </Stack>
        <Paper variant="outlined" sx={{ p: { xs: 3, sm: 4 } }}>
          <Outlet />
        </Paper>
      </Stack>
    </Box>
  );
}
