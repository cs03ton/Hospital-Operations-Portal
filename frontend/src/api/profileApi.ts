import { httpClient } from "./httpClient";
import type { ApiResponse } from "../types/auth";

export type UserProfile = {
  id: string;
  employeeCode?: string | null;
  fullname: string;
  username: string;
  position?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
  leaveContactAddress?: string | null;
  gender: string;
  employmentType?: string | null;
  employmentStartDate?: string | null;
  profileImageUrl?: string | null;
  hasProfileImage: boolean;
  profileImageUpdatedAt?: string | null;
  roles: string[];
  departmentId?: string | null;
  departmentName?: string | null;
  leaveApprovalRuleId?: string | null;
  leaveApprovalRuleName?: string | null;
  lineUserId?: string | null;
  isActive: boolean;
  permissions: string[];
};

export type UpdateUserProfileRequest = {
  fullname: string;
  position?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
  leaveContactAddress?: string | null;
};

export type ProfileImageUploadResponse = {
  profileImageUrl: string;
  message: string;
};

export type LineBindingStatus = {
  isBound: boolean;
  connected?: boolean;
  status: string;
  lineUserIdMasked?: string | null;
  displayName?: string | null;
  pictureUrl?: string | null;
  boundAt?: string | null;
  connectedAt?: string | null;
  unboundAt?: string | null;
  expiresAt?: string | null;
};

export type LinePairingCode = {
  code: string;
  expiresAt: string;
  instruction: string;
};

export type LineConnectToken = {
  token: string;
  shortCode: string;
  expiresAt: string;
  lineAddFriendUrl?: string | null;
  qrCodePayload: string;
};

export type LineTestSendResponse = {
  success: boolean;
  message: string;
  deliveryStatus?: string | null;
  deliveryLogId?: string | null;
};

export async function getMyProfile() {
  const response = await httpClient.get<ApiResponse<UserProfile>>("/api/me/profile");
  return response.data.data;
}

export async function updateMyProfile(payload: UpdateUserProfileRequest) {
  const response = await httpClient.put<ApiResponse<UserProfile>>("/api/me/profile", payload);
  return response.data.data;
}

export async function uploadMyProfileImage(file: File) {
  const formData = new FormData();
  formData.append("file", file);
  const response = await httpClient.post<ApiResponse<ProfileImageUploadResponse>>("/api/me/profile/image", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return response.data.data;
}

export async function deleteMyProfileImage() {
  await httpClient.delete("/api/me/profile/image");
}

export async function getMyLineBinding() {
  const response = await httpClient.get<ApiResponse<LineBindingStatus>>("/api/me/line/status");
  const data = response.data.data;
  return {
    ...data,
    isBound: data.connected ?? data.isBound,
    boundAt: data.connectedAt ?? data.boundAt,
  };
}

export async function createMyLinePairingCode() {
  const response = await httpClient.post<ApiResponse<LinePairingCode>>("/api/me/profile/line/pairing-code");
  return response.data.data;
}

export async function createMyLineConnectToken() {
  const response = await httpClient.post<ApiResponse<LineConnectToken>>("/api/me/line/connect-token");
  return response.data.data;
}

export async function unbindMyLine() {
  const response = await httpClient.post<ApiResponse<LineBindingStatus>>("/api/me/line/disconnect");
  const data = response.data.data;
  return {
    ...data,
    isBound: data.connected ?? data.isBound,
    boundAt: data.connectedAt ?? data.boundAt,
  };
}

export async function sendMyLineTestMessage() {
  const response = await httpClient.post<ApiResponse<LineTestSendResponse>>("/api/me/profile/line/test-send");
  return response.data.data;
}
