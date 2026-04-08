'use client';

import { createContext, useContext, useEffect, useState, useCallback, type ReactNode } from 'react';
import { api, type UserInfo, type AuthResponse } from './api';

interface AuthState {
  user: UserInfo | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string, tenantId: string) => Promise<void>;
  register: (data: RegisterData) => Promise<void>;
  logout: () => void;
}

export interface RegisterData {
  ruc: string;
  razonSocial: string;
  nombreComercial?: string;
  direccion?: string;
  adminEmail: string;
  adminPassword: string;
  adminFullName: string;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserInfo | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const stored = localStorage.getItem('user');
    const token = localStorage.getItem('access_token');
    if (stored && token) {
      try {
        setUser(JSON.parse(stored) as UserInfo);
        api.setToken(token);
      } catch {
        /* ignore */
      }
    }
    setIsLoading(false);
  }, []);

  const login = useCallback(async (email: string, password: string, tenantId: string) => {
    const res = await api.post<AuthResponse>('/v1/auth/login', { email, password, tenantId });
    api.setToken(res.accessToken);
    localStorage.setItem('refresh_token', res.refreshToken);
    localStorage.setItem('user', JSON.stringify(res.user));
    setUser(res.user);
  }, []);

  const register = useCallback(async (data: RegisterData) => {
    const res = await api.post<AuthResponse>('/v1/auth/register', data);
    api.setToken(res.accessToken);
    localStorage.setItem('refresh_token', res.refreshToken);
    localStorage.setItem('user', JSON.stringify(res.user));
    setUser(res.user);
  }, []);

  const logout = useCallback(() => {
    api.logout();
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider value={{ user, isLoading, isAuthenticated: !!user, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be inside AuthProvider');
  return ctx;
}
