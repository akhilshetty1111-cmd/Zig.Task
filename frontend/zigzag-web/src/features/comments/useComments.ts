import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { commentsApi } from '@/api/comments';
import { ApiError } from '@/api/client';

export function useComments(taskId: string | undefined) {
  return useQuery({
    queryKey: ['tasks', 'detail', taskId, 'comments'],
    queryFn: () => commentsApi.list(taskId!),
    enabled: !!taskId,
  });
}

export function useAddComment(taskId: string) {
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();

  return useMutation({
    mutationFn: (text: string) => commentsApi.add(taskId, text),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks', 'detail', taskId, 'comments'] });
    },
    onError: (err) =>
      enqueueSnackbar(err instanceof ApiError ? err.message : 'Could not add the comment.', { variant: 'error' }),
  });
}

export function useDeleteComment(taskId: string) {
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();

  return useMutation({
    mutationFn: (commentId: string) => commentsApi.delete(taskId, commentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks', 'detail', taskId, 'comments'] });
    },
    onError: (err) =>
      enqueueSnackbar(err instanceof ApiError ? err.message : 'Could not delete the comment.', { variant: 'error' }),
  });
}
