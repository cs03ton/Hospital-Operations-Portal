import { httpClient } from "./httpClient";
import type { ApiResponse, AuthUser, LoginResponse } from "../types/auth";

export async function login(username: string, password: string) {
  const response = await httpClient.post<ApiResponse<LoginResponse>>("/api/auth/login", {
    username,
    password,
  });

  return response.data.data;
}

export async function getCurrentUser() {
  const response = await httpClient.get<ApiResponse<AuthUser>>("/api/auth/me");
  return response.data.data;
}

export async function refreshSession(refreshToken?: string | null) {
  const response = await httpClient.post<ApiResponse<LoginResponse>>("/api/auth/refresh-token", { refreshToken });
  return response.data.data;
}

export async function logout(refreshToken: string | null) {
  await httpClient.post("/api/auth/logout", { refreshToken });
}

console.log(import.meta.env);
console.log("MODE", import.meta.env.MODE);
console.log("VITE_API_URL =", import.meta.env.VITE_API_URL);
console.log("VITE_API_BASE_URL =", import.meta.env.VITE_API_BASE_URL);