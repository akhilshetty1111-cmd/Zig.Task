import axios, { AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';
import type { AuthResponse } from '@/types/auth';

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
 */
export const apiClient: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 20_000,
  headers: { 'Content-Type': 'application/json' },
  // Refresh tokens are delivered as an HttpOnly cookie, which the browser only
  // attaches cross-origin when credentials are enabled.
  withCredentials: true,
});

// The access token lives in a module-level variable, never localStorage: a
// short-lived in-memory token is unreachable to an attacker who can only
// persist a payload (localStorage), not run code continuously in the tab.
let accessToken: string | null = null;

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

export function getAccessToken(): string | null {
  return accessToken;
}

/** AuthProvider registers this so the interceptor can trigger a full logout without importing React state directly. */
let onAuthFailure: (() => void) | null = null;

export function setOnAuthFailure(callback: (() => void) | null): void {
  onAuthFailure = callback;
}

apiClient.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

// Concurrent 401s must trigger exactly one refresh call, not one per request -
// every request that raced in shares this same in-flight promise.
let refreshPromise: Promise<string> | null = null;

async function refreshAccessToken(): Promise<string> {
  refreshPromise ??= apiClient
    .post<AuthResponse>('/auth/refresh')
    .then((response) => {
      setAccessToken(response.data.accessToken);
      return response.data.accessToken;
    })
    .finally(() => {
      refreshPromise = null;
    });

  return refreshPromise;
}

interface RetriableRequestConfig extends InternalAxiosRequestConfig {
  _retried?: boolean;
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiErrorResponse>) => {
    const config = error.config as RetriableRequestConfig | undefined;

    // No response at all: DNS failure, timeout, server down, blocked by CORS.
    if (!error.response) {
      return Promise.reject(
        new ApiError('Cannot reach the ZigZag server. Check your connection and try again.', 0, []),
      );
    }

    const { status, data } = error.response;
    const isAuthEndpoint = config?.url?.startsWith('/auth/');

    // One retry per request, and never for the auth endpoints themselves -
    // a 401 from /auth/login or /auth/refresh means the credentials/token
    // really are bad, not "go refresh and try again" (that would loop).
    if (status === 401 && config && !config._retried && !isAuthEndpoint) {
      config._retried = true;
      try {
        const newToken = await refreshAccessToken();
        config.headers.Authorization = `Bearer ${newToken}`;
        return apiClient(config);
      } catch {
        setAccessToken(null);
        onAuthFailure?.();
        return Promise.reject(new ApiError('Your session has expired. Please sign in again.', 401, []));
      }
    }

    if (status === 401 && !isAuthEndpoint) {
      onAuthFailure?.();
    }

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
