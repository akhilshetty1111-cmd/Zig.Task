import { Paper, Stack, Typography } from '@mui/material';
import type { ReactNode } from 'react';

export function StatTile({
  label,
  value,
  icon,
  tone = 'default',
}: {
  label: string;
  value: number;
  icon: ReactNode;
  tone?: 'default' | 'success' | 'warning' | 'error';
}) {
  const toneColor = tone === 'default' ? 'primary.main' : `${tone}.main`;

  return (
    <Paper variant="outlined" sx={{ p: 2.5, height: '100%' }}>
      <Stack direction="row" spacing={2} alignItems="center">
        <Stack
          alignItems="center"
          justifyContent="center"
          sx={{ width: 44, height: 44, borderRadius: 2, bgcolor: 'action.hover', color: toneColor, flexShrink: 0 }}
        >
          {icon}
        </Stack>
        <Stack sx={{ minWidth: 0 }}>
          <Typography variant="h2" sx={{ lineHeight: 1.1 }}>
            {value}
          </Typography>
          <Typography variant="body2" color="text.secondary" noWrap>
            {label}
          </Typography>
        </Stack>
      </Stack>
    </Paper>
  );
}
