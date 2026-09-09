import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider } from '@mui/material/styles';
import { describe, expect, it, vi } from 'vitest';

import App from '@/App';
import { apiClient } from '@/api/client';
import { zigzagTheme } from '@/theme';

/**
 * Phase 1 smoke test: proves the test harness itself works end to end -
 * jsdom, the '@' path alias, MUI theming, TanStack Query and jest-dom matchers.
 * Real feature tests arrive with the features.
 */
function renderApp() {
  // retry:false so a failed query surfaces immediately instead of backing off.
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={zigzagTheme}>
        <App />
      </ThemeProvider>
    </QueryClientProvider>,
  );
}

describe('App', () => {
  it('renders the ZigZag heading', async () => {
    vi.spyOn(apiClient, 'get').mockResolvedValue({ data: 'Healthy' });

    renderApp();

    expect(screen.getByRole('heading', { name: 'ZigZag' })).toBeInTheDocument();

    // Let the health query settle before the test ends. Without this the state
    // update lands after teardown and React logs an act() warning.
    await screen.findByText('Healthy');
  });

  it('shows the health status once the API responds', async () => {
    vi.spyOn(apiClient, 'get').mockResolvedValue({ data: 'Healthy' });

    renderApp();

    expect(await screen.findByText('Healthy')).toBeInTheDocument();
  });

  it('shows an error message when the API is unreachable', async () => {
    vi.spyOn(apiClient, 'get').mockRejectedValue(new Error('boom'));

    renderApp();

    expect(await screen.findByText('The API did not respond.')).toBeInTheDocument();
  });
});
