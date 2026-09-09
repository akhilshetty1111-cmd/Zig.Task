import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Badge,
  Box,
  Divider,
  IconButton,
  List,
  ListItemButton,
  ListItemText,
  Menu,
  Typography,
} from '@mui/material';
import NotificationsIcon from '@mui/icons-material/NotificationsOutlined';
import { useMarkNotificationRead, useNotifications } from '@/features/notifications/useNotifications';
import { formatRelativeTime } from '@/utils/formatRelativeTime';

export function NotificationBell() {
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const { data: notifications } = useNotifications(true);
  const markRead = useMarkNotificationRead();
  const navigate = useNavigate();
  const unreadCount = notifications?.length ?? 0;

  return (
    <>
      <IconButton color="inherit" onClick={(e) => setAnchorEl(e.currentTarget)} aria-label="Notifications">
        <Badge badgeContent={unreadCount} color="error" max={9}>
          <NotificationsIcon />
        </Badge>
      </IconButton>
      <Menu anchorEl={anchorEl} open={!!anchorEl} onClose={() => setAnchorEl(null)} sx={{ mt: 1 }}>
        <Box sx={{ px: 2, py: 1, minWidth: 320 }}>
          <Typography variant="h3">Notifications</Typography>
        </Box>
        <Divider />
        {!notifications || notifications.length === 0 ? (
          <Box sx={{ px: 2, py: 3, textAlign: 'center' }}>
            <Typography variant="body2" color="text.secondary">
              You&apos;re all caught up.
            </Typography>
          </Box>
        ) : (
          <List sx={{ maxHeight: 360, overflowY: 'auto', py: 0 }}>
            {notifications.map((n) => (
              <ListItemButton
                key={n.id}
                onClick={() => {
                  markRead.mutate(n.id);
                  setAnchorEl(null);
                  if (n.taskId) navigate(`/tasks/${n.taskId}`);
                }}
              >
                <ListItemText
                  primary={n.title}
                  secondary={formatRelativeTime(n.createdAt)}
                  slotProps={{ primary: { variant: 'body2', fontWeight: 600 } }}
                />
              </ListItemButton>
            ))}
          </List>
        )}
      </Menu>
    </>
  );
}
