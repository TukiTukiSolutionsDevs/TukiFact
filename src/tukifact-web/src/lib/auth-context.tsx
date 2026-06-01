'use client';

import { createContext, useContext, useEffect, useState, useCallback, startTransition, type ReactNode } from 'react';
import { GoogleOAuthProvider } from '@react-oauth/google';
import { api, type UserInfo, type AuthResponse } from './api';

const GOOGLE_CLIENT_ID = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID ?? '';

export interface TenantChoice {
  tenantId: string;
  ruc: string;
  razonSocial: string;
}

export interface GoogleLoginResult {
  auth: AuthResponse | null;
  tenants: TenantChoice[] | null;
}

export interface RegisterWithGoogleData {
  ruc: string;
  razonSocial: string;
  nombreComercial?: string;
  direccion?: string;
}

interface AuthState {
  user: UserInfo | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string, tenantId: string) => Promise<void>;
  /** Returns the list of tenants to pick from, or null if already logged in. */
  loginWithGoogle: (idToken: string) => Promise<TenantChoice[] | null>;
  /** Logs in with a specific tenant after the user picked one. */
  loginWithGoogleAtTenant: (idToken: string, tenantId: string) => Promise<void>;
  register: (data: RegisterData) => Promise<void>;
  registerWithGoogle: (idToken: string, data: RegisterWithGoogleData) => Promise<void>;
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
        startTransition(() => setUser(JSON.parse(stored) as UserInfo));
        api.setToken(token);
      } catch {
        /* ignore */
      }
    }
    startTransition(() => setIsLoading(false));
  }, []);

  const login = useCallback(async (email: string, password: string, tenantId: string) => {
    const res = await api.post<AuthResponse>('/v1/auth/login', { email, password, tenantId });
    api.setToken(res.accessToken);
    localStorage.setItem('refresh_token', res.refreshToken);
    localStorage.setItem('user', JSON.stringify(res.user));
    setUser(res.user);
  }, []);

  const storeAuth = useCallback((res: AuthResponse) => {
    api.setToken(res.accessToken);
    localStorage.setItem('refresh_token', res.refreshToken);
    localStorage.setItem('user', JSON.stringify(res.user));
    setUser(res.user);
  }, []);

  const loginWithGoogle = useCallback(async (idToken: string): Promise<TenantChoice[] | null> => {
    const res = await api.post<GoogleLoginResult>('/v1/auth/google', { idToken });
    if (res.auth) {
      storeAuth(res.auth);
      return null;
    }
    return res.tenants ?? [];
  }, [storeAuth]);

  const loginWithGoogleAtTenant = useCallback(async (idToken: string, tenantId: string) => {
    const res = await api.post<GoogleLoginResult>('/v1/auth/google', { idToken, tenantId });
    if (!res.auth) throw new Error('No se recibió respuesta de autenticación');
    storeAuth(res.auth);
  }, [storeAuth]);

  const registerWithGoogle = useCallback(async (idToken: string, data: RegisterWithGoogleData) => {
    const res = await api.post<AuthResponse>('/v1/auth/google/register', { idToken, ...data });
    storeAuth(res);
  }, [storeAuth]);

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
    <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>
      <AuthContext.Provider
        value={{
          user,
          isLoading,
          isAuthenticated: !!user,
          login,
          loginWithGoogle,
          loginWithGoogleAtTenant,
          register,
          registerWithGoogle,
          logout,
        }}
      >
        {children}
      </AuthContext.Provider>
    </GoogleOAuthProvider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be inside AuthProvider');
  return ctx;
}
