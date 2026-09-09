import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notificationsApi } from '@/api/notifications';

const notificationsKey = (unreadOnly: boolean) => ['notifications', { unreadOnly }] as const;

export function useNotifications(unreadOnly: boolean) {
  return useQuery({
    queryKey: notificationsKey(unreadOnly),
    queryFn: () => notificationsApi.list(unreadOnly),
    // Polling, not a push channel - simplest way to keep the bell reasonably
    // fresh without adding SignalR/WebSockets for a portfolio-scope feature.
    refetchInterval: 30_000,
  });
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
  });
}
