import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { authApi, type LoginPayload, type RegisterPayload } from '@/api/auth';
import { apiClient, setAccessToken, setOnAuthFailure } from '@/api/client';
import type { User } from '@/types/auth';

interface AuthContextValue {
  user: User | null;
  isAuthenticated: boolean;
  /** True only during the initial silent-refresh attempt on page load. */
  isInitializing: boolean;
  login: (payload: LoginPayload) => Promise<void>;
  register: (payload: RegisterPayload) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

/**
 * Holds the current user and wires the Axios client's token storage +
 * auth-failure callback to React state, so a 401 anywhere in the app (an
 * expired refresh token, a revoked session) reliably drops the user back to
 * a logged-out UI instead of leaving stale "logged in" state on screen.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isInitializing, setIsInitializing] = useState(true);

  const clearSession = useCallback(() => {
    setAccessToken(null);
    setUser(null);
  }, []);

  useEffect(() => {
    setOnAuthFailure(clearSession);
    return () => setOnAuthFailure(null);
  }, [clearSession]);

  // On first load, a valid refresh cookie (from a previous visit) should
  // silently restore the session rather than forcing a fresh login every
  // time the tab reloads - the access token itself never survives a reload
  // since it is deliberately memory-only.
  useEffect(() => {
    let cancelled = false;

    apiClient
      .post<{ accessToken: string; user: User }>('/auth/refresh')
      .then((response) => {
        if (cancelled) return;
        setAccessToken(response.data.accessToken);
        setUser(response.data.user);
      })
      .catch(() => {
        // No valid session cookie - that's a normal logged-out state, not an error.
      })
      .finally(() => {
        if (!cancelled) setIsInitializing(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const login = useCallback(async (payload: LoginPayload) => {
    const result = await authApi.login(payload);
    setAccessToken(result.accessToken);
    setUser(result.user);
  }, []);

  const register = useCallback(async (payload: RegisterPayload) => {
    const result = await authApi.register(payload);
    setAccessToken(result.accessToken);
    setUser(result.user);
  }, []);

  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } finally {
      clearSession();
    }
  }, [clearSession]);

  const value = useMemo(
    () => ({ user, isAuthenticated: user !== null, isInitializing, login, register, logout }),
    [user, isInitializing, login, register, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
