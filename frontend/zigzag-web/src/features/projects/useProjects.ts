import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { projectsApi, type AddMemberPayload, type CreateProjectPayload, type UpdateProjectPayload } from '@/api/projects';
import { ApiError } from '@/api/client';

export function useProjects(includeArchived = false) {
  return useQuery({
    queryKey: ['projects', { includeArchived }],
    queryFn: () => projectsApi.list(includeArchived),
  });
}

export function useProject(id: string | undefined) {
  return useQuery({
    queryKey: ['projects', id],
    queryFn: () => projectsApi.getById(id!),
    enabled: !!id,
  });
}

export function useProjectMembers(id: string | undefined) {
  return useQuery({
    queryKey: ['projects', id, 'members'],
    queryFn: () => projectsApi.getMembers(id!),
    enabled: !!id,
  });
}

function useApiErrorSnackbar() {
  const { enqueueSnackbar } = useSnackbar();
  return (err: unknown, fallback: string) =>
    enqueueSnackbar(err instanceof ApiError ? err.message : fallback, { variant: 'error' });
}

export function useCreateProject() {
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();
  const showError = useApiErrorSnackbar();

  return useMutation({
    mutationFn: (payload: CreateProjectPayload) => projectsApi.create(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      enqueueSnackbar('Project created', { variant: 'success' });
    },
    onError: (err) => showError(err, 'Could not create the project.'),
  });
}

export function useUpdateProject(id: string) {
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();
  const showError = useApiErrorSnackbar();

  return useMutation({
    mutationFn: (payload: UpdateProjectPayload) => projectsApi.update(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      enqueueSnackbar('Project updated', { variant: 'success' });
    },
    onError: (err) => showError(err, 'Could not update the project.'),
  });
}

export function useArchiveProject() {
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();
  const showError = useApiErrorSnackbar();

  return useMutation({
    mutationFn: (id: string) => projectsApi.archive(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      enqueueSnackbar('Project archived', { variant: 'success' });
    },
    onError: (err) => showError(err, 'Could not archive the project.'),
  });
}

export function useAddProjectMember(projectId: string) {
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();
  const showError = useApiErrorSnackbar();

  return useMutation({
    mutationFn: (payload: AddMemberPayload) => projectsApi.addMember(projectId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects', projectId] });
      enqueueSnackbar('Member added', { variant: 'success' });
    },
    onError: (err) => showError(err, 'Could not add that member.'),
  });
}

export function useRemoveProjectMember(projectId: string) {
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();
  const showError = useApiErrorSnackbar();

  return useMutation({
    mutationFn: (userId: string) => projectsApi.removeMember(projectId, userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects', projectId] });
      enqueueSnackbar('Member removed', { variant: 'success' });
    },
    onError: (err) => showError(err, 'Could not remove that member.'),
  });
}
