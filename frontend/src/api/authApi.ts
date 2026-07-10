import { httpClient } from "./httpClient";
import type { ApiResponse, AuthUser, LoginResponse } from "../types/auth";

export type PasswordPolicy = {
  minimumLength: number;
  requireUppercase: boolean;
  requireLowercase: boolean;
  requireDigit: boolean;
  requireSpecialCharacter: boolean;
  disallowUsername: boolean;
};

export type ChangePasswordPayload = {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
};

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

export async function getPasswordPolicy() {
  const response = await httpClient.get<ApiResponse<PasswordPolicy>>("/api/me/password-policy");
  return response.data.data;
}

export async function changePassword(payload: ChangePasswordPayload) {
  const response = await httpClient.post<ApiResponse<string>>("/api/me/change-password", payload);
  return response.data.message;
}
