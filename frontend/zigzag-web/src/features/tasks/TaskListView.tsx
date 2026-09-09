import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Avatar,
  Box,
  InputAdornment,
  MenuItem,
  Pagination,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import { useTasks } from './useTasks';
import { TASK_PRIORITIES, TASK_STATUSES, STATUS_LABELS, type TaskPriority, type TaskStatus } from '@/types/task';
import { PriorityChip, StatusChip } from '@/components/StatusPriorityChips';
import { EmptyState, ErrorState, LoadingState } from '@/components/StateViews';
import { formatDate } from '@/utils/formatRelativeTime';
import { ApiError } from '@/api/client';

export function TaskListView({ projectId }: { projectId: string }) {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<TaskStatus | ''>('');
  const [priority, setPriority] = useState<TaskPriority | ''>('');
  const [pageNumber, setPageNumber] = useState(1);

  const { data, isPending, isError, error, refetch } = useTasks(projectId, {
    search: search || undefined,
    status: status || undefined,
    priority: priority || undefined,
    pageNumber,
    pageSize: 10,
  });

  return (
    <Box>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2.5 }}>
        <TextField
          placeholder="Search tasks…"
          size="small"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPageNumber(1);
          }}
          slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment> } }}
          sx={{ flexGrow: 1 }}
        />
        <TextField
          select
          size="small"
          label="Status"
          value={status}
          onChange={(e) => {
            setStatus(e.target.value as TaskStatus | '');
            setPageNumber(1);
          }}
          sx={{ minWidth: 150 }}
        >
          <MenuItem value="">All statuses</MenuItem>
          {TASK_STATUSES.map((s) => (
            <MenuItem key={s} value={s}>
              {STATUS_LABELS[s]}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          size="small"
          label="Priority"
          value={priority}
          onChange={(e) => {
            setPriority(e.target.value as TaskPriority | '');
            setPageNumber(1);
          }}
          sx={{ minWidth: 150 }}
        >
          <MenuItem value="">All priorities</MenuItem>
          {TASK_PRIORITIES.map((p) => (
            <MenuItem key={p} value={p}>
              {p}
            </MenuItem>
          ))}
        </TextField>
      </Stack>

      {isPending && <LoadingState label="Loading tasks…" />}

      {isError && (
        <ErrorState message={error instanceof ApiError ? error.message : 'Could not load tasks.'} onRetry={() => refetch()} />
      )}

      {data && data.items.length === 0 && (
        <EmptyState title="No tasks match your filters" description="Try clearing search or filters, or create a new task." />
      )}

      {data && data.items.length > 0 && (
        <>
          <TableContainer sx={{ border: 1, borderColor: 'divider', borderRadius: 1.5 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Title</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Priority</TableCell>
                  <TableCell>Assignee</TableCell>
                  <TableCell>Due date</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data.items.map((task) => (
                  <TableRow
                    key={task.id}
                    hover
                    onClick={() => navigate(`/tasks/${task.id}`)}
                    sx={{ cursor: 'pointer' }}
                  >
                    <TableCell sx={{ fontWeight: 600 }}>{task.title}</TableCell>
                    <TableCell>
                      <StatusChip status={task.status} />
                    </TableCell>
                    <TableCell>
                      <PriorityChip priority={task.priority} />
                    </TableCell>
                    <TableCell>
                      {task.assignedToName ? (
                        <Stack direction="row" spacing={1} alignItems="center">
                          <Avatar sx={{ width: 22, height: 22, fontSize: 11, bgcolor: 'primary.main' }}>
                            {task.assignedToName.charAt(0).toUpperCase()}
                          </Avatar>
                          {task.assignedToName}
                        </Stack>
                      ) : (
                        '—'
                      )}
                    </TableCell>
                    <TableCell>{formatDate(task.dueDate)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>

          {data.totalPages > 1 && (
            <Stack alignItems="center" sx={{ mt: 2.5 }}>
              <Pagination count={data.totalPages} page={pageNumber} onChange={(_e, page) => setPageNumber(page)} color="primary" />
            </Stack>
          )}
        </>
      )}
    </Box>
  );
}
