import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { attachmentsApi } from '@/api/attachments';
import { ApiError } from '@/api/client';

export function useAttachments(taskId: string | undefined) {
  return useQuery({
    queryKey: ['tasks', 'detail', taskId, 'attachments'],
    queryFn: () => attachmentsApi.list(taskId!),
    enabled: !!taskId,
  });
}

export function useUploadAttachment(taskId: string) {
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();

  return useMutation({
    mutationFn: (file: File) => attachmentsApi.upload(taskId, file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks', 'detail', taskId, 'attachments'] });
    },
    onError: (err) =>
      enqueueSnackbar(err instanceof ApiError ? err.message : 'Could not upload the file.', { variant: 'error' }),
  });
}

export function useDeleteAttachment(taskId: string) {
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();

  return useMutation({
    mutationFn: (attachmentId: string) => attachmentsApi.delete(taskId, attachmentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks', 'detail', taskId, 'attachments'] });
    },
    onError: (err) =>
      enqueueSnackbar(err instanceof ApiError ? err.message : 'Could not delete the file.', { variant: 'error' }),
  });
}
