import axios, { AxiosError, type AxiosInstance } from 'axios';

/**
 * The error envelope every ZigZag endpoint returns on failure.
 * Mirrors the shape produced by the backend's global exception middleware.
 */
export interface ApiErrorResponse {
  success: false;
  message: string;
  errors: string[];
  traceId: string;
}

/**
 * A normalized failure the UI can render directly. Components should never have
 * to unwrap an AxiosError themselves.
 */
export class ApiError extends Error {
  public readonly status: number;
  public readonly errors: string[];
  public readonly traceId: string | undefined;

  constructor(message: string, status: number, errors: string[], traceId?: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.errors = errors;
    this.traceId = traceId;
  }
}

/**
 * Single Axios instance for the whole app.
 *
 * Why centralize: base URL, auth headers, and 401/refresh handling must behave
 * identically on every call. Configuring Axios per-feature is how apps end up
 * with one endpoint that silently skips token refresh.
 *
 * PHASE 1 SCOPE: base configuration + error normalization.
 * The access-token header and the 401 -> refresh -> retry interceptor are added
 * in Phase 4 alongside the auth endpoints.
 */
export const apiClient: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 20_000,
  headers: { 'Content-Type': 'application/json' },
  // Refresh tokens are delivered as an HttpOnly cookie, which the browser only
  // attaches cross-origin when credentials are enabled.
  withCredentials: true,
});

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiErrorResponse>) => {
    // No response at all: DNS failure, timeout, server down, blocked by CORS.
    if (!error.response) {
      return Promise.reject(
        new ApiError(
          'Cannot reach the ZigZag server. Check your connection and try again.',
          0,
          [],
        ),
      );
    }

    const { status, data } = error.response;

    // A well-formed error from our own API.
    if (data && typeof data.message === 'string') {
      return Promise.reject(new ApiError(data.message, status, data.errors ?? [], data.traceId));
    }

    // Something else answered (a proxy, a gateway) - do not leak its body to the UI.
    return Promise.reject(new ApiError(defaultMessageFor(status), status, []));
  },
);

function defaultMessageFor(status: number): string {
  switch (status) {
    case 400:
      return 'The request was invalid.';
    case 401:
      return 'Your session has expired. Please sign in again.';
    case 403:
      return 'You do not have permission to do that.';
    case 404:
      return 'That item could not be found.';
    case 409:
      return 'That change conflicts with the current state.';
    default:
      return 'Something went wrong. Please try again.';
  }
}
