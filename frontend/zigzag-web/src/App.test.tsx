import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider } from '@mui/material/styles';
import { SnackbarProvider } from 'notistack';
import { describe, expect, it, vi } from 'vitest';

import App from '@/App';
import { apiClient, ApiError } from '@/api/client';
import { zigzagTheme } from '@/theme';

/**
 * Smoke test: an unauthenticated visitor lands on /login rather than a
 * protected page. AuthProvider's initial silent-refresh call is mocked to
 * fail immediately (no session cookie), matching a first-time visitor.
 */
function renderApp() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  return render(
    <MemoryRouter initialEntries={['/']}>
      <QueryClientProvider client={queryClient}>
        <ThemeProvider theme={zigzagTheme}>
          <SnackbarProvider>
            <App />
          </SnackbarProvider>
        </ThemeProvider>
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

describe('App', () => {
  it('redirects an unauthenticated visitor to the login page', async () => {
    vi.spyOn(apiClient, 'post').mockRejectedValue(new ApiError('No session', 401, []));

    renderApp();

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument();
  });
});
