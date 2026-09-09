import { Box, Stack, Typography } from '@mui/material';
import { useTheme } from '@mui/material/styles';
import type { PriorityCount, StatusCount } from '@/types/dashboard';
import { STATUS_LABELS } from '@/types/task';

const STATUS_TONE: Record<string, 'default' | 'info' | 'warning' | 'success' | 'error'> = {
  Todo: 'default',
  InProgress: 'info',
  InReview: 'warning',
  Done: 'success',
  Blocked: 'error',
};

const PRIORITY_TONE: Record<string, 'default' | 'info' | 'warning' | 'error'> = {
  Low: 'default',
  Medium: 'info',
  High: 'warning',
  Urgent: 'error',
};

/**
 * Horizontal bars, each directly labeled with its name and count - identity
 * is never color-alone, so no separate legend box is needed for either chart.
 */
function BreakdownList({
  rows,
}: {
  rows: { label: string; count: number; tone: 'default' | 'info' | 'warning' | 'success' | 'error' }[];
}) {
  const theme = useTheme();
  const max = Math.max(1, ...rows.map((r) => r.count));

  const toneColor = (tone: string) =>
    tone === 'default' ? theme.palette.text.disabled : theme.palette[tone as 'info' | 'warning' | 'success' | 'error'].main;

  return (
    <Stack spacing={1.5}>
      {rows.map((row) => (
        <Stack key={row.label} spacing={0.5}>
          <Stack direction="row" justifyContent="space-between">
            <Typography variant="body2">{row.label}</Typography>
            <Typography variant="body2" fontWeight={600}>
              {row.count}
            </Typography>
          </Stack>
          <Box sx={{ height: 8, borderRadius: 4, bgcolor: 'action.hover', overflow: 'hidden' }}>
            <Box
              sx={{
                height: '100%',
                width: `${(row.count / max) * 100}%`,
                borderRadius: 4,
                bgcolor: toneColor(row.tone),
                transition: 'width 0.3s ease',
              }}
            />
          </Box>
        </Stack>
      ))}
    </Stack>
  );
}

export function StatusBreakdown({ data }: { data: StatusCount[] }) {
  return (
    <BreakdownList
      rows={data.map((d) => ({ label: STATUS_LABELS[d.status], count: d.count, tone: STATUS_TONE[d.status] }))}
    />
  );
}

export function PriorityBreakdown({ data }: { data: PriorityCount[] }) {
  return (
    <BreakdownList rows={data.map((d) => ({ label: d.priority, count: d.count, tone: PRIORITY_TONE[d.priority] }))} />
  );
}
