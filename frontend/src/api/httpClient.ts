import axios, { type InternalAxiosRequestConfig } from "axios";
import { notifyGlobal } from "../contexts/NotificationContext";
import { authStorageKeys } from "../types/auth";

const apiBaseUrl =
  import.meta.env.VITE_API_URL ?? import.meta.env.VITE_API_BASE_URL ?? "";
const tokenStorageMode = (import.meta.env.VITE_AUTH_TOKEN_STORAGE_MODE ?? "cookie").toLowerCase();
const cookieTokenMode = tokenStorageMode === "cookie";
const csrfCookieName = import.meta.env.VITE_AUTH_CSRF_COOKIE_NAME ?? "hop_csrf_token";
const csrfHeaderName = import.meta.env.VITE_AUTH_CSRF_HEADER_NAME ?? "X-CSRF-TOKEN";
let memoryAccessToken: string | null = null;

export const httpClient = axios.create({
  baseURL: apiBaseUrl,
  timeout: 15000,
  withCredentials: cookieTokenMode,
});

const refreshClient = axios.create({
  baseURL: apiBaseUrl,
  timeout: 15000,
  withCredentials: cookieTokenMode,
});

const csrfClient = axios.create({
  baseURL: apiBaseUrl,
  timeout: 15000,
  withCredentials: cookieTokenMode,
});

let csrfRequest: Promise<void> | null = null;

export function setAuthToken(token: string | null) {
  memoryAccessToken = token;
  if (token) {
    httpClient.defaults.headers.common.Authorization = `Bearer ${token}`;
    return;
  }

  delete httpClient.defaults.headers.common.Authorization;
}

export function isCookieTokenMode() {
  return cookieTokenMode;
}

export function getApiBaseUrl() {
  return apiBaseUrl;
}

httpClient.interceptors.request.use(async (config) => {
  const token = memoryAccessToken ?? (cookieTokenMode ? null : localStorage.getItem(authStorageKeys.accessToken));
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return attachCsrfToken(config);
});

refreshClient.interceptors.request.use(attachCsrfToken);

httpClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as typeof error.config & { _retry?: boolean };

    if (error.response?.status !== 401 || originalRequest?._retry) {
      notifyHttpError(error);
      return Promise.reject(error);
    }

    const refreshToken = cookieTokenMode ? null : localStorage.getItem(authStorageKeys.refreshToken);
    if (!cookieTokenMode && !refreshToken) {
      clearStoredSession();
      notifyGlobal("warning", "กรุณาเข้าสู่ระบบใหม่");
      return Promise.reject(error);
    }

    originalRequest._retry = true;

    try {
      const response = await refreshClient.post("/api/auth/refresh-token", { refreshToken });
      const data = response.data.data;

      if (!cookieTokenMode) {
        localStorage.setItem(authStorageKeys.accessToken, data.accessToken);
        localStorage.setItem(authStorageKeys.refreshToken, data.refreshToken);
      }
      localStorage.setItem(authStorageKeys.user, JSON.stringify(data.user));
      setAuthToken(data.accessToken);
      window.dispatchEvent(new CustomEvent("hop-auth-refreshed", { detail: data }));

      originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
      return httpClient(originalRequest);
    } catch (refreshError) {
      clearStoredSession();
      notifyGlobal("warning", "กรุณาเข้าสู่ระบบใหม่");
      window.location.assign("/login");
      return Promise.reject(refreshError);
    }
  },
);

function notifyHttpError(error: unknown) {
  if (!axios.isAxiosError(error)) {
    notifyGlobal("error", "เกิดข้อผิดพลาด กรุณาลองใหม่อีกครั้ง");
    return;
  }

  if (!error.response) {
    notifyGlobal("error", "ไม่สามารถเชื่อมต่อเซิร์ฟเวอร์ได้");
    return;
  }

  if (error.response.status === 403) {
    notifyGlobal("warning", "คุณไม่มีสิทธิ์ดำเนินการรายการนี้");
    return;
  }

  if (error.response.status === 401) {
    notifyGlobal("warning", "กรุณาเข้าสู่ระบบใหม่");
    return;
  }

  const payload = error.response.data as { message?: string; referenceId?: string } | undefined;
  const message = payload?.message || "เกิดข้อผิดพลาด กรุณาลองใหม่อีกครั้ง";
  notifyGlobal("error", payload?.referenceId ? `${message} (Reference ID: ${payload.referenceId})` : message);
}

function clearStoredSession() {
  localStorage.removeItem(authStorageKeys.accessToken);
  localStorage.removeItem(authStorageKeys.refreshToken);
  localStorage.removeItem(authStorageKeys.user);
  setAuthToken(null);
}

function isUnsafeMethod(method?: string) {
  return ["post", "put", "patch", "delete"].includes((method ?? "get").toLowerCase());
}

async function attachCsrfToken(config: InternalAxiosRequestConfig) {
  if (!isUnsafeMethod(config.method)) {
    return config;
  }

  let csrfToken = readCookie(csrfCookieName);
  if (!csrfToken && cookieTokenMode) {
    await ensureCsrfCookie();
    csrfToken = readCookie(csrfCookieName);
  }

  if (csrfToken) {
    config.headers[csrfHeaderName] = csrfToken;
  }

  return config;
}

async function ensureCsrfCookie() {
  csrfRequest ??= csrfClient.get("/api/csrf").then(() => undefined).finally(() => {
    csrfRequest = null;
  });
  return csrfRequest;
}

function readCookie(name: string) {
  return document.cookie
    .split(";")
    .map((value) => value.trim())
    .find((value) => value.startsWith(`${name}=`))
    ?.slice(name.length + 1);
}
