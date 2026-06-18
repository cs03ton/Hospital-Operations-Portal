import axios from "axios";
import { authStorageKeys } from "../types/auth";

const apiBaseUrl =
  import.meta.env.VITE_API_URL ?? import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";

export const httpClient = axios.create({
  baseURL: apiBaseUrl,
  timeout: 15000,
});

const refreshClient = axios.create({
  baseURL: apiBaseUrl,
  timeout: 15000,
});

export function setAuthToken(token: string | null) {
  if (token) {
    httpClient.defaults.headers.common.Authorization = `Bearer ${token}`;
    return;
  }

  delete httpClient.defaults.headers.common.Authorization;
}

httpClient.interceptors.request.use((config) => {
  const token = localStorage.getItem(authStorageKeys.accessToken);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

httpClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as typeof error.config & { _retry?: boolean };

    if (error.response?.status !== 401 || originalRequest?._retry) {
      return Promise.reject(error);
    }

    const refreshToken = localStorage.getItem(authStorageKeys.refreshToken);
    if (!refreshToken) {
      clearStoredSession();
      return Promise.reject(error);
    }

    originalRequest._retry = true;

    try {
      const response = await refreshClient.post("/api/auth/refresh-token", { refreshToken });
      const data = response.data.data;

      localStorage.setItem(authStorageKeys.accessToken, data.accessToken);
      localStorage.setItem(authStorageKeys.refreshToken, data.refreshToken);
      localStorage.setItem(authStorageKeys.user, JSON.stringify(data.user));
      setAuthToken(data.accessToken);
      window.dispatchEvent(new Event("hop-auth-refreshed"));

      originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
      return httpClient(originalRequest);
    } catch (refreshError) {
      clearStoredSession();
      window.location.assign("/login");
      return Promise.reject(refreshError);
    }
  },
);

function clearStoredSession() {
  localStorage.removeItem(authStorageKeys.accessToken);
  localStorage.removeItem(authStorageKeys.refreshToken);
  localStorage.removeItem(authStorageKeys.user);
  setAuthToken(null);
}
