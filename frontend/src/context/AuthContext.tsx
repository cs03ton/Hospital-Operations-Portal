/* eslint-disable react-refresh/only-export-components */
import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import * as authApi from "../api/authApi";
import { isCookieTokenMode, setAuthToken } from "../api/httpClient";
import { notifyGlobal } from "../contexts/NotificationContext";
import { authStorageKeys, type AuthUser } from "../types/auth";

type AuthContextValue = {
  user: AuthUser | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  clearSession: () => void;
  refreshUser: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [accessToken, setAccessToken] = useState<string | null>(() =>
    isCookieTokenMode() ? null : localStorage.getItem(authStorageKeys.accessToken),
  );
  const [refreshToken, setRefreshToken] = useState<string | null>(() =>
    isCookieTokenMode() ? null : localStorage.getItem(authStorageKeys.refreshToken),
  );
  const [user, setUser] = useState<AuthUser | null>(() => {
    const raw = localStorage.getItem(authStorageKeys.user);
    return raw ? normalizeUser(JSON.parse(raw) as AuthUser) : null;
  });
  const [isLoading, setIsLoading] = useState(true);

  const clearSession = useCallback(() => {
    setAuthToken(null);
    setAccessToken(null);
    setRefreshToken(null);
    setUser(null);
    localStorage.removeItem(authStorageKeys.accessToken);
    localStorage.removeItem(authStorageKeys.refreshToken);
    localStorage.removeItem(authStorageKeys.user);
  }, []);

  useEffect(() => {
    setAuthToken(accessToken);

    async function loadProfile() {
      if (!accessToken) {
        if (isCookieTokenMode()) {
          if (window.location.pathname === "/login") {
            clearSession();
            setIsLoading(false);
            return;
          }

          try {
            const refreshed = await authApi.refreshSession(null);
            setAuthToken(refreshed.accessToken);
            setAccessToken(refreshed.accessToken);
            setRefreshToken(null);
            const normalizedUser = normalizeUser(refreshed.user);
            setUser(normalizedUser);
            localStorage.setItem(authStorageKeys.user, JSON.stringify(normalizedUser));
          } catch {
            clearSession();
          } finally {
            setIsLoading(false);
          }
          return;
        }

        setIsLoading(false);
        return;
      }

      try {
        const profile = await authApi.getCurrentUser();
        const normalizedProfile = normalizeUser(profile);
        setUser(normalizedProfile);
        localStorage.setItem(authStorageKeys.user, JSON.stringify(normalizedProfile));
      } catch {
        clearSession();
      } finally {
        setIsLoading(false);
      }
    }

    loadProfile();
  }, [accessToken, clearSession]);

  useEffect(() => {
    function syncRefreshedSession(event: Event) {
      const detail = (event as CustomEvent<Awaited<ReturnType<typeof authApi.refreshSession>>>).detail;
      setAccessToken(detail?.accessToken ?? (isCookieTokenMode() ? null : localStorage.getItem(authStorageKeys.accessToken)));
      setRefreshToken(isCookieTokenMode() ? null : detail?.refreshToken ?? localStorage.getItem(authStorageKeys.refreshToken));
      const raw = localStorage.getItem(authStorageKeys.user);
      setUser(detail?.user ? normalizeUser(detail.user) : raw ? normalizeUser(JSON.parse(raw) as AuthUser) : null);
    }

    window.addEventListener("hop-auth-refreshed", syncRefreshedSession);
    return () => window.removeEventListener("hop-auth-refreshed", syncRefreshedSession);
  }, []);

  const signIn = useCallback(async (username: string, password: string) => {
    const result = await authApi.login(username, password);
    setAuthToken(result.accessToken);
    setAccessToken(result.accessToken);
    setRefreshToken(isCookieTokenMode() ? null : result.refreshToken);
    const normalizedUser = normalizeUser(result.user);
    setUser(normalizedUser);
    if (!isCookieTokenMode()) {
      localStorage.setItem(authStorageKeys.accessToken, result.accessToken);
      localStorage.setItem(authStorageKeys.refreshToken, result.refreshToken);
    }
    localStorage.setItem(authStorageKeys.user, JSON.stringify(normalizedUser));
    notifyGlobal("success", "เข้าสู่ระบบสำเร็จ");
  }, []);

  const signOut = useCallback(async () => {
    try {
      if (accessToken) {
        await authApi.logout(refreshToken);
      }
    } finally {
      clearSession();
      notifyGlobal("success", "ออกจากระบบเรียบร้อยแล้ว");
    }
  }, [accessToken, clearSession, refreshToken]);

  const refreshUser = useCallback(async () => {
    const profile = await authApi.getCurrentUser();
    const normalizedProfile = normalizeUser(profile);
    setUser(normalizedProfile);
    localStorage.setItem(authStorageKeys.user, JSON.stringify(normalizedProfile));
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      accessToken,
      refreshToken,
      isAuthenticated: Boolean(accessToken && user),
      isLoading,
      login: signIn,
      logout: signOut,
      clearSession,
      refreshUser,
    }),
    [accessToken, clearSession, isLoading, refreshToken, refreshUser, signIn, signOut, user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used inside AuthProvider.");
  }

  return context;
}

function normalizeUser(user: AuthUser): AuthUser {
  return {
    ...user,
    permissions: user.permissions ?? [],
  };
}
