'use client';

import { createContext, useContext, useEffect, useState, useCallback, type ReactNode } from 'react';
import { api } from './api';

export interface BackofficeUser {
  id: number;
  email: string;
  fullName: string;
  role: string; // superadmin | support | ops
}

interface BackofficeAuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: BackofficeUser;
}

interface BackofficeAuthState {
  user: BackofficeUser | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const STORAGE_KEY_TOKEN = 'bo_access_token';
const STORAGE_KEY_REFRESH = 'bo_refresh_token';
const STORAGE_KEY_USER = 'bo_user';

const BackofficeAuthContext = createContext<BackofficeAuthState | null>(null);

export function BackofficeAuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<BackofficeUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const stored = localStorage.getItem(STORAGE_KEY_USER);
    const token = localStorage.getItem(STORAGE_KEY_TOKEN);
    if (stored && token) {
      try {
        setUser(JSON.parse(stored) as BackofficeUser);
        api.setToken(token);
      } catch {
        /* ignore */
      }
    }
    setIsLoading(false);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const res = await api.post<BackofficeAuthResponse>('/v1/backoffice/auth/login', {
      email,
      password,
    });
    api.setToken(res.accessToken);
    localStorage.setItem(STORAGE_KEY_TOKEN, res.accessToken);
    localStorage.setItem(STORAGE_KEY_REFRESH, res.refreshToken);
    localStorage.setItem(STORAGE_KEY_USER, JSON.stringify(res.user));
    setUser(res.user);
  }, []);

  const logout = useCallback(() => {
    api.setToken(null);
    setUser(null);
    if (typeof window !== 'undefined') {
      localStorage.removeItem(STORAGE_KEY_TOKEN);
      localStorage.removeItem(STORAGE_KEY_REFRESH);
      localStorage.removeItem(STORAGE_KEY_USER);
    }
  }, []);

  return (
    <BackofficeAuthContext.Provider
      value={{ user, isLoading, isAuthenticated: !!user, login, logout }}
    >
      {children}
    </BackofficeAuthContext.Provider>
  );
}

export function useBackofficeAuth() {
  const ctx = useContext(BackofficeAuthContext);
  if (!ctx) throw new Error('useBackofficeAuth must be inside BackofficeAuthProvider');
  return ctx;
}
